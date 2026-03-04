using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Reservation.Models.EFRestaurantModels;
using Reservation.Models.ViewModels;
using Reservation.Service;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Reservation.Controllers
{
    public class ReservationController : Controller
    {
        private readonly RestaurantContext _restaurantContext;
        private readonly RestaurantService _restaurantService;
        private readonly InlineAppsService _inlineAppsService;
        private readonly Func_Log _Log;

        public ReservationController(
            RestaurantContext restaurantContext,
            RestaurantService restaurantService,
            InlineAppsService inlineAppsService,
            Func_Log fileLogService)
        {
            _restaurantContext = restaurantContext;
            _restaurantService = restaurantService;
            _inlineAppsService = inlineAppsService;
            _Log = fileLogService;
        }

        /// <summary>
        /// 訂位頁面 
        /// </summary>
        public async Task<IActionResult> Index(string id, string companyId, string branchId)
        {
            // id = groupId, companyId, branchId 來自 URL
            if(string.IsNullOrEmpty(id) || string.IsNullOrEmpty(companyId) || string.IsNullOrEmpty(branchId))
            {
                _Log?.SystemErrorLog_Txt($"訂位頁面參數缺失 - id: {id}, companyId: {companyId}, branchId: {branchId}");
                return RedirectToAction("Index", "Restaurant");
            }

            try
            {
                // ========== 步驟 1：取得分館列表 ==========
                var mallGroups = await _restaurantService.GetMallsAsync();
                var branchDict = new Dictionary<string, int>();
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
                }

                // ========== 步驟 2：從資料庫讀取餐廳基本資訊 ==========
                var restaurant = await _restaurantService.GetRestaurantDetailAsync(id, branchId);
                if(restaurant == null)
                {
                    _Log?.SystemErrorLog_Txt($"找不到餐廳資料 - GroupId: {id}, BranchId: {branchId}");
                    return RedirectToAction("Index", "Restaurant");
                }

                // ========== 步驟 3：呼叫 Framework API 取得可訂位時段 ==========
                var today = DateTime.Now.ToString("yyyy-MM-dd");
                var apiResult = await _inlineAppsService.GetBranchAsync(
                    id, companyId, branchId, 
                    "booking",  // type=booking 取得可訂位資訊
                    size: 2,
                    date: DateTime.UtcNow.Date);

                var availableTimeSlots = new List<string>();
                var menus = new List<MenuModel>();
                string openingHours = "10:00-22:00"; // 預設值
                int minGroupSize =1;
                int maxGroupSize =8;

                if(apiResult.Code == 200)
                {
                    try
                    {
                        var data = JObject.Parse(apiResult.Data);
                        
                        var apiDataPreview = apiResult.Data.Length > 500 ? apiResult.Data.Substring(0, 500) + "..." : apiResult.Data;
                        _Log?.SystemLog_Txt($"[API Response Preview] API 回應預覽: {apiDataPreview}");
                        
                        // 解析可訂位時段（對應 Framework 的 GetBookingTime）
                        var bookingInfo = data.GetValue("bookingInfo");
                        if(bookingInfo != null)
                        {
                            var defaultBooking = bookingInfo.Value<JObject>("default");
                            
                            // 先檢查 defaultBooking 是否存在
                            if(defaultBooking != null)
                            {
                                // 嘗試從 defaultBooking 取得人數限制（使用舊版欄位名稱）
                                var minSize = defaultBooking.GetValue("minBookingGroupSize");
                                var maxSize = defaultBooking.GetValue("maxBookingGroupSize");

                                if(minSize != null)
                                {
                                    minGroupSize = minSize.Value<int>();
                                    _Log?.SystemLog_Txt($"從 bookingInfo.default 取得 minBookingGroupSize: {minGroupSize}");
                                }
                                
                                if(maxSize != null)
                                {
                                    maxGroupSize = maxSize.Value<int>();
                                    _Log?.SystemLog_Txt($"從 bookingInfo.default 取得 maxBookingGroupSize: {maxGroupSize}");
                                }
                                
                                // 處理時段資料
                                var todayBooking = defaultBooking.Value<JObject>(today);
                                if(todayBooking != null)
                                {
                                    var timeProperties = todayBooking.Properties();
                                    foreach(var prop in timeProperties)
                                    {
                                        if(prop.Value.ToString() == "open")
                                        {
                                            var normalizedTime = NormalizeTimeSlot(prop.Name);
                                            availableTimeSlots.Add(normalizedTime);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                _Log?.SystemErrorLog_Txt($"bookingInfo.default 為 null，嘗試從其他位置尋找人數限制");
                            }
                            
                            // 根據預設餐期過濾時間
                            availableTimeSlots = FilterTimeSlots(availableTimeSlots, "中午");

                            availableTimeSlots = FilterTimeSlots(availableTimeSlots,"中午");
                            
                            // 如果還是預設值，嘗試從 bookingInfo 的直接屬性取得（使用舊版欄位名稱）
                            if(minGroupSize == 1 && maxGroupSize == 8)
                            {
                                var bookingInfoObj = bookingInfo as JObject;
                                if(bookingInfoObj != null)
                                {
                                    var bookingMinSize = bookingInfoObj.GetValue("minBookingGroupSize");
                                    var bookingMaxSize = bookingInfoObj.GetValue("maxBookingGroupSize");
                                    
                                    if(bookingMinSize != null)
                                    {
                                        minGroupSize = bookingMinSize.Value<int>();
                                        _Log?.SystemLog_Txt($"從 bookingInfo 直接屬性取得 minBookingGroupSize: {minGroupSize}");
                                    }
                                    
                                    if(bookingMaxSize != null)
                                    {
                                        maxGroupSize = bookingMaxSize.Value<int>();
                                        _Log?.SystemLog_Txt($"從 bookingInfo 直接屬性取得 maxBookingGroupSize: {maxGroupSize}");
                                    }
                                }
                            }
                        }
                        else
                        {
                            _Log?.SystemErrorLog_Txt($"API 回應中未找到 bookingInfo");
                        }
                        
                        // 如果還是預設值，嘗試從根層級取得（使用舊版欄位名稱）
                        if(minGroupSize == 1 && maxGroupSize == 8)
                        {
                            var rootMinSize = data.GetValue("minBookingGroupSize");
                            var rootMaxSize = data.GetValue("maxBookingGroupSize");
                            
                            if(rootMinSize != null)
                            {
                                minGroupSize = rootMinSize.Value<int>();
                                _Log?.SystemLog_Txt($"從根層級取得 minBookingGroupSize: {minGroupSize}");
                            }
                            
                            if(rootMaxSize != null)
                            {
                                maxGroupSize = rootMaxSize.Value<int>();
                                _Log?.SystemLog_Txt($"從根層級取得 maxBookingGroupSize: {maxGroupSize}");
                            }
                        }
                        
                        // 如果仍然無法取得，記錄警告
                        if(minGroupSize == 1 && maxGroupSize == 8)
                        {
                            _Log?.SystemErrorLog_Txt($"無法從 API 取得人數限制，使用預設值 - minGroupSize: {minGroupSize}, maxGroupSize: {maxGroupSize}");
                        }

                        // 取得菜單圖片
                        var menusArray = data.Value<JArray>("menus");
                        if(menusArray != null && menusArray.Count > 0)
                        {
                            var menuUrls = menusArray.ToObject<List<string>>();
                            if(menuUrls != null)
                            {
                                int menuId = 1;
                                foreach(var menuUrl in menuUrls)
                                {
                                    if(!string.IsNullOrEmpty(menuUrl))
                                    {
                                        menus.Add(new MenuModel
                                        {
                                            Id = menuId++,
                                            RestaurantId = restaurant.id.GetHashCode(),
                                            ImageUrl = menuUrl
                                        });
                                    }
                                }
                            }
                        }

                        // 取得營業時間（可選）
                        var openingTimes = data.Value<JArray>("openingTimes");
                        if(openingTimes != null && openingTimes.Count > 0)
                        {
                            var todayOpening = openingTimes.FirstOrDefault(ot =>
                                ot.Value<string>("date") == today) as JObject;

                            if(todayOpening != null)
                            {
                                var times = todayOpening.Value<JArray>("times");
                                if(times != null && times.Count > 0)
                                {
                                    var firstTime = times[0] as JObject;
                                    var start = firstTime?.Value<string>("start") ?? "10:00";
                                    var end = firstTime?.Value<string>("end") ?? "22:00";
                                    openingHours = $"{start}-{end}";
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        _Log?.SystemErrorLog_Txt($"解析 API 回應失敗: {ex.Message}");
                    }
                }
                else
                {
                    _Log?.SystemErrorLog_Txt($"API 請求失敗 - Code: {apiResult.Code}, Msg: {apiResult.Msg}");
                }

                // ========== 步驟 4：建立 ViewModel ==========
                var restaurantImageUrl = _restaurantService.GetRestaurantImageUrl(id, branchId);
                var selectedBranchId = branchDict.ContainsKey(id) ? branchDict[id] : 1;

                var restaurantInfo = new Reservation.Models.ViewModels.RestaurantInfoModel
                {
                    Id = selectedBranchId,
                    Name = restaurant.Name,
                    ImageUrl = restaurantImageUrl,
                    Location = restaurant.Address,
                    Phone = restaurant.PhoneNumber,
                    OpeningHours = openingHours,
                    BranchId = selectedBranchId,
                    CategoryId = 1,
                    IsPopular = false,
                    IsNew = false
                };

                var selectedBranch = branches.FirstOrDefault(b => b.Id == selectedBranchId) ?? branches.FirstOrDefault();

                var viewModel = new ReservationModel
                {
                    Restaurant = restaurantInfo,
                    Branch = selectedBranch ?? new BranchModel(),
                    SelectedDate = DateTime.Today,
                    AdultCount = 2,
                    ChildCount = 0,
                    SelectedMealPeriod = "中午",
                    AvailableTimeSlots = availableTimeSlots.OrderBy(t => t).ToList(),
                    Menus = menus,
                    MinGroupSize = minGroupSize,
                    MaxGroupSize = maxGroupSize
                };

                // 儲存到 ViewBag 供 View 使用
                ViewBag.GroupId = id;
                ViewBag.CompanyId = companyId;
                ViewBag.BranchId = branchId;

                return View(viewModel);
            }
            catch(Exception ex)
            {
                _Log?.SystemErrorLog_Txt($"訂位頁面發生錯誤: {ex.Message}\r\nStackTrace: {ex.StackTrace}");
                TempData["ErrorMsg"] = "載入餐廳資料時發生錯誤";
                return RedirectToAction("Index", "Restaurant", new { groupId = id });
            }
        }

        /// <summary>
        /// 處理訂位表單提交 - 跳轉到確認頁
        /// </summary>
        [HttpPost]
        public IActionResult Index(ReservationModel model)
        {

            if(string.IsNullOrWhiteSpace(model.SelectedTimeSlot))
            {
                ModelState.AddModelError("SelectedTimeSlot","請選擇訂位時間");
            }
            if (!ModelState.IsValid)
            {
                model.AvailableTimeSlots = GetAvailableTimeSlots(model.SelectedMealPeriod, model.SelectedDate ?? DateTime.Today);
                model.Menus = new List<MenuModel>();
                return View(model);
            }

            // 從 Form 或 ViewBag 取得參數
            var groupId = Request.Form["GroupId"].ToString();
            var companyId = Request.Form["CompanyId"].ToString();
            var branchId = Request.Form["BranchId"].ToString();

            return RedirectToAction("Confirm", "Reservation", new
            {
                id = groupId,
                companyId = companyId,
                branchId = branchId,
                selectedDate = model.SelectedDate ?? DateTime.Today,
                adultCount = model.AdultCount,
                childCount = model.ChildCount,
                selectedMealPeriod = model.SelectedMealPeriod,
                selectedTimeSlot = model.SelectedTimeSlot ?? string.Empty
            });
        }

        /// <summary>
        /// 確認頁面 
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Confirm(
            string id, 
            string companyId, 
            string branchId, 
            DateTime selectedDate, 
            int adultCount, 
            int childCount, 
            string selectedMealPeriod, 
            string selectedTimeSlot)
        {
            if(string.IsNullOrEmpty(id) || string.IsNullOrEmpty(companyId) || string.IsNullOrEmpty(branchId))
            {
                return RedirectToAction("Index", "Restaurant");
            }

            try
            {
                // ========== 呼叫 Framework API 取得完整資訊 ==========
                var apiResult = await _inlineAppsService.GetBranchAsync(
                    id, companyId, branchId, 
                    "all",  // type=all 取得完整資訊
                    size: 2,
                    date: selectedDate.Date);

                var restaurant = await _restaurantService.GetRestaurantDetailAsync(id, branchId);
                if(restaurant == null)
                {
                    return RedirectToAction("Index", "Restaurant");
                }

                var mallGroups = await _restaurantService.GetMallsAsync();
                var branchDict = new Dictionary<string, int>();
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
                }

                var selectedBranchId = branchDict.ContainsKey(id) ? branchDict[id] : 1;
                var selectedBranch = branches.FirstOrDefault(b => b.Id == selectedBranchId) ?? branches.FirstOrDefault();

                var restaurantInfo = new Reservation.Models.ViewModels.RestaurantInfoModel
                {
                    Id = selectedBranchId,
                    Name = restaurant.Name,
                    ImageUrl = _restaurantService.GetRestaurantImageUrl(id, branchId),
                    Location = restaurant.Address,
                    Phone = restaurant.PhoneNumber,
                    OpeningHours = "10:00-22:00",
                    BranchId = selectedBranchId,
                    CategoryId = 1,
                    IsPopular = false,
                    IsNew = false
                };

                var model = new ReservationConfirmModel
                {
                    Restaurant = restaurantInfo,
                    Branch = selectedBranch ?? new BranchModel(),
                    SelectedDate = selectedDate,
                    AdultCount = adultCount,
                    ChildCount = childCount,
                    SelectedMealPeriod = selectedMealPeriod ?? "中午",
                    SelectedTimeSlot = selectedTimeSlot ?? string.Empty
                };

                // 儲存到 ViewBag 供 View 使用
                ViewBag.GroupId = id;
                ViewBag.CompanyId = companyId;
                ViewBag.BranchId = branchId;

                return View(model);
            }
            catch(Exception ex)
            {
                _Log?.SystemErrorLog_Txt($"確認頁面發生錯誤: {ex.Message}");
                TempData["ErrorMsg"] = "載入確認頁面時發生錯誤";
                return RedirectToAction("Index", "Restaurant");
            }
        }

        /// <summary>
        /// 確認頁面 POST
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Confirm(ReservationConfirmModel model)
        {
            try
            {
                // ========== 步驟 1：驗證資料 ==========
                var errors = new List<string>();
                
                if(string.IsNullOrWhiteSpace(model.CustomerName))
                {
                    errors.Add("訂位人姓名為必填欄位");
                }
                
                if(string.IsNullOrWhiteSpace(model.CustomerPhone))
                {
                    errors.Add("訂位人電話為必填欄位");
                }
                
                if(errors.Any())
                {
                    return Json(new { success = false, errors = errors });
                }

                // ========== 步驟 2：從 Form 取得 GroupId, CompanyId, BranchId ==========
                var groupId = Request.Form["GroupId"].ToString();
                var companyId = Request.Form["CompanyId"].ToString();
                var branchId = Request.Form["BranchId"].ToString();
                
                if(string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(companyId) || string.IsNullOrEmpty(branchId))
                {
                    // 如果 Form 沒有，嘗試從資料庫查詢
                    var restaurant = await _restaurantService.GetRestaurantDetailAsync(
                        groupId ?? string.Empty, 
                        branchId ?? string.Empty);
                    
                    if(restaurant == null)
                    {
                        return Json(new { success = false, errors = new[] { "找不到餐廳資料" } });
                    }
                    
                    groupId = restaurant.GroupId;
                    companyId = restaurant.CompanyId;
                    branchId = restaurant.id;
                }

                // ========== 步驟 3：取得登入會員 ID ==========
                int memberId = 0;
                if(HttpContext.Items["MemberId"] != null && HttpContext.Items["MemberId"] is int memberIdValue)
                {
                    memberId = memberIdValue;
                    _Log?.SystemLog_Txt($"訂位-取得會員ID:{memberId}");
                }else
                {
                    _Log?.SystemLog_Txt($"訂位-未取得會員ID，使用預設值0");
                    _Log?.SystemLog_Txt($"訂位-HttpContext.Items[\"MemberId\"]: {HttpContext.Items["MemberId"]}");
                    if(HttpContext.Items.ContainsKey("MemberId"))
                    {
                        _Log?.SystemLog_Txt($"訂位-HttpContext.Items[\"MemberId\"]: {HttpContext.Items["MemberId"]}");
                    }
                }

                // ========== 步驟 4：將 CustomerTitle (先生/小姐) 轉換為 Gender (0/1/2) ==========
                int gender = 2; // 預設：未指定
                if(model.CustomerTitle == "先生")
                {
                    gender = 0;
                }
                else if(model.CustomerTitle == "小姐")
                {
                    gender = 1;
                }

                // ========== 步驟 4：格式化電話號碼==========
                string formattedPhone = model.CustomerPhone;
                bool isMobile = Regex.IsMatch(model.CustomerPhone, @"^09[0-9]{8}$");
                
                if(isMobile)
                {
                    formattedPhone = "+886" + model.CustomerPhone.Substring(1);
                }
                else
                {
                    return Json(new { success = false, errors = new[] { "手機電話格式不正確，請輸入09開頭的10碼手機號碼" } });
                }

                // ========== 步驟 5：轉換日期時間為 UTC==========
                DateTime bookingDateTime;
                string datetimeIso;
                
                var timeParts = model.SelectedTimeSlot.Split(':');
                if(timeParts.Length == 2 && int.TryParse(timeParts[0], out int hours))
                {
                    // 處理跨日時間（00:00-02:59 屬於次日）
                    if(hours < 3)
                    {
                        bookingDateTime = model.SelectedDate.AddDays(1);
                    }
                    else
                    {
                        bookingDateTime = model.SelectedDate;
                    }

                    if(int.TryParse(timeParts[1], out int minutes))
                    {
                        bookingDateTime = new DateTime(
                            bookingDateTime.Year, 
                            bookingDateTime.Month, 
                            bookingDateTime.Day, 
                            hours, 
                            minutes, 
                            0);
                    }
                    else
                    {
                        // 如果無法解析分鐘，預設為 0
                        bookingDateTime = new DateTime(
                            bookingDateTime.Year, 
                            bookingDateTime.Month, 
                            bookingDateTime.Day, 
                            hours, 
                            0, 
                            0);
                    }

                    // 檢查時間是否已過（如果是今天）
                    if(bookingDateTime.Date == DateTime.Today && bookingDateTime <= DateTime.Now)
                    {
                        return Json(new { success = false, errors = new[] { "選擇的時間已過，請選擇未來的時間" } });
                    }
                    
                    DateTime bookingDateTimeUtc = bookingDateTime.AddHours(-8); // 台灣時間轉 UTC
                    datetimeIso = bookingDateTimeUtc.ToString("s") + "Z"; // ISO 8601 格式，使用大寫 Z

                    _Log?.SystemLog_Txt($"訂位時間轉換 - 選擇日期: {model.SelectedDate:yyyy-MM-dd}, 選擇時間: {model.SelectedTimeSlot}, 組合時間: {bookingDateTime:yyyy-MM-dd HH:mm:ss}, UTC時間: {datetimeIso}");
                }
                else
                {
                    // 如果無法解析時間格式，使用預設邏輯
                    string dateTimeStr = $"{model.SelectedDate:yyyy-MM-dd} {model.SelectedTimeSlot}:00";
                    bookingDateTime = DateTime.Parse(dateTimeStr);
                    
                    if(bookingDateTime.Date == DateTime.Today && bookingDateTime <= DateTime.Now)
                    {
                        return Json(new { success = false, errors = new[] { "選擇的時間已過，請選擇未來的時間" } });
                    }
                    
                    DateTime bookingDateTimeUtc = bookingDateTime.AddHours(-8);
                    datetimeIso = bookingDateTimeUtc.ToString("s") + "Z";
                    
                    _Log?.SystemLog_Txt($"訂位時間轉換（預設邏輯）- 選擇日期: {model.SelectedDate:yyyy-MM-dd}, 選擇時間: {model.SelectedTimeSlot}, 組合時間: {bookingDateTime:yyyy-MM-dd HH:mm:ss}, UTC時間: {datetimeIso}");
                }

                // ========== 步驟 6：組合 customerNote（用餐目的 + 備註）==========
                string customerNote = string.Empty;
                if(!string.IsNullOrWhiteSpace(model.DiningPurpose))
                {
                    customerNote = model.DiningPurpose;
                }
                if(!string.IsNullOrWhiteSpace(model.Remarks))
                {
                    if(!string.IsNullOrEmpty(customerNote))
                    {
                        customerNote += "；";
                    }
                    customerNote += model.Remarks;
                }

                // ========== 步驟 7：建立 Framework API 的請求資料 ==========
                var reservationData = new InlineApiPostModel
                {
                    customerName = model.CustomerName,
                    gender = gender,
                    phone = formattedPhone,
                    language = "zh-TW",
                    groupSize = model.AdultCount,
                    numberOfKidChairs = model.ChildCount,
                    datetime = datetimeIso,
                    customerNote = customerNote,
                    createdFrom = "FEDSWEB"
                };

                _Log?.SystemLog_Txt($"=== 提交訂位 ===");
                _Log?.SystemLog_Txt($"GroupId: {groupId}, CompanyId: {companyId}, BranchId: {branchId}");
                _Log?.SystemLog_Txt($"訂位資料: {System.Text.Json.JsonSerializer.Serialize(reservationData)}");

                // ========== 步驟 8：POST API ==========
                var apiResult = await _inlineAppsService.PostReservationAsync(
                    companyId, 
                    branchId, 
                    reservationData);

                if(apiResult.Code == 200)
                {
                    _Log?.SystemLog_Txt($"訂位成功 - API 回應: {apiResult.Data}");
                    
                    try
                    {
                        // ========== 解析 API 回應的所有資料 ==========
                        string parsedApiData = string.Empty;
                        string externalReservationId = string.Empty;
                        string customerId = string.Empty;
                        string reservationLink = string.Empty;
                        
                        try
                        {
                            var result = Newtonsoft.Json.Linq.JObject.Parse(apiResult.Data);
                            
                            // 解析所有欄位
                            externalReservationId = result["reservationId"]?.ToString() ?? string.Empty;
                            customerId = result["customerId"]?.ToString() ?? string.Empty;
                            reservationLink = result["reservationLink"]?.ToString() ?? string.Empty;
                            
                            // 組合解析後的資料（用於記錄到 Remark）
                            var parsedFields = new System.Text.StringBuilder();
                            parsedFields.AppendLine("=== API 回應解析 ===");
                            
                            foreach(var prop in result.Properties())
                            {
                                parsedFields.AppendLine($"{prop.Name}: {prop.Value?.ToString() ?? "null"}");
                                
                                // 記錄到日誌
                                _Log?.SystemLog_Txt($"API 回應欄位 - {prop.Name}: {prop.Value?.ToString() ?? "null"}");
                            }
                            
                            parsedApiData = parsedFields.ToString();
                            
                            // 記錄解析結果
                            _Log?.SystemLog_Txt($"訂位 API 解析完成 - ReservationId: {externalReservationId}, CustomerId: {customerId}, Link: {reservationLink}");
                        }
                        catch(Exception parseEx)
                        {
                            _Log?.SystemErrorLog_Txt($"解析訂位 API 回應失敗: {parseEx.Message}");
                            parsedApiData = $"解析失敗: {parseEx.Message}";
                        }

                        var memberReserveLog = new MemberReserveLog{
                            Status = "N",
                            Json = apiResult.Data,  // 完整 JSON 回應
                            Creator = 0,
                            CreateDate = DateTime.Now,
                            CreateFrom = "FEDS-SYS"
                        };

                        var memberReserve = new MemberReserve{
                            MemberId = memberId, // 會員ID，非會員為 0
                            CompanyId = companyId,
                            BranchId = branchId,
                            GroupSize = model.AdultCount,
                            NumberOfKid = model.ChildCount,
                            ContactName = model.CustomerName,
                            ContactPhone = formattedPhone,
                            ContactGender = (byte)gender,
                            Datetime = bookingDateTime, // 台灣時間（非 UTC）
                            Note = customerNote,
                            CustomerId = customerId, 
                            ExternalReservationId = externalReservationId,  
                            Status = "已預訂", 
                            Remark = null,
                            Creator = 0,
                            CreateDate = DateTime.Now,
                            CreateFrom = "FEDS-SYS",
                            MemberReserveLogs = new List<MemberReserveLog> { memberReserveLog }
                        };

                        try
                        {
                            _Log?.SystemLog_Txt("[DB] 準備寫入 MemberReserve");
                            _Log?.SystemLog_Txt($"[DB] MemberId={memberReserve.MemberId}, CompanyIdLen={memberReserve.CompanyId?.Length}, BranchIdLen={memberReserve.BranchId?.Length}");
                            _Log?.SystemLog_Txt($"[DB] ContactNameLen={memberReserve.ContactName?.Length}, PhoneLen={memberReserve.ContactPhone?.Length}");
                            _Log?.SystemLog_Txt($"[DB] NoteLen={memberReserve.Note?.Length}, RemarkLen={memberReserve.Remark?.Length}");
                            _Log?.SystemLog_Txt($"[DB] CustomerIdLen={memberReserve.CustomerId?.Length}, ExternalReservationIdLen={memberReserve.ExternalReservationId?.Length}");
                            _Log?.SystemLog_Txt($"[DB] StatusLen={memberReserve.Status?.Length}, CreateFromLen={memberReserve.CreateFrom?.Length}");                       
                        
                            // 序列化 Entity 時避免循環引用問題
                            try
                            {
                                var jsonOptions = new JsonSerializerOptions 
                                { 
                                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                                    MaxDepth = 3
                                };
                                _Log?.SystemLog_Txt($"[DB] EntitySnapshot={JsonSerializer.Serialize(memberReserve, jsonOptions)}");
                            }
                            catch(Exception jsonEx)
                            {
                                _Log?.SystemErrorLog_Txt($"[DB] 序列化 Entity 失敗: {jsonEx.Message}");
                            }

                            _restaurantContext.MemberReserves.Add(memberReserve);
                            var affected = await _restaurantContext.SaveChangesAsync();

                            _Log?.SystemLog_Txt($"[DB] SaveChanges 成功，affected={affected}, id={memberReserve.Id}, MemberId={memberReserve.MemberId}");
                            _Log?.SystemLog_Txt($"訂位資料已儲存到資料庫: {memberReserve.Id}, ReservationId: {externalReservationId}, MemberId: {memberReserve.MemberId}");
                        }
                        catch(DbUpdateException dbEx)
                        {
                            _Log?.SystemErrorLog_Txt($"[DB] DbUpdateException: {dbEx.Message}");
                            _Log?.SystemErrorLog_Txt($"[DB] InnerExceptionType: {dbEx.InnerException?.GetType().FullName}");
                            _Log?.SystemErrorLog_Txt($"[DB] InnerException: {dbEx.InnerException?.Message}");
                            _Log?.SystemErrorLog_Txt($"[DB] StackTrace: {dbEx.StackTrace}");

                            if(dbEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
                            {
                                _Log?.SystemErrorLog_Txt($"[DB] SqlException.Number={sqlEx.Number}, State={sqlEx.State}, Class={sqlEx.Class}, LineNumber={sqlEx.LineNumber}, Procedure={sqlEx.Procedure}");
                                foreach (Microsoft.Data.SqlClient.SqlError err in sqlEx.Errors)
                                {
                                    _Log?.SystemErrorLog_Txt($"[DB] SqlError: Number={err.Number}, Message={err.Message}, Line={err.LineNumber}, Proc={err.Procedure}");
                                }
                            }
                            return Json(new { success = false, errors = new[] { "寫入訂位資料失敗，請查看系統紀錄" } });
                        }
                        catch(Exception ex)
                        {
                            _Log?.SystemErrorLog_Txt($"[DB] Exception: {ex.Message}");
                            _Log?.SystemErrorLog_Txt($"[DB] StackTrace: {ex.StackTrace}");
                            return Json(new { success = false, errors = new[] { "系統錯誤，請稍後再試" } });
                        }

                        // ========== 呼叫 API 取得完整記錄並更新資料庫 ==========
                        if(!string.IsNullOrEmpty(customerId) && !string.IsNullOrEmpty(companyId))
                        {
                            try
                            {
                                 _Log?.SystemLog_Txt($"開始呼叫 API 取得訂位完整記錄 - CustomerId: {customerId}");

                                 var queryApiResult = await _inlineAppsService.GetCustomerReservationAsync(companyId, customerId, branchId, "booking,waiting");

                                 if(queryApiResult.Code == 200 && !string.IsNullOrEmpty(queryApiResult.Data))
                                 {
                                    var queryResult = Newtonsoft.Json.Linq.JObject.Parse(queryApiResult.Data);
                                    var reservations = queryResult["reservations"] as Newtonsoft.Json.Linq.JArray;
                                    if(reservations !=null)
                                    {
                                        foreach(var reservation in reservations)
                                        {
                                            var apiReservationId = reservation["id"]?.ToString() ?? string.Empty;
                                            var apiState = reservation["state"]?.ToString()?? string.Empty;
                                            var apiType = reservation["type"]?.ToString()??string.Empty;

                                            // 記錄所有 API 回應的詳細資訊
                                            var rawReservationJson = reservation?.ToString(Newtonsoft.Json.Formatting.None) ?? "(null)";
                                            _Log?.SystemLog_Txt($"[StatusDebug] 檢查 API 記錄 - ApiId:{apiReservationId}, Type:{apiType}, State:{apiState}, ExternalReservationId:{externalReservationId}, DbExternalReservationId:{memberReserve.ExternalReservationId}");

                                            if(apiReservationId == externalReservationId || 
                                            (!string.IsNullOrEmpty(memberReserve.ExternalReservationId) && 
                                            apiReservationId == memberReserve.ExternalReservationId))
                                            {
                                                _Log?.SystemLog_Txt($"[StatusDebug] 找到匹配記錄 - ApiId:{apiReservationId}, Type:{apiType}, State:{apiState}, RawJson:{rawReservationJson}");
                                                
                                                memberReserve.ExternalReservationId = apiReservationId;
                                                memberReserve.CustomerId = customerId;
                                                
                                                string status = "已預定";
                                                if(apiType == "booking")
                                                {
                                                    status = apiState switch
                                                    {
                                                        "waiting" => "已預定",
                                                        "seated" => "已入座",
                                                        "cancelled" => "已取消",
                                                        _=>"已預定"
                                                    };
                                                }
                                                else if(apiType == "waiting")
                                                {
                                                    status = apiState switch
                                                    {
                                                        "waiting" => "已預定",
                                                        "seated" => "已入座",
                                                        "cancelled" => "已取消",
                                                        _=>"已預定"
                                                    };
                                                }else if(apiType == "walk-in")
                                                {
                                                    status = apiState switch
                                                    {
                                                        "waiting" => "已預定",
                                                        "seated" => "已入座",
                                                        "cancelled" => "已取消",
                                                        _=>"已預定"
                                                    };
                                                }
                                                else
                                                {
                                                    status = "未知狀態";
                                                    _Log?.SystemErrorLog_Txt($"[StatusDebug] 未知類型 - Type:{apiType}, State:{apiState}, RawJson:{rawReservationJson}");
                                                }
                                                memberReserve.Status = status;
                                                await _restaurantContext.SaveChangesAsync();
                                                _Log?.SystemLog_Txt($"訂位紀錄更新 - Status:{status}, Type:{apiType}, State:{apiState}, ApiId:{apiReservationId}");
                                                break;
                                            }
                                        }
                                    }
                                 }
                            }catch(Exception ex)
                            {
                                _Log?.SystemErrorLog_Txt($"取得訂位完整記錄失敗: {ex.Message}");
                            }


                        }
                    }
                    catch(Exception ex)
                    {
                      _Log?.SystemErrorLog_Txt($"儲存訂位資料到資料庫失敗: {ex.Message}");
                    }

                    return Json(new { success = true, message = "訂位成功" });
                }
                else
                {
                    _Log?.SystemErrorLog_Txt($"訂位失敗 - Code: {apiResult.Code}, Msg: {apiResult.Msg}, Data: {apiResult.Data}");
                    
                    string errorMessage = "訂位失敗，請稍後再試";
                    bool isLimitReached = false;
                    bool isDuplicate = false;
                    
                    try
                    {
                        var errorData = Newtonsoft.Json.Linq.JObject.Parse(apiResult.Data);
                        var message = errorData.GetValue("message")?.ToString();
                        var reason = errorData.GetValue("reason")?.ToString();
                        var errorCode = errorData.GetValue("code")?.ToString();
                        
                        // 先檢查錯誤碼（最優先）
                        if(!string.IsNullOrEmpty(errorCode))
                        {
                            // 300001 表示達到訂位數量限制
                            if(errorCode == "300001")
                            {
                                isLimitReached = true;
                                errorMessage = "您已達到該餐廳的訂位數量上限，請先取消其他訂位或聯絡客服單位";
                            }
                            else
                            {
                                var errorCodeUpper = errorCode.ToUpper();
                                if(errorCodeUpper.Contains("DUPLICATE") || errorCodeUpper.Contains("ALREADY"))
                                {
                                    isDuplicate = true;
                                }
                            }
                        }
                        
                        // 檢查錯誤訊息
                        if(!string.IsNullOrEmpty(message) && !isLimitReached && !isDuplicate)
                        {
                            var messageLower = message.ToLower();
                            if(messageLower.Contains("hit customer") || messageLower.Contains("limit"))
                            {
                                isLimitReached = true;
                                errorMessage = "您已達到該餐廳的訂位數量上限，請先取消其他訂位或聯絡客服單位";
                            }
                            else if(messageLower.Contains("duplicate") || messageLower.Contains("already"))
                            {
                                isDuplicate = true;
                            }
                            else
                            {
                                errorMessage = message;
                            }
                        }
                        
                        // 檢查 reason
                       if(!string.IsNullOrEmpty(reason) && !isLimitReached && !isDuplicate)
                        {
                            var reasonLower = reason.ToLower();
                            if(reasonLower.Contains("not in bookable time") || reasonLower.Contains("bookable time"))
                            {
                                errorMessage = "選擇的時間不在可訂位時間範圍內，請重新選擇時間";
                            }
                            else if(reasonLower.Contains("hit customer") || reasonLower.Contains("limit") || reasonLower.Contains("not allowed to make more"))
                            {
                                isLimitReached = true;
                                errorMessage = "您已達到該餐廳的訂位數量上限，請先取消其他訂位或聯絡客服單位";
                            }
                            else if(reasonLower.Contains("duplicate") || reasonLower.Contains("already"))
                            {
                                isDuplicate = true;
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        _Log?.SystemErrorLog_Txt($"訂位失敗 - 解析錯誤回應失敗: {ex.Message}");
                    }
                    
                    if(isDuplicate)
                    {
                        errorMessage = "此訂位已存在，請聯絡客服單位";
                    }
                    
                    return Json(new { success = false, errors = new[] { errorMessage } });
                }
            }
            catch(Exception ex)
            {
                _Log?.SystemErrorLog_Txt($"提交訂位發生錯誤: {ex.Message}\r\nStackTrace: {ex.StackTrace}");
                return Json(new { success = false, errors = new[] { "系統發生錯誤，請稍後再試" } });
            }
        }

        private List<string> GetAvailableTimeSlots(string mealPeriod, DateTime selectedDate)
        {
            var timeSlots = new List<string>();
            var now = DateTime.Now;
            var isToday = selectedDate.Date == now.Date;

            if (mealPeriod == "中午")
            {
                for (int hour = 11; hour <= 15; hour++)
                {
                    timeSlots.Add($"{hour:00}:00");
                    timeSlots.Add($"{hour:00}:30");
                }
                timeSlots.Add("16:00");

                if (isToday)
                {
                    timeSlots = timeSlots.Where(slot =>
                    {
                        var timeParts = slot.Split(':');
                        var slotHour = int.Parse(timeParts[0]);
                        var slotMinute = int.Parse(timeParts[1]);
                        var slotTime = new DateTime(now.Year, now.Month, now.Day, slotHour, slotMinute, 0);
                        return slotTime > now;
                    }).ToList();
                }
            }
            else if (mealPeriod == "晚上")
            {
                for (int hour = 17; hour <= 21; hour++)
                {
                    timeSlots.Add($"{hour:00}:00");
                    timeSlots.Add($"{hour:00}:30");
                }
                timeSlots.Add("22:00");

                if (isToday)
                {
                    timeSlots = timeSlots.Where(slot =>
                    {
                        var timeParts = slot.Split(':');
                        var slotHour = int.Parse(timeParts[0]);
                        var slotMinute = int.Parse(timeParts[1]);
                        var slotTime = new DateTime(now.Year, now.Month, now.Day, slotHour, slotMinute, 0);
                        return slotTime > now;
                    }).ToList();
                }
            }

            return timeSlots;
        }


        /// <summary>
        /// 轉換 API 回傳的時間格式（處理跨日時間，如 24:00 → 00:00）
        /// </summary>
        private string NormalizeTimeSlot(string timeSlot)
        {
            if(string.IsNullOrEmpty(timeSlot))
                return timeSlot;

            // 解析時間格式 HH:mm
            var parts = timeSlot.Split(':');
            if(parts.Length != 2)
                return timeSlot; // 格式不正確，直接返回
            
            if(int.TryParse(parts[0], out int hours) && int.TryParse(parts[1], out int minutes))
            {
                // 如果小時數 >= 24，轉換為標準格式
                if(hours >= 24)
                {
                    hours = hours % 24;
                }
                
                // 確保分鐘數在有效範圍內
                if(minutes < 0 || minutes >= 60)
                {
                    minutes = 0;
                }
                
                return $"{hours:D2}:{minutes:D2}";
            }
            
            return timeSlot; // 無法解析，直接返回
        }

        /// <summary>
        /// 根據餐期過濾時間
        /// </summary>
        private List<string> FilterTimeSlots(List<string> timeSlots, string mealPeriod)
        {
            if(string.IsNullOrEmpty(mealPeriod) || timeSlots == null || timeSlots.Count == 0)
            {
                return timeSlots ?? new List<string>();
            }

            var filteredSlots = new List<string>();

            foreach(var slot in timeSlots)
            {
                var parts = slot.Split(':');
                if(parts.Length == 2 && int.TryParse(parts[0], out int hours))
                {
                    if(mealPeriod == "中午")
                    {
                        // 中午：11:00 - 16:59
                        if(hours >= 11 && hours < 17)
                        {
                            filteredSlots.Add(slot);
                        }
                    }
                    else if(mealPeriod == "晚上")
                    {
                        // 晚上：17:00 - 23:59 或 00:00 - 02:59（跨日）
                        if(hours >= 17 || hours < 3)
                        {
                            filteredSlots.Add(slot);
                        }
                    }
                }
            }
            
            return filteredSlots.OrderBy(t => 
            {
                var parts = t.Split(':');
                if(parts.Length == 2 && int.TryParse(parts[0], out int hours) && int.TryParse(parts[1], out int minutes))
                {
                    if(hours < 3)
                    {
                        return hours * 100 + minutes + 2400; // 例如：00:00 -> 2400, 02:30 -> 2630
                    }
                    else
                    {
                        return hours * 100 + minutes; // 例如：17:00 -> 1700, 23:45 -> 2345
                    }
                }
                return 9999; // 無法解析的時間排在最後
            }).ToList();
        }
        /// <summary>
        /// 取得可訂位時段（AJAX 用）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTimeSlots(string id, string companyId, string branchId, string date, string mealPeriod = "")
        {
            if(string.IsNullOrEmpty(id) || string.IsNullOrEmpty(companyId) || string.IsNullOrEmpty(branchId))
            {
                return Json(new List<string>());
            }

            try
            {  
                if(DateTime.TryParse(date, out DateTime selectedDate))
                {
                    var apiResult = await _inlineAppsService.GetBranchAsync(
                        id, companyId, branchId, 
                        "booking",
                        size: 2,
                        date: selectedDate.Date);

                    var timeSlots = new List<string>();

                    if(apiResult.Code == 200)
                    {
                        var data = JObject.Parse(apiResult.Data);
                        var bookingInfo = data.GetValue("bookingInfo");
                        
                        if(bookingInfo != null)
                        {
                            var defaultBooking = bookingInfo.Value<JObject>("default");
                            if(defaultBooking != null)
                            {
                                var dateBooking = defaultBooking.Value<JObject>(selectedDate.ToString("yyyy-MM-dd"));
                                if(dateBooking != null)
                                {
                                    var timeProperties = dateBooking.Properties();
                                    foreach(var prop in timeProperties)
                                    {
                                        if(prop.Value.ToString() == "open")
                                        {
                                            var normalizedTime = NormalizeTimeSlot(prop.Name);
                                            timeSlots.Add(normalizedTime);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // 如果 mealPeriod 為空，返回所有時間（不過濾）
                    if(!string.IsNullOrEmpty(mealPeriod))
                    {
                        timeSlots = FilterTimeSlots(timeSlots, mealPeriod);
                    }
                    else
                    {
                        // 如果不過濾，需要排序所有時間（跨日時間排在後面）
                        timeSlots = timeSlots.OrderBy(t => 
                        {
                            var parts = t.Split(':');
                            if(parts.Length == 2 && int.TryParse(parts[0], out int hours) && int.TryParse(parts[1], out int minutes))
                            {
                                if(hours < 3)
                                {
                                    return (hours + 24) * 100 + minutes;
                                }
                                else
                                {
                                    return hours * 100 + minutes;
                                }
                            }
                            return 9999;
                        }).ToList();
                    }
                    
                    return Json(timeSlots);
                }
            }
            catch(Exception ex)
            {
                _Log?.SystemErrorLog_Txt($"取得時段失敗: {ex.Message}");
            }

            return Json(new List<string>());
        }
    }
}
