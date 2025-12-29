using Newtonsoft.Json.Linq;
using Reservation.Models.DB;
using Reservation.ViewModels.Restaurants;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using Reservation.Models.ViewModels;
using System.ComponentModel.Design;
using System.Net;
using System.Xml.Linq;


namespace Reservation.Models
{
    public class Restaurants
    {

        /// <summary>
        /// 取得商場列表
        /// </summary>
        /// <returns></returns>
        public List<Mall> GetMalls()
        {
            FEDSRESTAURANTEntities _db = new FEDSRESTAURANTEntities();
            return _db.MallGroups.Where(m => m.IsUse)?.OrderBy(m => m.CreateDate).Select(m => new Mall { GroupId = m.id, Name = m.Name }).ToList();
        }

        /// <summary>
        /// 取得餐廳列表
        /// </summary>
        /// <param name="GroupId"></param>
        /// <returns></returns>
        public List<RestaurantCard> GetRestaurants(string GroupId)
        {
            List<RestaurantCard> cards = new List<RestaurantCard>();
            Dictionary<string, WaitingInfo> waitingInfos = new Dictionary<string, WaitingInfo>();
            try
            {
               // 取得分公司餐廳
                ApiResult apiResult = InlineApps.GetInlineappsApi($"/v2/groups/{GroupId}", $"type=all&size=1&date={DateTime.Now:yyyy-MM-dd}");


                if (apiResult.code == 200)
                {
                   // 解析API資料
                    JObject data = JObject.Parse(apiResult.data);
                    JArray jarrCards = (JArray)data.GetValue("branches");

                    foreach (JObject jCard in jarrCards)
                    {
                        RestaurantCard card = jCard.ToObject<RestaurantCard>();
                        if (card == null) continue;

                        bool hasUpdate = false;
                        string updateLog = string.Empty;
                        JObject company = jCard.Value<JObject>("company");

                        string id = jCard.Value<string>("id");
                        JObject waitingInfo = jCard.Value<JObject>("waitingInfo");
                        if (waitingInfo != null)
                        {
                            waitingInfos[id] = new WaitingInfo
                            {
                                WaitingCount = waitingInfo.Value<int>("waitingCount"),
                                EstimatedMinutes = waitingInfo.Value<int>("estimatedWaitingMinutes")
                            };
                        }
                        ;

                        using (FEDSRESTAURANTEntities _db = new FEDSRESTAURANTEntities())
                        {
                            // === 更新公司資料 ===
                            if (company != null)
                            {
                                card.Name = $"{company.Value<string>("name")} {card.Name}";

                                string companyId = company.Value<string>("id");
                                string companyName = company.Value<string>("name");
                                var restaurant = _db.Restaurants.Find(companyId);

                                if (restaurant == null)
                                {
                                    _db.Restaurants.Add(new DB.Restaurant()
                                    {
                                        id = companyId,
                                        Name = companyName,
                                        CreateDate = DateTime.Now,
                                        CreateFrom = "Inline groups API",
                                        Creator = 0,
                                    });
                                    hasUpdate = true;
                                    updateLog += $"\r\n\t新增廠商 Id:{companyId}, Name:{companyName}";
                                }
                                else if (!restaurant.Name.Equals(companyName))
                                {
                                    restaurant.Name = companyName;
                                    restaurant.EditDate = DateTime.Now;
                                    restaurant.EditFrom = "Inline groups API";
                                    restaurant.Editor = 0;
                                    _db.Entry(restaurant).State = EntityState.Modified;
                                    hasUpdate = true;
                                    updateLog += $"\r\n\t修改廠商資料 Id:{companyId}, Name:{companyName}";
                                }
                            }

                           //  === 更新餐廳資料 ===
                            var currentBranch = _db.RestaurantBranches.Find(card.id);
                            if (currentBranch == null)
                            {
                                _db.RestaurantBranches.Add(new RestaurantBranch()
                                {
                                    id = card.id,
                                    GroupId = GroupId,
                                    CompanyId = card.CompanyId,
                                    Name = card.Name,
                                    Address = card.Address,
                                    PhoneNumber = card.PhoneNumber,
                                    WebBookingEnable = card.WebBookingEnabled,
                                    WebWaitingEnable = card.WebWaitingEnabled,
                                    CreateDate = DateTime.Now,
                                    CreateFrom = "Inline groups API",
                                    Creator = 0,
                                    IsUse = true
                                });
                                hasUpdate = true;
                                updateLog += $"\r\n\t新增餐廳 Id:{card.id}, Name:{card.Name}, GroupId:{GroupId}";
                            }
                            else if (currentBranch.Name != card.Name ||
                             currentBranch.Address != card.Address ||
                             currentBranch.PhoneNumber != card.PhoneNumber ||
                             currentBranch.WebBookingEnable != card.WebBookingEnabled ||
                             currentBranch.WebWaitingEnable != card.WebWaitingEnabled)
                            {
                                currentBranch.Name = card.Name;
                                currentBranch.Address = card.Address;
                                currentBranch.PhoneNumber = card.PhoneNumber;
                                currentBranch.WebBookingEnable = card.WebBookingEnabled;
                                currentBranch.WebWaitingEnable = card.WebWaitingEnabled;
                                currentBranch.IsUse = true;
                                _db.Entry(currentBranch).State = EntityState.Modified;
                                hasUpdate = true;
                            }

                            if (hasUpdate)
                                try
                                {
                                    _db.SaveChanges();
                                    Log.Func_Log.InsertLog(Log.LogType.Info, $"從API更新資料: {updateLog}");
                                }
                                catch (DbEntityValidationException ex)
                                {
                                    if (ex.EntityValidationErrors != null)
                                    {
                                        updateLog += "\r\n\t";
                                        foreach (DbEntityValidationResult EntityValidationError in ex.EntityValidationErrors)
                                            foreach (DbValidationError ValidationError in EntityValidationError.ValidationErrors)
                                                updateLog += $"  {ValidationError.ErrorMessage}";
                                    }
                                    Log.Func_Log.InsertLog(Log.LogType.Warn, $"從API更新資料發生錯誤: {updateLog}");
                                }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Log.Func_Log.InsertLog(Log.LogType.Error, $"API同步更新資料失敗: {ex.Message}");
            }

            try
            {
                using (FEDSRESTAURANTEntities _db = new FEDSRESTAURANTEntities())
                {
                    var dbBranches = _db.RestaurantBranches.Where(x => x.GroupId == GroupId && x.IsUse == true);

                    if (dbBranches.Count() == 0)
                    {
                        Log.Func_Log.InsertLog(Log.LogType.Warn, $"查詢結果為空,GroupId{GroupId}");
                    }

                    foreach (var dbBranch in dbBranches)
                    {
                        RestaurantCard card = new RestaurantCard
                        {
                            id = dbBranch.id,
                            GroupId = dbBranch.GroupId,
                            CompanyId = dbBranch.CompanyId,
                            Name = dbBranch.Name,
                            Address = dbBranch.Address,
                            PhoneNumber = dbBranch.PhoneNumber,
                            WebBookingEnabled = dbBranch.WebBookingEnable,
                            WebWaitingEnabled = dbBranch.WebWaitingEnable,
                            WaitingCount = 0,
                            EstimatedWaitingMinutes = 0                          
                            
                        };

                        //補候位資訊
                        if (waitingInfos.ContainsKey(dbBranch.id))
                        {
                            
                            card.WaitingCount = waitingInfos[dbBranch.id].WaitingCount;
                            card.EstimatedWaitingMinutes = waitingInfos[dbBranch.id].EstimatedMinutes;
                        }

                        Log.Func_Log.InsertLog(Log.LogType.Info,$"資料庫撈取資料:"+
                          $"[Branch] ID: {dbBranch.id}, Name: {dbBranch.Name}, CompanyId: {dbBranch.CompanyId}, " +
                          $"Address: {dbBranch.Address}, Phone: {dbBranch.PhoneNumber}, " +
                          $"WebBooking: {dbBranch.WebBookingEnable}, WebWaiting: {dbBranch.WebWaitingEnable}");

                        cards.Add(card);
                    };
                    
                   

                    Log.Func_Log.InsertLog(Log.LogType.Info, $"從資料庫撈取資料: {cards.Count}筆資料, GroupId:{GroupId}");
                }
            }
            catch (Exception ex)
            {
                Log.Func_Log.InsertLog(Log.LogType.Error, $"從資料庫撈取失敗:{ex.Message}");
            }

            return cards;

        }


    }
}