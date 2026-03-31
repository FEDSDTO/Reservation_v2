using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reservation.Models;
using Reservation.Models.EFRestaurantModels;
using Reservation.Models.ViewModels;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace Reservation.Service
{
    /// <summary>
    /// 餐廳服務類別
    /// </summary>
    public class RestaurantService
    {
        private readonly RestaurantContext _restaurantContext;
        private readonly InlineAppsService _inlineAppsService;
        private readonly Func_Log _Log;
        private readonly IWebHostEnvironment _env;

        public RestaurantService(
            RestaurantContext restaurantContext,
            InlineAppsService inlineAppsService,
            Func_Log fileLogService,
            IWebHostEnvironment env)
        {
            _restaurantContext = restaurantContext;
            _inlineAppsService = inlineAppsService;
            _Log = fileLogService;
            _env = env;
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
        public async Task<List<RestaurantCardModel>> GetRestaurantsAsync(string groupId, int? categoryId = null)
        {
            var cards = new List<RestaurantCardModel>();
            try
            {
                var query = _restaurantContext.RestaurantBranches
                    .Where(x => x.GroupId == groupId && x.IsUse == true);

                if (categoryId == 1)
                {
                    query = query.Where(x => x.RestaurantCategoryId == 1 || x.RestaurantCategoryId == 3);
                }
                else if (categoryId == 2)
                {
                    query = query.Where(x => x.RestaurantCategoryId == 2 || x.RestaurantCategoryId == 3);
                }

                var dbBranches = await query.ToListAsync();

                foreach (var dbBranch in dbBranches)
                {
                    var card = new RestaurantCardModel
                    {
                        id = dbBranch.Id,
                        GroupId = dbBranch.GroupId ?? "",
                        CompanyId = dbBranch.CompanyId ?? "",
                        Name = dbBranch.Name,
                        Address = dbBranch.Address,
                        PhoneNumber = dbBranch.PhoneNumber,
                        WebBookingEnabled = dbBranch.WebBookingEnable,
                        WebWaitingEnabled = dbBranch.WebWaitingEnable,
                        WaitingCount = 0,
                        EstimatedWaitingMinutes = 0,
                        Images = new List<string>(),
                        ImageUrl = GetRestaurantImageUrl(dbBranch.GroupId ?? "", dbBranch.Id)
                    };
                    cards.Add(card);
                }
            }
            catch (Exception ex)
            {
                _Log?.SystemErrorLog_Txt($"資料庫讀取錯誤 - GroupId: {groupId}, CategoryId: {categoryId}, Error: {ex.Message}");
            }

            return cards;
        }
    
        /// <summary>
        /// 從 API 同步餐廳資料到資料庫（只在主頁載入時呼叫）
        /// </summary>
        /// <param name="groupId">分館 ID</param>
        /// <returns>同步是否成功</returns>
        public async Task<bool> RestaurantApiAsync(string groupId)
        {
            try
            {
                var apiResult = await _inlineAppsService.GetInlineApps(
                    $"/v2/groups/{groupId}", 
                    $"type=all&size=1&date={DateTime.Now:yyyy-MM-dd}");

                if(apiResult.Code == 200)
                {
                    var data = JObject.Parse(apiResult.Data);
                    var jarrCards = (JArray?)data.GetValue("branches");

                    if(jarrCards != null)
                    {
                        foreach(JObject jCard in jarrCards)
                        {
                            var card = jCard.ToObject<RestaurantCardModel>();
                            if(card == null) continue;
                            
                            bool hasUpdate = false;
                            var company = jCard.Value<JObject>("company");
                            string? id = jCard.Value<string>("id");
                            if(string.IsNullOrEmpty(id)) continue;

                            // ========== 更新餐廳公司（品牌）資料 ==========
                            if(company != null)
                            {
                                string? companyName = company.Value<string>("name");
                                if(!string.IsNullOrEmpty(companyName))
                                {
                                    card.Name = $"{companyName} {card.Name}";
                                }
                                string? companyId = company.Value<string>("id");
                                if(!string.IsNullOrEmpty(companyId) && !string.IsNullOrEmpty(companyName))
                                {
                                    var restaurant = await _restaurantContext.Restaurants.FindAsync(companyId);
                                    if(restaurant == null)
                                    {
                                        _restaurantContext.Restaurants.Add(new Restaurant(){
                                            Id = companyId,
                                            Name = companyName,
                                            CreateDate = DateTime.Now,
                                            CreateFrom = "Inline groups API",
                                            Creator = 0,
                                        });
                                        hasUpdate = true;
                                    }
                                    else if(!restaurant.Name.Equals(companyName))
                                    {
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
                            if(currentBranch == null)
                            {
                                _restaurantContext.RestaurantBranches.Add(new RestaurantBranch(){
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
                                    IsUse = true,
                                });
                                hasUpdate = true;
                            }
                            else
                            {
                                bool needUpdate = currentBranch.Name != card.Name ||
                                                 currentBranch.Address != card.Address ||
                                                 currentBranch.PhoneNumber != card.PhoneNumber ||
                                                 currentBranch.WebBookingEnable != card.WebBookingEnabled ||
                                                 currentBranch.WebWaitingEnable != card.WebWaitingEnabled;

                                if(needUpdate)
                                {
                                    currentBranch.Name = card.Name;
                                    currentBranch.Address = card.Address;
                                    currentBranch.PhoneNumber = card.PhoneNumber;
                                    currentBranch.WebBookingEnable = card.WebBookingEnabled;
                                    currentBranch.WebWaitingEnable = card.WebWaitingEnabled;
                                    currentBranch.IsUse = true;
                                    currentBranch.EditDate = DateTime.Now;
                                    currentBranch.EditFrom = "Inline groups API";
                                    _restaurantContext.Entry(currentBranch).State = EntityState.Modified;
                                    hasUpdate = true;
                                }
                            }
                            
                            // 如果有資料變更，儲存到資料庫
                            if(hasUpdate)
                            {
                                try
                                {
                                    await _restaurantContext.SaveChangesAsync();
                                }
                                catch(Exception ex)
                                {
                                    _Log?.SystemErrorLog_Txt($"儲存資料失敗 - BranchId: {card.id}, Error: {ex.Message}");
                                }
                            }
                        }
                        return true;
                    }
                }
            }
            catch(Exception ex)
            {
                _Log?.SystemErrorLog_Txt($"API 同步錯誤 - GroupId: {groupId}, Error: {ex.Message}");
                return false;
            }
            return false;
        }
    
        /// <summary>
        /// 從資料庫取得單一分店的詳細資料
        /// </summary>
        /// <param name="groupId">分館 ID</param>
        /// <param name="branchId">分店 ID</param>
        /// <returns>餐廳卡片資料，如果找不到則回傳 null</returns>
        public async Task<RestaurantCardModel?> GetRestaurantDetailAsync(string groupId, string branchId)
        {
            try
            {
                var dbBranch = await _restaurantContext.RestaurantBranches
                    .FirstOrDefaultAsync(x => x.GroupId == groupId && x.Id == branchId && x.IsUse == true);

                if(dbBranch == null)
                {
                    return null;
                }

                return new RestaurantCardModel
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
                    EstimatedWaitingMinutes = 0,
                    Images = new List<string>(),
                    ImageUrl = GetRestaurantImageUrl(dbBranch.GroupId ?? "", dbBranch.Id)
                };
            }
            catch(Exception ex)
            {
                _Log?.SystemErrorLog_Txt($"取得餐廳詳細資料錯誤 - GroupId: {groupId}, BranchId: {branchId}, Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 依 GroupId / BranchId 從 wwwroot/IMG/HomePage 取得圖片路徑；若檔案不存在則寫錯誤 log 並回傳預設圖。
        /// </summary>
        public string GetRestaurantImageUrl(string groupId, string branchId)
        {
            var relativePath = Path.Combine("IMG", "HomePage", groupId, $"{branchId}.jpg");
            var physicalPath = Path.Combine(_env.WebRootPath, relativePath);

            if (!File.Exists(physicalPath))
            {
                _Log?.SystemErrorLog_Txt(
                    $"[RestaurantService.GetRestaurantImageUrl] 實體圖片不存在 - PhysicalPath: {physicalPath}, GroupId: {groupId}, BranchId: {branchId}");
                return "~/IMG/HomePage/10.jpg";
            }

            return "~/" + relativePath.Replace("\\", "/");
        }
    }
}
