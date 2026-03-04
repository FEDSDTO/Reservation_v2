using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
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
        private readonly InlineAppsService _inlineAppsService;
        private const string TokenCookieName = "MemberToken";

        public RestaurantController(    
            RestaurantContext restaurantContext,
            MemberContext memberContext,
            RestaurantService restaurantService,
            Func_Log fileLogService,
            InlineAppsService inlineAppsService
            )
        {
            _restaurantContext = restaurantContext;
            _memberContext = memberContext;
            _restaurantService = restaurantService;
            _Log = fileLogService;
            _inlineAppsService = inlineAppsService;
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

                  // ========== 步驟 1: 批次查詢所有 RestaurantBranch ==========
                var companyBranchPairs = allReserves
                  .Where(r => !string.IsNullOrEmpty(r.CompanyId) && !string.IsNullOrEmpty(r.BranchId))
                  .Select(r => new { r.CompanyId, r.BranchId })
                  .Distinct()
                  .ToList();

                var restaurantBranches = new Dictionary<(string CompanyId, string BranchId), Reservation.Models.EFRestaurantModels.RestaurantBranch>();
                if(companyBranchPairs.Count > 0)
                {
                    // 提取 CompanyId 和 BranchId 列表（EF Core 可以轉譯 Contains）
                    var companyIds = companyBranchPairs.Select(p => p.CompanyId).Distinct().ToList();
                    var branchIds = companyBranchPairs.Select(p => p.BranchId).Distinct().ToList();
                    
                    // 先查詢符合條件的 RestaurantBranches
                    var branchesList = await _restaurantContext.RestaurantBranches
                        .Where(rb => companyIds.Contains(rb.CompanyId) && branchIds.Contains(rb.Id))
                        .ToListAsync();
                    
                    // 在記憶體中過濾並建立 Dictionary
                    foreach(var rb in branchesList)
                    {
                        var key = (CompanyId: rb.CompanyId, BranchId: rb.Id);
                        if(companyBranchPairs.Any(p => p.CompanyId == rb.CompanyId && p.BranchId == rb.Id))
                        {
                            restaurantBranches[key] = rb;
                        }
                    }
                }
               

               // ========== 步驟 2: 處理訂位記錄 ==========
               var validReserves = allReserves
                  .Where(r => !string.IsNullOrEmpty(r.CompanyId) && 
                             !string.IsNullOrEmpty(r.CustomerId) &&
                             !string.IsNullOrEmpty(r.ExternalReservationId))
                  .ToList();

                var uniquePairs = validReserves
                  .Select(r => new { r.CompanyId, r.CustomerId })
                  .Distinct()
                  .ToList();
                var statusUpdateDict = new Dictionary<long, string>();
                 // ========== 步驟 3: 呼叫 API 同步狀態（按組呼叫，減少 API 呼叫次數） ==========
                _Log?.SystemLog_Txt($"[QueryRecord] 發現 {uniquePairs.Count} 個不同的 (CompanyId, CustomerId) 組合");
                
                foreach(var pair in uniquePairs)
                {
                    try
                    {
                        var queryApiResult = await _inlineAppsService.GetCustomerReservationAsync(
                            pair.CompanyId,
                            pair.CustomerId,
                            branchId: null!,
                            "booking,waiting");

                        if(queryApiResult.Code == 200 && !string.IsNullOrEmpty(queryApiResult.Data))
                        {
                            var queryResult = Newtonsoft.Json.Linq.JObject.Parse(queryApiResult.Data);
                            var reservations = queryResult["reservations"] as Newtonsoft.Json.Linq.JArray;

                            if(reservations != null)
                            {
                                var apiDict = new Dictionary<string, Newtonsoft.Json.Linq.JObject>();
                                
                                foreach(var reservation in reservations)
                                {
                                    var apiId = reservation["id"]?.ToString();
                                    if(!string.IsNullOrEmpty(apiId))
                                    {
                                        var apiReservationObj = reservation as Newtonsoft.Json.Linq.JObject;
                                        if(apiReservationObj != null)
                                        {
                                            apiDict[apiId] = apiReservationObj;
                                        }
                                    }
                                }
                                
                                var reservesInGroup = validReserves
                                    .Where(r => r.CompanyId == pair.CompanyId && r.CustomerId == pair.CustomerId)
                                    .ToList();

                                foreach(var reserve in reservesInGroup)
                                {
                                    if(apiDict.TryGetValue(reserve.ExternalReservationId, out var apiReservation))
                                    {
                                        var apiState = apiReservation["state"]?.ToString() ?? string.Empty;
                                        var apiType = apiReservation["type"]?.ToString() ?? string.Empty;

                                        string status = "已預定";
                                        if(apiType == "booking" || apiType == "waiting" || apiType == "walk-in")
                                        {
                                            status = apiState switch
                                            {
                                                "waiting" => "已預定",
                                                "seated" => "已入座",
                                                "cancelled" => "已取消",
                                                _ => "已預定"
                                            };
                                        }

                                        statusUpdateDict[reserve.Id] = status;
                                    }
                                }

                                _Log?.SystemLog_Txt($"[QueryRecord] API 回應包含 {reservations.Count} 筆記錄，成功匹配 {reservesInGroup.Count(r => statusUpdateDict.ContainsKey(r.Id))} 筆 - CompanyId: {pair.CompanyId}, CustomerId: {pair.CustomerId}");
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        _Log?.SystemErrorLog_Txt($"[QueryRecord] API 呼叫失敗 - CompanyId: {pair.CompanyId}, CustomerId: {pair.CustomerId}, Error: {ex.Message}");
                    }
                }

                // ========== 步驟 4: 批次更新資料庫 ==========
                bool hasChanges = false;
                foreach(var reserve in allReserves)
                {
                    if(statusUpdateDict.TryGetValue(reserve.Id, out var newStatus))
                    {
                        if(reserve.Status != newStatus)
                        {
                            reserve.Status = newStatus;
                            hasChanges = true;
                        }
                    }
                }

                if(hasChanges)
                {
                    await _restaurantContext.SaveChangesAsync();
                    _Log?.SystemLog_Txt($"[QueryRecord] 批次更新資料庫完成，共更新 {statusUpdateDict.Count} 筆記錄");
                }

                // ========== 步驟 5: 建立 ViewModel ==========
                foreach(var reserve in allReserves)
                {
                    if(!string.IsNullOrEmpty(reserve.CompanyId) && !string.IsNullOrEmpty(reserve.BranchId))
                    {
                        var key = (CompanyId: reserve.CompanyId, BranchId: reserve.BranchId);
                        if(restaurantBranches.TryGetValue(key, out var RestaurantBranch))
                        {
                            var groupId = RestaurantBranch.GroupId ?? "";
                            string currentStatus = statusUpdateDict.TryGetValue(reserve.Id, out var syncedStatus) 
                                ? syncedStatus 
                                : (reserve.Status ?? "已預訂");

                            reservationRecords.Add(new ReservationRecordModel{
                                ReservationId = (int)reserve.Id,
                                RestaurantId = RestaurantBranch.CompanyId.GetHashCode(),
                                RestaurantName = RestaurantBranch.Name,
                                RestaurantImageUrl = _restaurantService.GetRestaurantImageUrl(groupId, reserve.BranchId),
                                RestaurantLocation = RestaurantBranch.Address,
                                RestaurantPhone = RestaurantBranch.PhoneNumber,
                                ReservationDate = reserve.Datetime,
                                DayOfWeek = reserve.Datetime.ToString("dddd", new System.Globalization.CultureInfo("zh-TW")),
                                AdultCount = reserve.GroupSize,
                                ChildCount = reserve.NumberOfKid,
                                Status = currentStatus
                            });
                        }
                    }
                }
               
               _Log?.SystemLog_Txt($"[QueryRecord] 成功處理訂位記錄數: {reservationRecords.Count}");
               

               // 查詢所有候位記錄
               var allWaitings = await _restaurantContext.MemberWaitings
               .Where(w=>w.MemberId == memberId && w.CreateDate >= fromDate && w.CreateDate < toDate )
               .OrderByDescending(w=>w.CreateDate)
               .ToListAsync();

               _Log?.SystemLog_Txt($"[QueryRecord] 查詢到候位紀錄總數:{allWaitings.Count}");

                //========== 批次查詢候位記錄的 RestaurantBranch ==========
                var waitingCompanyBranchPairs = allWaitings
                .Where(w=>!string.IsNullOrEmpty(w.CompanyId) && !string.IsNullOrEmpty(w.BranchId))
                .Select(w=>new {w.CompanyId,w.BranchId})
                .Distinct()
                .ToList();

                var waitingRestaurantBranchesDict = new Dictionary<(string CompanyId, string BranchId), Reservation.Models.EFRestaurantModels.RestaurantBranch>();
                if(waitingCompanyBranchPairs.Count >0)
                {
                    var companyIds = waitingCompanyBranchPairs.Select(p=>p.CompanyId).Distinct().ToList();
                    var branchIds = waitingCompanyBranchPairs.Select(p=>p.BranchId).Distinct().ToList();

                    var branchesList2 = await _restaurantContext.RestaurantBranches
                    .Where(rb => companyIds.Contains(rb.CompanyId) && branchIds.Contains(rb.Id)).ToListAsync();

                    foreach(var rb in branchesList2)
                    {
                        var key = (CompanyId: rb.CompanyId, BranchId: rb.Id);
                        if(waitingCompanyBranchPairs.Any(p => p.CompanyId == rb.CompanyId && p.BranchId == rb.Id))
                        {
                            waitingRestaurantBranchesDict[key] = rb;
                        }
                    }
                }

                // ========== 候位記錄 API 同步狀態 ==========
                var validWaitings = allWaitings
                .Where(w=>!string.IsNullOrEmpty(w.CompanyId) &&
                !string.IsNullOrEmpty(w.CustomerId) &&
                !string.IsNullOrEmpty(w.Id))
                .ToList();

                var uniqueWaitingPairs = validWaitings
                .Select(w=>new{w.CompanyId,w.CustomerId})
                .Distinct()
                .ToList();

               var waitingStatusUpdateDict = new Dictionary<string, (string Status, int? PositionInLine)>();

               _Log?.SystemLog_Txt($"[QueryRecord] 發現 {uniqueWaitingPairs.Count} 個不同的 (CompanyId, CustomerId) 組合");

               foreach(var pair in uniqueWaitingPairs)
               {
                  try
                  {
                    var queryApiResult = await _inlineAppsService.GetCustomerReservationAsync(
                    pair.CompanyId,
                    pair.CustomerId,
                    branchId: null!,
                    "booking,waiting");

                    if(queryApiResult.Code == 200 && !string.IsNullOrEmpty(queryApiResult.Data))
                    {
                        var queryResult = Newtonsoft.Json.Linq.JObject.Parse(queryApiResult.Data);
                        var reservations = queryResult["reservations"] as Newtonsoft.Json.Linq.JArray;

                        if(reservations != null)
                        {
                            var apiDict = new Dictionary<string, Newtonsoft.Json.Linq.JObject>();
                           
                           foreach(var reservation in reservations)
                           {
                                var apiId = reservation["id"]?.ToString();
                                var apiType = reservation["type"]?.ToString()?? string.Empty;

                                if(!string.IsNullOrEmpty(apiId) && apiType == "waiting")
                                {
                                    var apiReservationObj = reservation as Newtonsoft.Json.Linq.JObject;
                                    if(apiReservationObj != null)
                                    {
                                        apiDict[apiId] = apiReservationObj;
                                    }
                                }
                           }

                           var waitingsInGroup = validWaitings
                           .Where(w=>w.CompanyId == pair.CompanyId && w.CustomerId == pair.CustomerId)
                           .ToList();

                           foreach(var waiting in waitingsInGroup)
                           {
                                if(apiDict.TryGetValue(waiting.Id, out var apiReservation))
                                {
                                    var apiState = apiReservation["state"]?.ToString() ?? string.Empty;
                                    var positionInLine = apiReservation["positionInLine"]?.Value<int?>();
                                
                                    string status = "等待中";
                                       if(apiState == "seated")
                                       {
                                           status = "已入座";
                                       }
                                       else if(apiState == "cancelled")
                                       {
                                           status = "已取消";
                                       }
                                       else if(apiState == "waiting")
                                       {
                                           status = "等待中";
                                       }
                                       
                                       waitingStatusUpdateDict[waiting.Id] = (status, positionInLine);
                                }
                           }
                        }
                    }
                  }
                  catch(Exception ex)
                  {
                     _Log?.SystemErrorLog_Txt($"[QueryRecord] 候位 API 呼叫失敗 - CompanyId: {pair.CompanyId}, CustomerId: {pair.CustomerId}, Error: {ex.Message}");
                  }
               }

                // ========== 批次更新候位記錄資料庫 ==========
                bool hasWaitingChanges = false;

                foreach(var waiting in allWaitings)
                {
                    if(waitingStatusUpdateDict.TryGetValue(waiting.Id,out var updateinfo))
                    {
                        bool needUpdate = false;
                        if(waiting.Status !=updateinfo.Status)
                        {
                            waiting.Status = updateinfo.Status;
                            needUpdate = true;
                        }
                        if(waiting.PositionInLine != updateinfo.PositionInLine)
                        {
                            waiting.PositionInLine = updateinfo.PositionInLine;
                            needUpdate = true;
                        }
                        if(needUpdate)
                        {
                            hasWaitingChanges = true;
                        }
                    }
                }

                if(hasWaitingChanges)
                {
                    await _restaurantContext.SaveChangesAsync();
                    _Log?.SystemLog_Txt($"[QueryRecord] 批次更新候位記錄資料庫完成");
                }

                 // ========== 建立候位記錄 ViewModel ==========

                 foreach(var waiting in allWaitings)
                 {
                    if(!string.IsNullOrEmpty(waiting.CompanyId) && !string.IsNullOrEmpty(waiting.BranchId))
                    {
                        var key = (CompanyId: waiting.CompanyId, BranchId: waiting.BranchId);
                        if(waitingRestaurantBranchesDict.TryGetValue(key, out var RestaurantBranch))
                        {
                            var groupId = RestaurantBranch.GroupId ?? "";

                            string currentStatus = waitingStatusUpdateDict.TryGetValue(waiting.Id, out var syncedInfo) 
                                ? syncedInfo.Status 
                                : (waiting.Status ?? "等待中");
                            int queueNumber = waitingStatusUpdateDict.TryGetValue(waiting.Id, out var syncedInfo2) 
                                ? (syncedInfo2.PositionInLine ?? 0)
                                : (waiting.PositionInLine ?? 0);

                            waitingRecords.Add(new Reservation.Models.ViewModels.WaitingRecordModel{
                                WaitingId = waiting.Id,
                                RestaurantId = RestaurantBranch.CompanyId.GetHashCode(),
                                RestaurantName = RestaurantBranch.Name,
                                RestaurantImageUrl = _restaurantService.GetRestaurantImageUrl(groupId, waiting.BranchId),
                                RestaurantLocation = RestaurantBranch.Address,
                                RestaurantPhone = RestaurantBranch.PhoneNumber,
                                JoinDate = waiting.CreateDate,
                                AdultCount = waiting.GroupSize,
                                ChildCount = waiting.NumberOfKid,
                                QueueNumber = queueNumber,
                                Status = currentStatus
                            });
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

                // ========== 批次查詢 RestaurantBranch ==========
                var reserveCompanyBranchPairs = reserves
                    .Where(r => !string.IsNullOrEmpty(r.CompanyId) && !string.IsNullOrEmpty(r.BranchId))
                    .Select(r => new { r.CompanyId, r.BranchId })
                    .Distinct()
                    .ToList();

                var reserveRestaurantBranchesDict = new Dictionary<(string CompanyId, string BranchId), Reservation.Models.EFRestaurantModels.RestaurantBranch>();
                if(reserveCompanyBranchPairs.Count > 0)
                {
                    var companyIds = reserveCompanyBranchPairs.Select(p => p.CompanyId).Distinct().ToList();
                    var branchIds = reserveCompanyBranchPairs.Select(p => p.BranchId).Distinct().ToList();
                    
                    var branchesList3 = await _restaurantContext.RestaurantBranches
                        .Where(rb => companyIds.Contains(rb.CompanyId) && branchIds.Contains(rb.Id))
                        .ToListAsync();
                    
                    foreach(var rb in branchesList3)
                    {
                        var key = (CompanyId: rb.CompanyId, BranchId: rb.Id);
                        if(reserveCompanyBranchPairs.Any(p => p.CompanyId == rb.CompanyId && p.BranchId == rb.Id))
                        {
                            reserveRestaurantBranchesDict[key] = rb;
                        }
                    }
                }

                // ========== 過濾屬於選定分館的記錄 ==========
                var filteredReserves = reserves
                    .Where(r => !string.IsNullOrEmpty(r.CompanyId) && !string.IsNullOrEmpty(r.BranchId))
                    .Where(r => reserveRestaurantBranchesDict.ContainsKey((CompanyId: r.CompanyId, BranchId: r.BranchId)))
                    .Where(r => reserveRestaurantBranchesDict[(CompanyId: r.CompanyId, BranchId: r.BranchId)].GroupId == selectedGroupId)
                    .ToList();

                // ========== API 同步狀態 ==========
                var validReserves2 = filteredReserves
                    .Where(r => !string.IsNullOrEmpty(r.CompanyId) && 
                               !string.IsNullOrEmpty(r.CustomerId) &&
                               !string.IsNullOrEmpty(r.ExternalReservationId))
                    .ToList();

                var uniquePairs2 = validReserves2
                    .Select(r => new { r.CompanyId, r.CustomerId })
                    .Distinct()
                    .ToList();

                var statusUpdateDict2 = new Dictionary<long, string>();

                foreach(var pair in uniquePairs2)
                {
                    try
                    {
                        var queryApiResult = await _inlineAppsService.GetCustomerReservationAsync(
                            pair.CompanyId,
                            pair.CustomerId,
                            branchId: null!,
                            "booking,waiting");
                        
                        if(queryApiResult.Code == 200 && !string.IsNullOrEmpty(queryApiResult.Data))
                        {
                            var queryResult = Newtonsoft.Json.Linq.JObject.Parse(queryApiResult.Data);
                            var reservations = queryResult["reservations"] as Newtonsoft.Json.Linq.JArray;
                            
                            if(reservations != null)
                            {
                                var apiDict = new Dictionary<string, Newtonsoft.Json.Linq.JObject>();
                                foreach(var reservation in reservations)
                                {
                                    var apiId = reservation["id"]?.ToString();
                                    if(!string.IsNullOrEmpty(apiId))
                                    {
                                        var apiReservationObj = reservation as Newtonsoft.Json.Linq.JObject;
                                        if(apiReservationObj != null)
                                        {
                                            apiDict[apiId] = apiReservationObj;
                                        }
                                    }
                                }
                                
                                var reservesInGroup = validReserves2
                                    .Where(r => r.CompanyId == pair.CompanyId && r.CustomerId == pair.CustomerId)
                                    .ToList();
                                
                                foreach(var reserve in reservesInGroup)
                                {
                                    if(apiDict.TryGetValue(reserve.ExternalReservationId, out var apiReservation))
                                    {
                                        var apiState = apiReservation["state"]?.ToString() ?? string.Empty;
                                        var apiType = apiReservation["type"]?.ToString() ?? string.Empty;
                                        
                                        string status = "已預定";
                                        if(apiType == "booking" || apiType == "waiting" || apiType == "walk-in")
                                        {
                                            status = apiState switch
                                            {
                                                "waiting" => "已預定",
                                                "seated" => "已入座",
                                                "cancelled" => "已取消",
                                                _ => "已預定"
                                            };
                                        }
                                        
                                        statusUpdateDict2[reserve.Id] = status;
                                    }
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        _Log?.SystemErrorLog_Txt($"[QueryRecord] API 呼叫失敗(指定分館) - CompanyId: {pair.CompanyId}, CustomerId: {pair.CustomerId}, Error: {ex.Message}");
                    }
                }

                // ========== 批次更新資料庫 ==========
                bool hasChanges2 = false;
                foreach(var reserve in filteredReserves)
                {
                    if(statusUpdateDict2.TryGetValue(reserve.Id, out var newStatus))
                    {
                        if(reserve.Status != newStatus)
                        {
                            reserve.Status = newStatus;
                            hasChanges2 = true;
                        }
                    }
                }

                if(hasChanges2)
                {
                    await _restaurantContext.SaveChangesAsync();
                }

                // ========== 建立 ViewModel ==========
                foreach(var reserve in filteredReserves)
                {
                    var key = (CompanyId: reserve.CompanyId, BranchId: reserve.BranchId);
                    if(reserveRestaurantBranchesDict.TryGetValue(key, out var restaurantBranch))
                    {
                        string currentStatus = statusUpdateDict2.TryGetValue(reserve.Id, out var syncedStatus) 
                            ? syncedStatus 
                            : (reserve.Status ?? "已預訂");

                        reservationRecords.Add(new ReservationRecordModel{
                            ReservationId = (int)reserve.Id,
                            RestaurantId = restaurantBranch.CompanyId.GetHashCode(),
                            RestaurantName = restaurantBranch.Name,
                            RestaurantImageUrl = _restaurantService.GetRestaurantImageUrl(selectedGroupId, reserve.BranchId),
                            RestaurantLocation = restaurantBranch.Address,
                            RestaurantPhone = restaurantBranch.PhoneNumber,
                            ReservationDate = reserve.Datetime,
                            DayOfWeek = reserve.Datetime.ToString("dddd", new System.Globalization.CultureInfo("zh-TW")),
                            AdultCount = reserve.GroupSize,
                            ChildCount = reserve.NumberOfKid,
                            Status = currentStatus
                        });
                    }
                }
                
                _Log?.SystemLog_Txt($"[QueryRecord] 指定分館成功處理訂位記錄數: {reservationRecords.Count}");
                // 查詢候位記錄
                var waitings = await _restaurantContext.MemberWaitings
                .Where(w => w.MemberId == memberId && w.BranchId != null && w.CreateDate >= fromDate && w.CreateDate < toDate)
                .OrderByDescending(w => w.CreateDate)
                .ToListAsync();

                _Log?.SystemLog_Txt($"[QueryRecord] 指定分館查詢到候位記錄總數: {waitings.Count}, GroupId: {selectedGroupId}");

                // ========== 批次查詢候位記錄的 RestaurantBranch ==========
                var waitingCompanyBranchPairs2 = waitings
                    .Where(w => !string.IsNullOrEmpty(w.CompanyId) && !string.IsNullOrEmpty(w.BranchId))
                    .Select(w => new { w.CompanyId, w.BranchId })
                    .Distinct()
                    .ToList();

                var waitingRestaurantBranchesDict2 = new Dictionary<(string CompanyId, string BranchId), Reservation.Models.EFRestaurantModels.RestaurantBranch>();
                if(waitingCompanyBranchPairs2.Count > 0)
                {
                    var companyIds = waitingCompanyBranchPairs2.Select(p => p.CompanyId).Distinct().ToList();
                    var branchIds = waitingCompanyBranchPairs2.Select(p => p.BranchId).Distinct().ToList();
                    
                    var branchesList4 = await _restaurantContext.RestaurantBranches
                        .Where(rb => companyIds.Contains(rb.CompanyId) && branchIds.Contains(rb.Id))
                        .ToListAsync();
                    
                    foreach(var rb in branchesList4)
                    {
                        var key = (CompanyId: rb.CompanyId, BranchId: rb.Id);
                        if(waitingCompanyBranchPairs2.Any(p => p.CompanyId == rb.CompanyId && p.BranchId == rb.Id))
                        {
                            waitingRestaurantBranchesDict2[key] = rb;
                        }
                    }
                }

                // ========== 過濾屬於選定分館的候位記錄 ==========
                var filteredWaitings = waitings
                    .Where(w => !string.IsNullOrEmpty(w.CompanyId) && !string.IsNullOrEmpty(w.BranchId))
                    .Where(w => waitingRestaurantBranchesDict2.ContainsKey((CompanyId: w.CompanyId, BranchId: w.BranchId)))
                    .Where(w => waitingRestaurantBranchesDict2[(CompanyId: w.CompanyId, BranchId: w.BranchId)].GroupId == selectedGroupId)
                    .ToList();

                // ========== 候位記錄 API 同步狀態 ==========
                var validWaitings2 = filteredWaitings
                    .Where(w => !string.IsNullOrEmpty(w.CompanyId) && 
                               !string.IsNullOrEmpty(w.CustomerId) &&
                               !string.IsNullOrEmpty(w.Id))
                    .ToList();

                var uniqueWaitingPairs2 = validWaitings2
                    .Select(w => new { w.CompanyId, w.CustomerId })
                    .Distinct()
                    .ToList();

                var waitingStatusUpdateDict2 = new Dictionary<string, (string Status, int? PositionInLine)>();

                foreach(var pair in uniqueWaitingPairs2)
                {
                    try
                    {
                        var queryApiResult = await _inlineAppsService.GetCustomerReservationAsync(
                            pair.CompanyId,
                            pair.CustomerId,
                            branchId: null!,
                            "booking,waiting");
                        
                        if(queryApiResult.Code == 200 && !string.IsNullOrEmpty(queryApiResult.Data))
                        {
                            var queryResult = Newtonsoft.Json.Linq.JObject.Parse(queryApiResult.Data);
                            var reservations = queryResult["reservations"] as Newtonsoft.Json.Linq.JArray;
                            
                            if(reservations != null)
                            {
                                var apiDict = new Dictionary<string, Newtonsoft.Json.Linq.JObject>();
                                
                                foreach(var reservation in reservations)
                                {
                                    var apiId = reservation["id"]?.ToString();
                                    var apiType = reservation["type"]?.ToString() ?? string.Empty;
                                    
                                    if(!string.IsNullOrEmpty(apiId) && apiType == "waiting")
                                    {
                                        var apiReservationObj = reservation as Newtonsoft.Json.Linq.JObject;
                                        if(apiReservationObj != null)
                                        {
                                            apiDict[apiId] = apiReservationObj;
                                        }
                                    }
                                }
                                
                                var waitingsInGroup = validWaitings2
                                    .Where(w => w.CompanyId == pair.CompanyId && w.CustomerId == pair.CustomerId)
                                    .ToList();
                                
                                foreach(var waiting in waitingsInGroup)
                                {
                                    if(apiDict.TryGetValue(waiting.Id, out var apiReservation))
                                    {
                                        var apiState = apiReservation["state"]?.ToString() ?? string.Empty;
                                        var positionInLine = apiReservation["positionInLine"]?.Value<int?>();
                                        
                                        string status = "等待中";
                                        if(apiState == "seated")
                                        {
                                            status = "已入座";
                                        }
                                        else if(apiState == "cancelled")
                                        {
                                            status = "已取消";
                                        }
                                        else if(apiState == "waiting")
                                        {
                                            status = "等待中";
                                        }
                                        
                                        waitingStatusUpdateDict2[waiting.Id] = (status, positionInLine);
                                    }
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        _Log?.SystemErrorLog_Txt($"[QueryRecord] 候位 API 呼叫失敗(指定分館) - CompanyId: {pair.CompanyId}, CustomerId: {pair.CustomerId}, Error: {ex.Message}");
                    }
                }

                // ========== 批次更新候位記錄資料庫 ==========
                bool hasWaitingChanges2 = false;
                foreach(var waiting in filteredWaitings)
                {
                    if(waitingStatusUpdateDict2.TryGetValue(waiting.Id, out var updateInfo))
                    {
                        bool needUpdate = false;
                        
                        if(waiting.Status != updateInfo.Status)
                        {
                            waiting.Status = updateInfo.Status;
                            needUpdate = true;
                        }
                        
                        if(waiting.PositionInLine != updateInfo.PositionInLine)
                        {
                            waiting.PositionInLine = updateInfo.PositionInLine;
                            needUpdate = true;
                        }
                        
                        if(needUpdate)
                        {
                            hasWaitingChanges2 = true;
                        }
                    }
                }

                if(hasWaitingChanges2)
                {
                    await _restaurantContext.SaveChangesAsync();
                    _Log?.SystemLog_Txt($"[QueryRecord] 批次更新候位記錄資料庫完成(指定分館)");
                }

                // ========== 建立候位記錄 ViewModel ==========
                foreach(var waiting in filteredWaitings)
                {
                    var key = (CompanyId: waiting.CompanyId, BranchId: waiting.BranchId);
                    if(waitingRestaurantBranchesDict2.TryGetValue(key, out var restaurantBranch))
                    {
                        string currentStatus = waitingStatusUpdateDict2.TryGetValue(waiting.Id, out var syncedInfo) 
                            ? syncedInfo.Status 
                            : (waiting.Status ?? "等待中");
                        int queueNumber = waitingStatusUpdateDict2.TryGetValue(waiting.Id, out var syncedInfo2) 
                            ? (syncedInfo2.PositionInLine ?? 0)
                            : (waiting.PositionInLine ?? 0);
                        
                        waitingRecords.Add(new WaitingRecordModel{
                            WaitingId = waiting.Id,
                            RestaurantId = restaurantBranch.CompanyId.GetHashCode(),
                            RestaurantName = restaurantBranch.Name,
                            RestaurantImageUrl = _restaurantService.GetRestaurantImageUrl(selectedGroupId, waiting.BranchId),
                            RestaurantLocation = restaurantBranch.Address,
                            RestaurantPhone = restaurantBranch.PhoneNumber,
                            JoinDate = waiting.CreateDate,
                            AdultCount = waiting.GroupSize,
                            ChildCount = waiting.NumberOfKid,
                            QueueNumber = queueNumber,
                            Status = currentStatus
                        });
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
