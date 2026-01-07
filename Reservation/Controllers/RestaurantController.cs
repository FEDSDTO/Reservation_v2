using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Reservation.Models.EFMemeberModels;
using Reservation.Models.EFRestaurantModels;
using Reservation.Models.ViewModels;
using Reservation.Service;

namespace Reservation.Controllers
{
    public class RestaurantController : Controller
    {
        private readonly RestaurantContext _restaurantContext;
        private readonly MemberContext _memberContext;
        private readonly RestaurantService _restaurantService;
        private readonly Func_Log _fileLogService;
        private const string TokenCookieName = "MemberToken";

        public RestaurantController(
            RestaurantContext restaurantContext,
            MemberContext memberContext,
            RestaurantService restaurantService,
            Func_Log fileLogService)
        {
            _restaurantContext = restaurantContext;
            _memberContext = memberContext;
            _restaurantService = restaurantService;
            _fileLogService = fileLogService;
        }

        private List<CategoryModel> GetCategories()
        {
            return new List<CategoryModel>
            {
                new CategoryModel { Id = 0, Name = "所有餐廳" },
                new CategoryModel { Id = 1, Name = "主題餐廳" },
                new CategoryModel { Id = 2, Name = "輕食甜點" },
                new CategoryModel { Id = 3, Name = "吃到飽" }
            };
        }

        public async Task<IActionResult> Index(string? groupId, int? categoryId)
        {
            var mallGroups = await _restaurantService.GetMallsAsync();
            var branchDict = new Dictionary<string, int>();
            var groupIdToBranchId = new Dictionary<string, int>();
            var branches = new List<BranchModel>();
            
            for(int i = 0; i < mallGroups.Count; i++)
            {
                var branch = new BranchModel
                { 
                    Id = i + 1,
                    Name = mallGroups[i].Name 
                };
                branches.Add(branch);
                branchDict[mallGroups[i].GroupId] = branch.Id;
                groupIdToBranchId[mallGroups[i].GroupId] = branch.Id;
            }

            var allCategories = GetCategories();
            var restaurantCards = new List<RestaurantCardModel>();
            
            // ========== 主頁載入時，從 API 同步資料到資料庫 ==========
            if(string.IsNullOrWhiteSpace(groupId))
            {
                // 顯示所有分館：同步所有分館的資料
                foreach(var mallGroup in mallGroups)
                {
                    await _restaurantService.RestaurantApiAsync(mallGroup.GroupId);
                    var groupRestaurants = await _restaurantService.GetRestaurantsAsync(mallGroup.GroupId);
                    restaurantCards.AddRange(groupRestaurants);
                }
            }
            else
            {
                // 顯示特定分館：只同步該分館的資料
                await _restaurantService.RestaurantApiAsync(groupId);
                restaurantCards = await _restaurantService.GetRestaurantsAsync(groupId);
            }

            var viewModel = new RestaurantListModel
            {
                Branches = branches,
                Categories = allCategories,
                RestaurantCards = restaurantCards,
                SelectedGroupId = groupId,
                SelectedBranchId = !string.IsNullOrWhiteSpace(groupId) && groupIdToBranchId.ContainsKey(groupId)
                    ? groupIdToBranchId[groupId]
                    : null,
                SelectedCategoryId = categoryId,
                GroupIdToBranchId = groupIdToBranchId
            };
            return View(viewModel);
        }
        public async Task<IActionResult> QueryRecord(int? branchId)
        {
            var mallGroups = await _restaurantService.GetMallsAsync();
            
            var branchDict = new Dictionary<string, int>();
            var branches = new List<BranchModel>();
            
            for (int i = 0; i < mallGroups.Count; i++)
            {
                var branch = new BranchModel 
                { 
                    Id = i + 1,
                    Name = mallGroups[i].Name 
                };
                branches.Add(branch);
                branchDict[mallGroups[i].GroupId] = branch.Id;
            }

            var reservationRecords = new List<ReservationRecordModel>();
            var waitingRecords = new List<WaitingRecordModel>();

            if (branchId.HasValue)
            {
                var selectedGroupId = branchDict.FirstOrDefault(x => x.Value == branchId.Value).Key;
                if (!string.IsNullOrEmpty(selectedGroupId))
                {
                    var restaurantCards = await _restaurantService.GetRestaurantsAsync(selectedGroupId);
                }
            }

            var viewModel = new RecordQueryModel
            {
                Branches = branches,
                SelectedBranchId = branchId,
                ReservationRecords = reservationRecords,
                WaitingRecords = waitingRecords
            };

            return View(viewModel);
        }

