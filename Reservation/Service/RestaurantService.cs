using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reservation.Models;
using Reservation.Models.EFRestaurantModels;
using Reservation.Models.ViewModels;

namespace Reservation.Service
{
    /// <summary>
    /// 餐廳服務類別
    /// </summary>
    public class RestaurantService
    {
        private readonly RestaurantContext _restaurantContext;
        private readonly InlineAppsService _inlineAppsService;

        public RestaurantService(
            RestaurantContext restaurantContext,
            InlineAppsService inlineAppsService)
        {
            _restaurantContext = restaurantContext;
            _inlineAppsService = inlineAppsService;
        }

        /// <summary>
        /// 取得所有啟用的商場（分館）列表
        /// </summary>
        /// <returns>分館列表，按建立時間排序</returns>
        public async Task<List<MallModel>> GetMallsAsync()
        {
            try
            {
                var malls = await _restaurantContext.MallGroups
                    .Where(m => m.IsUse == true)  
                    .OrderBy(m => m.CreateDate)  
                    .Select(m => new MallModel 
                    { 
                        GroupId = m.Id,  
                        Name = m.Name    
                    })
                    .ToListAsync();

                return malls;
            }
            catch (Exception)
            {
                return new List<MallModel>();
            }
        }

        /// <summary>
        /// 取得指定分館的餐廳列表
        /// </summary>
        /// <param name="groupId">分館 ID（GroupId）</param>
        /// <returns>餐廳卡片列表</returns>
        public async Task<List<RestaurantCardModel>> GetRestaurantsAsync(string groupId)
        {
            var cards = new List<RestaurantCardModel>();
            var waitingInfos = new Dictionary<string, WaitingInfoModel>();

            try
            {

                var apiResult = await _inlineAppsService.GetInlineApps(
                    $"/v2/groups/{groupId}", 
                    $"type=all&size=1&date={DateTime.Now:yyyy-MM-dd}");

                // 檢查 API 回應是否成功（HTTP 200）
                if (apiResult.Code == 200)
                {
                    // 解析 JSON 回應資料
                    var data = JObject.Parse(apiResult.Data);
                    var jarrCards = (JArray?)data.GetValue("branches");  

                    if (jarrCards != null)
                    {
                        // 逐一處理每個餐廳分店的資料
                        foreach (JObject jCard in jarrCards)
                        {
                            // 將 JSON 物件轉換為 RestaurantCardModel 物件
                            var card = jCard.ToObject<RestaurantCardModel>();
                            if (card == null) continue;  // 如果轉換失敗，跳過此筆資料

                            bool hasUpdate = false;  // 標記是否有資料需要更新
                            
                            // 取得餐廳公司（品牌）資訊
                            var company = jCard.Value<JObject>("company");
                            
                            // 取得餐廳分店 ID
                            string? id = jCard.Value<string>("id");
                            if (string.IsNullOrEmpty(id)) continue;

                            // 取得候位資訊（如果有的話）
                            var waitingInfo = jCard.Value<JObject>("waitingInfo");
                            if (waitingInfo != null)
                            {
                                // 將候位資訊儲存到字典中，稍後會合併到資料庫資料
                                waitingInfos[id] = new WaitingInfoModel
                                {
                                    WaitingCount = waitingInfo.Value<int>("waitingCount"),  // 目前候位組數
                                    EstimatedMinutes = waitingInfo.Value<int>("estimatedWaitingMinutes")  // 預計等候時間（分鐘）
                                };
                            }

                            // ========== 更新餐廳公司（品牌）資料 ==========
                            if (company != null)
                            {
                                string? companyName = company.Value<string>("name");
                                if (!string.IsNullOrEmpty(companyName))
                                {
                                    card.Name = $"{companyName} {card.Name}";
                                }

                                string? companyId = company.Value<string>("id");
                                if (!string.IsNullOrEmpty(companyId) && !string.IsNullOrEmpty(companyName))
                                {
                                    // 查詢資料庫中是否已存在此餐廳公司
                                    var restaurant = await _restaurantContext.Restaurants.FindAsync(companyId);

                                    if (restaurant == null)
                                    {
                                        // 如果不存在，新增餐廳公司資料
                                        _restaurantContext.Restaurants.Add(new Reservation.Models.EFRestaurantModels.Restaurant()
                                        {
                                            Id = companyId,
                                            Name = companyName,
                                            CreateDate = DateTime.Now,
                                            CreateFrom = "Inline groups API",
                                            Creator = 0,
                                        });
                                        hasUpdate = true;
                                    }
                                    else if (!restaurant.Name.Equals(companyName))
                                    {
                                        // 如果存在但名稱不同，更新餐廳公司資料
                                        restaurant.Name = companyName;
                                        restaurant.EditDate = DateTime.Now;
                                        restaurant.EditFrom = "Inline groups API";
                                        restaurant.Editor = 0;
                                        _restaurantContext.Entry(restaurant).State = EntityState.Modified;
                                        hasUpdate = true;
                                    }
                                }
                            }

                            // ========== 更新餐廳分店資料 ==========
                            var currentBranch = await _restaurantContext.RestaurantBranches.FindAsync(card.id);
                            
                            if (currentBranch == null)
                            {

                                _restaurantContext.RestaurantBranches.Add(new RestaurantBranch()
                                {
                                    Id = card.id,
                                    GroupId = groupId,
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
                            }
                            else
                            {
                                // 如果分店已存在，檢查是否有資料變更
                                bool needUpdate = currentBranch.Name != card.Name ||
                                                 currentBranch.Address != card.Address ||
                                                 currentBranch.PhoneNumber != card.PhoneNumber ||
                                                 currentBranch.WebBookingEnable != card.WebBookingEnabled ||
                                                 currentBranch.WebWaitingEnable != card.WebWaitingEnabled;

                                if (needUpdate)
                                {
                                    // 更新分店資料
                                    currentBranch.Name = card.Name;
                                    currentBranch.Address = card.Address;
                                    currentBranch.PhoneNumber = card.PhoneNumber;
                                    currentBranch.WebBookingEnable = card.WebBookingEnabled;
                                    currentBranch.WebWaitingEnable = card.WebWaitingEnabled;
                                    currentBranch.IsUse = true;  // 確保是啟用狀態
                                    _restaurantContext.Entry(currentBranch).State = EntityState.Modified;
                                    hasUpdate = true;
                                }
                            }

                            // 如果有資料變更，儲存到資料庫
                            if (hasUpdate)
                            {
                                try
                                {
                                    await _restaurantContext.SaveChangesAsync();
                                }
                                catch (Exception)
                                {
                                    // 忽略儲存錯誤，繼續處理下一筆
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 忽略 API 同步錯誤，繼續從資料庫讀取資料
            }

            // ========== 第二階段：從資料庫讀取餐廳資料 ==========
            try
            {
                // 從資料庫讀取指定分館的所有啟用餐廳分店
                var dbBranches = await _restaurantContext.RestaurantBranches
                    .Where(x => x.GroupId == groupId && x.IsUse == true)
                    .ToListAsync();

                // 將資料庫的餐廳分店資料轉換為 RestaurantCardModel
                foreach (var dbBranch in dbBranches)
                {
                    var card = new RestaurantCardModel
                    {
                        id = dbBranch.Id,
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

                    // 如果有從 API 取得的候位資訊，合併到卡片資料中
                    if (waitingInfos.ContainsKey(dbBranch.Id))
                    {
                        card.WaitingCount = waitingInfos[dbBranch.Id].WaitingCount;
                        card.EstimatedWaitingMinutes = waitingInfos[dbBranch.Id].EstimatedMinutes;
                    }

                    cards.Add(card);
                }
            }
            catch (Exception)
            {
                // 忽略資料庫讀取錯誤，回傳空列表
            }

            return cards;
        }
    }
}
