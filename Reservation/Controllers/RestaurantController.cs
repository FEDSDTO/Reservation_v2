using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly Func_Log _Log;
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
            _Log = fileLogService;
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
           _Log?.SystemLog_Txt($"[QueryRecord] 開始查詢訂候位記錄 - BranchId: {branchId?.ToString() ?? "全部"}");
           var mallGroups = await _restaurantService.GetMallsAsync();
           var branchDict =  new Dictionary<string, int>();
           var branches = new List<BranchModel>();

           for(int i = 0;i<mallGroups.Count;i++)
           {
              var branch = new BranchModel
              {
                Id = i +1,
                Name = mallGroups[i].Name
              };
              branches.Add(branch);
              branchDict[mallGroups[i].GroupId] = branch.Id;
           }

           var reservationRecords = new List<ReservationRecordModel>();
           var waitingRecords = new List<WaitingRecordModel>();

           // ========== 取得登入會員 ID ==========
          int memberId = 0;
          if(HttpContext.Items["MemberId"] != null && HttpContext.Items["MemberId"] is int memberIdValue)
          {
            memberId = memberIdValue;
          }
          if(memberId == 0)
          {
                _Log?.SystemLog_Txt($"[QueryRecord] 會員未登入，重定向到餐廳列表");
                TempData["ErrorMsg"] ="請先登入會員";
                return RedirectToAction("Index","Restaurant");
          }
          
          var fromDate = DateTime.Today.AddMonths(-3);
          var toDate = DateTime.Today.AddMonths(3).AddDays(1);
          
          _Log?.SystemLog_Txt($"[QueryRecord] 查詢條件 - MemberId: {memberId}, BranchId: {branchId?.ToString() ?? "全部"}");
          // ========== 取得會員姓名並提取姓氏 ==========
          string memberName = "會員";
          try
          {
             var member = await _memberContext.Members.FirstOrDefaultAsync(m=>m.Id == memberId);

             if(member != null && !string.IsNullOrEmpty(member.Name))
             {
                if(member.Name.Length > 1)
                {
                    memberName = member.Name.Substring(0,1)+"0"+member.Name.Substring(member.Name.Length-1);
                }
                else
                {
                    memberName = member.Name;
                }
             }
          }
          catch(Exception ex)
          {
            _Log?.SystemErrorLog_Txt($"[QueryRecord] 取得會員姓名失敗:{ex.Message}");
          }

           // ========== 查詢所有分館的記錄（如果沒有指定 branchId） ==========
           if(!branchId.HasValue)
           {
               // 查詢所有訂位記錄
               var allReserves = await _restaurantContext.MemberReserves
                   .Where(r => r.MemberId == memberId && r.CreateDate >= fromDate && r.CreateDate < toDate)
                   .OrderByDescending(r => r.CreateDate)
                   .ToListAsync();

               _Log?.SystemLog_Txt($"[QueryRecord] 查詢到訂位記錄總數: {allReserves.Count}");

               foreach(var reserve in allReserves)
               {
                if(!string.IsNullOrEmpty(reserve.CompanyId) && !string.IsNullOrEmpty(reserve.BranchId))
                {
                    var RestaurantBranch = await _restaurantContext.RestaurantBranches.FirstOrDefaultAsync(rb => rb.CompanyId==reserve.CompanyId && rb.Id == reserve.BranchId);

                    if(RestaurantBranch != null)
                    {
                        var groupId = RestaurantBranch.GroupId ?? "";

                        reservationRecords.Add(new ReservationRecordModel{
                            ReservationId = (int)reserve.Id,
                            RestaurantId = RestaurantBranch.CompanyId.GetHashCode(),
                            RestaurantName = RestaurantBranch.Name,
                            RestaurantImageUrl = $"~/IMG/HomePage/{groupId}/{reserve.BranchId}.jpg",
                            RestaurantLocation = RestaurantBranch.Address,
                            RestaurantPhone = RestaurantBranch.PhoneNumber,
                            ReservationDate = reserve.Datetime,
                            DayOfWeek = reserve.Datetime.ToString("dddd", new System.Globalization.CultureInfo("zh-TW")),
                            AdultCount = reserve.GroupSize,
                            ChildCount = reserve.NumberOfKid,
                            Status = reserve.Status ?? "已預訂"
                        });
                        
                        // 記錄每筆訂位記錄
                        _Log?.SystemLog_Txt($"[QueryRecord] 訂位記錄 - ID: {reserve.Id}, 餐廳: {RestaurantBranch.Name}, 日期: {reserve.Datetime:yyyy-MM-dd}, 狀態: {reserve.Status ?? "已預訂"}, CompanyId: {reserve.CompanyId}, BranchId: {reserve.BranchId}");
                    }
                    else
                    {
                        _Log?.SystemLog_Txt($"[QueryRecord] 訂位記錄找不到餐廳資訊 - ReserveId: {reserve.Id}, CompanyId: {reserve.CompanyId}, BranchId: {reserve.BranchId}");
                    }
                }
               }
               
               _Log?.SystemLog_Txt($"[QueryRecord] 成功處理訂位記錄數: {reservationRecords.Count}");
               
               // 查詢所有候位記錄
               var allWaitings = await _restaurantContext.MemberWaitings
                   .Where(w => w.MemberId == memberId && w.CreateDate >= fromDate && w.CreateDate < toDate)
                   .OrderByDescending(w => w.CreateDate)
                   .ToListAsync();

               _Log?.SystemLog_Txt($"[QueryRecord] 查詢到候位記錄總數: {allWaitings.Count}");

               foreach(var waiting in allWaitings)
               {
                    // 透過 CompanyId 和 BranchId 查詢 RestaurantBranch
                    if(!string.IsNullOrEmpty(waiting.CompanyId) && !string.IsNullOrEmpty(waiting.BranchId))
                    {
                        var restaurantBranch = await _restaurantContext.RestaurantBranches
                            .FirstOrDefaultAsync(rb => rb.CompanyId == waiting.CompanyId && rb.Id == waiting.BranchId);
                        
                        if(restaurantBranch != null)
                        {
                            var groupId = restaurantBranch.GroupId ?? "";
                            
                            waitingRecords.Add(new WaitingRecordModel{
                                WaitingId = waiting.Id,
                                RestaurantId = restaurantBranch.CompanyId.GetHashCode(),
                                RestaurantName = restaurantBranch.Name,
                                RestaurantImageUrl = $"~/IMG/HomePage/{groupId}/{waiting.BranchId}.jpg",
                                RestaurantLocation = restaurantBranch.Address,
                                RestaurantPhone = restaurantBranch.PhoneNumber,
                                JoinDate = waiting.CreateDate,
                                AdultCount = waiting.GroupSize,
                                ChildCount = waiting.NumberOfKid,
                                QueueNumber = waiting.PositionInLine ?? 0,
                                Status = waiting.Status ?? "等待中"
                            });
                            
                            // 記錄每筆候位記錄
                            _Log?.SystemLog_Txt($"[QueryRecord] 候位記錄 - ID: {waiting.Id}, 餐廳: {restaurantBranch.Name}, 日期: {waiting.CreateDate:yyyy-MM-dd}, 狀態: {waiting.Status ?? "等待中"}, 排隊號碼: {waiting.PositionInLine ?? 0}, CompanyId: {waiting.CompanyId}, BranchId: {waiting.BranchId}");
                        }
                        else
                        {
                            _Log?.SystemLog_Txt($"[QueryRecord] 候位記錄找不到餐廳資訊 - WaitingId: {waiting.Id}, CompanyId: {waiting.CompanyId}, BranchId: {waiting.BranchId}");
                        }
                    }
               }
               
               _Log?.SystemLog_Txt($"[QueryRecord] 成功處理候位記錄數: {waitingRecords.Count}");
           }

           if(branchId.HasValue)
           {
            var selectedGroupId = branchDict.FirstOrDefault(x=>x.Value == branchId.Value).Key;
            if(!string.IsNullOrEmpty(selectedGroupId))
            {
                // 查詢訂位記錄
                 var reserves = await _restaurantContext.MemberReserves
                .Where(r => r.MemberId == memberId && r.BranchId != null && r.CreateDate >= fromDate && r.CreateDate < toDate)
                .OrderByDescending(r => r.CreateDate)
                .ToListAsync();

                _Log?.SystemLog_Txt($"[QueryRecord] 指定分館查詢到訂位記錄總數: {reserves.Count}, GroupId: {selectedGroupId}");

                foreach(var reserve in reserves)
                {
                    // 透過 CompanyId 和 BranchId 查詢 RestaurantBranch
                    if(!string.IsNullOrEmpty(reserve.CompanyId) && !string.IsNullOrEmpty(reserve.BranchId))
                    {
                        var restaurantBranch = await _restaurantContext.RestaurantBranches
                            .FirstOrDefaultAsync(rb => rb.CompanyId == reserve.CompanyId && rb.Id == reserve.BranchId);
                        
                        // 檢查是否屬於選定的分館
                        if(restaurantBranch != null && restaurantBranch.GroupId == selectedGroupId)
                        {
                            reservationRecords.Add(new ReservationRecordModel{
                                ReservationId = (int)reserve.Id,
                                RestaurantId = restaurantBranch.CompanyId.GetHashCode(),
                                RestaurantName = restaurantBranch.Name,
                                RestaurantImageUrl = $"~/IMG/HomePage/{selectedGroupId}/{reserve.BranchId}.jpg",
                                RestaurantLocation = restaurantBranch.Address,
                                RestaurantPhone = restaurantBranch.PhoneNumber,
                                ReservationDate = reserve.Datetime,
                                DayOfWeek = reserve.Datetime.ToString("dddd", new System.Globalization.CultureInfo("zh-TW")),
                                AdultCount = reserve.GroupSize,
                                ChildCount = reserve.NumberOfKid,
                                Status = reserve.Status ?? "已預訂"
                            });
                            
                            // 記錄每筆訂位記錄
                            _Log?.SystemLog_Txt($"[QueryRecord] 訂位記錄(指定分館) - ID: {reserve.Id}, 餐廳: {restaurantBranch.Name}, 日期: {reserve.Datetime:yyyy-MM-dd}, 狀態: {reserve.Status ?? "已預訂"}, CompanyId: {reserve.CompanyId}, BranchId: {reserve.BranchId}");
                        }
                    }
                }
                
                _Log?.SystemLog_Txt($"[QueryRecord] 指定分館成功處理訂位記錄數: {reservationRecords.Count}");
                // 查詢候位記錄
                var waitings = await _restaurantContext.MemberWaitings
                .Where(w => w.MemberId == memberId && w.BranchId != null && w.CreateDate >= fromDate && w.CreateDate < toDate)
                .OrderByDescending(w => w.CreateDate)
                .ToListAsync();

                _Log?.SystemLog_Txt($"[QueryRecord] 指定分館查詢到候位記錄總數: {waitings.Count}, GroupId: {selectedGroupId}");

                foreach(var waiting in waitings)
                {
                    // 透過 CompanyId 和 BranchId 查詢 RestaurantBranch
                    if(!string.IsNullOrEmpty(waiting.CompanyId) && !string.IsNullOrEmpty(waiting.BranchId))
                    {
                        var restaurantBranch = await _restaurantContext.RestaurantBranches
                            .FirstOrDefaultAsync(rb => rb.CompanyId == waiting.CompanyId && rb.Id == waiting.BranchId);
                        
                        // 檢查是否屬於選定的分館
                        if(restaurantBranch != null && restaurantBranch.GroupId == selectedGroupId)
                        {
                            waitingRecords.Add(new WaitingRecordModel{
                                WaitingId = waiting.Id,
                                RestaurantId = restaurantBranch.CompanyId.GetHashCode(),
                                RestaurantName = restaurantBranch.Name,
                                RestaurantImageUrl = $"~/IMG/HomePage/{selectedGroupId}/{waiting.BranchId}.jpg",
                                RestaurantLocation = restaurantBranch.Address,
                                RestaurantPhone = restaurantBranch.PhoneNumber,
                                JoinDate = waiting.CreateDate,
                                AdultCount = waiting.GroupSize,
                                ChildCount = waiting.NumberOfKid,
                                QueueNumber = waiting.PositionInLine ?? 0,
                                Status = waiting.Status ?? "等待中"
                            });
                            
                            // 記錄每筆候位記錄
                            _Log?.SystemLog_Txt($"[QueryRecord] 候位記錄(指定分館) - ID: {waiting.Id}, 餐廳: {restaurantBranch.Name}, 日期: {waiting.CreateDate:yyyy-MM-dd}, 狀態: {waiting.Status ?? "等待中"}, 排隊號碼: {waiting.PositionInLine ?? 0}, CompanyId: {waiting.CompanyId}, BranchId: {waiting.BranchId}");
                        }
                    }
                }
                
                _Log?.SystemLog_Txt($"[QueryRecord] 指定分館成功處理候位記錄數: {waitingRecords.Count}");
            }
           }
           var viewModel = new RecordQueryModel
           {
            Branches = branches,
            SelectedBranchId = branchId,
            ReservationRecords = reservationRecords,
            WaitingRecords = waitingRecords,
            MemberName = memberName,
           };
           
           _Log?.SystemLog_Txt($"[QueryRecord] 查詢完成 - 會員: {memberName}(ID:{memberId}), 訂位記錄: {reservationRecords.Count} 筆, 候位記錄: {waitingRecords.Count} 筆, 分館: {branchId?.ToString() ?? "全部"}");
           
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
                _Log?.SystemLog_Txt($"=== GPS 定位請求開始 ===");
                
                if(!lat.HasValue || !lng.HasValue)
                {
                    return Json(new { success = false, message = "座標參數缺失" });
                }

                var mallGroups = await _restaurantService.GetMallsAsync();
                _Log?.SystemLog_Txt($"取得分館列表成功，共 {mallGroups.Count} 個分館");

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

                foreach(var group in mallGroups)
                {
                    if(branchLocations.ContainsKey(group.GroupId))
                    {
                        var branch = branchLocations[group.GroupId];
                        var distance = CalculateDistance(lat.Value, lng.Value, branch.lat, branch.lng);
                        checkedCount++;   
                        
                        if(distance < minDistance)
                        {
                            minDistance = distance;
                            nearestGroupId = group.GroupId;
                            matchedCount++;
                        }
                    }
                }

                _Log?.SystemLog_Txt($"計算完成 - 檢查了 {checkedCount} 個分館，匹配了 {matchedCount} 個分館");

                if(!string.IsNullOrEmpty(nearestGroupId))
                {
                    return Json(new { success = true, groupId = nearestGroupId, distance = minDistance });
                }

                if(mallGroups.Any())
                {
                    var defaultGroupId = mallGroups.First().GroupId;
                    _Log?.SystemLog_Txt($"GPS 定位未找到匹配分館，使用預設分館: {defaultGroupId}");
                    return Json(new{
                        success = true,
                        groupId = defaultGroupId,
                        distance = 0,
                        message="使用預設分館"
                    });
                }
                return Json(new { success = false, message = "找不到最近的分館" });
            }
            catch(Exception ex)
            {
                _Log?.SystemErrorLog_Txt($"GPS 定位發生異常：{ex.Message}\r\n堆疊追蹤：{ex.StackTrace}");
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