        public IActionResult Queue(int id)
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMallByGPS(double? lat, double? lng)
        {
            try
            {
                _fileLogService?.SystemLog_Txt($"=== GPS 定位請求開始 ===");
                _fileLogService?.SystemLog_Txt($"GPS 座標參數 - 緯度: {lat}, 經度: {lng}");
                
                if(!lat.HasValue || !lng.HasValue)
                {
                    _fileLogService?.SystemErrorLog_Txt("GPS 定位失敗：座標參數缺失");
                    return Json(new { success = false, message = "座標參數缺失" });
                }

                _fileLogService?.SystemLog_Txt("開始取得分館列表...");
                var mallGroups = await _restaurantService.GetMallsAsync();
                _fileLogService?.SystemLog_Txt($"取得分館列表成功，共 {mallGroups.Count} 個分館");

                // 定義各分館的座標範圍（根據實際分館位置）
                var branchLocations = new Dictionary<string, (double lat, double lng, double radius)>
                {
                    { "feds-32", (25.041859000, 121.509014000, 0.05) }, // 遠百寶慶
                    { "feds-37", (23.473118000, 120.441096000, 0.05) }, // 遠百嘉義
                    { "feds-40", (24.989982000, 121.313795000, 0.05) }, // 遠百桃園
                    { "feds-42", (24.802178000, 120.964880000, 0.05) }, // 新竹大遠百
                    { "feds-48", (22.996651000, 120.214358000, 0.05) }, // 台南大遠百
                    { "feds-50", (25.011361000, 121.464402000, 0.05) }, // 遠百板橋
                    { "feds-51", (22.613360000, 120.303994000, 0.05) }, // 高雄大遠百
                    { "feds-52", (23.978682000, 121.599776000, 0.05) }, // 遠百花蓮
                    { "feds-53", (24.164204000, 120.644555000, 0.05) }, // 台中大遠百
                    { "feds-54", (25.013950000, 121.466880000, 0.05) }, // 板橋大遠百
                    { "feds-55", (25.036882000, 121.566125100, 0.05) }, // 遠百信義A13
                    { "feds-72", (24.822541000, 121.022852800, 0.05) }  // 遠百竹北
                };

                string? nearestGroupId = null;
                double minDistance = double.MaxValue;
                int checkedCount = 0;
                int matchedCount = 0;

                _fileLogService?.SystemLog_Txt("開始計算最近的分館...");
                foreach(var group in mallGroups)
                {
                    if(branchLocations.ContainsKey(group.GroupId))
                    {
                        var branch = branchLocations[group.GroupId];
                        var distance = CalculateDistance(lat.Value, lng.Value, branch.lat, branch.lng);
                        checkedCount++;
                        
                        _fileLogService?.SystemLog_Txt($"分館 {group.GroupId} ({group.Name}) - 距離: {distance:F4} 公里");
                        
                        if(distance < minDistance)
                        {
                            minDistance = distance;
                            nearestGroupId = group.GroupId;
                            matchedCount++;
                            _fileLogService?.SystemLog_Txt($"  → 更新最近分館: {nearestGroupId}, 距離: {minDistance:F4} 公里");
                        }
                    }
                    else
                    {
                        _fileLogService?.SystemLog_Txt($"分館 {group.GroupId} ({group.Name}) - 不在座標字典中，跳過");
                    }
                }

                _fileLogService?.SystemLog_Txt($"計算完成 - 檢查了 {checkedCount} 個分館，匹配了 {matchedCount} 個分館");

                if(!string.IsNullOrEmpty(nearestGroupId))
                {
                    _fileLogService?.SystemLog_Txt($"GPS 定位成功 - 最近分館: {nearestGroupId}, 距離: {minDistance:F4} 公里");
                    return Json(new { success = true, groupId = nearestGroupId, distance = minDistance });
                }

                if(mallGroups.Any())
                {
                    var defaultGroupId = mallGroups.First().GroupId;
                    _fileLogService?.SystemLog_Txt($"GPS 定位未找到匹配分館，使用預設分館: {defaultGroupId}");
                    return Json(new{
                        success = true,
                        groupId = defaultGroupId,
                        distance = 0,
                        message="使用預設分館"
                    });
                }

                _fileLogService?.SystemErrorLog_Txt("GPS 定位失敗：找不到最近的分館，且沒有可用分館");
                return Json(new { success = false, message = "找不到最近的分館" });
            }
            catch(Exception ex)
            {
                _fileLogService?.SystemErrorLog_Txt($"GPS 定位發生異常：{ex.Message}\r\n堆疊追蹤：{ex.StackTrace}");
                return Json(new { success = false, message = "GPS 定位處理發生錯誤" });
            }
        }

        // 計算兩點間距離（公里）- Haversine 公式
        private double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371; // 地球半徑（公里）
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLng = (lng2 - lng1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }
}
