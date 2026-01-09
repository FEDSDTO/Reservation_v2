using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Reservation.Models.EFRestaurantModels;
using Reservation.Models.ViewModels;
using Reservation.Service;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace Reservation.Controllers
{
    public class ReservationController : Controller
    {
        private readonly RestaurantContext _restaurantContext;
        private readonly RestaurantService _restaurantService;
        private readonly InlineAppsService _inlineAppsService;
        private readonly Func_Log _fileLogService;

        public ReservationController(
            RestaurantContext restaurantContext,
            RestaurantService restaurantService,
            InlineAppsService inlineAppsService,
            Func_Log fileLogService)
        {
            _restaurantContext = restaurantContext;
            _restaurantService = restaurantService;
            _inlineAppsService = inlineAppsService;
            _fileLogService = fileLogService;
        }

        /// <summary>
        /// 訂位頁面 
        /// </summary>
        public async Task<IActionResult> Index(string id, string companyId, string branchId)
        {
            // id = groupId, companyId, branchId 來自 URL
            if(string.IsNullOrEmpty(id) || string.IsNullOrEmpty(companyId) || string.IsNullOrEmpty(branchId))
            {
                _fileLogService?.SystemErrorLog_Txt($"訂位頁面參數缺失 - id: {id}, companyId: {companyId}, branchId: {branchId}");
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
                    _fileLogService?.SystemErrorLog_Txt($"找不到餐廳資料 - GroupId: {id}, BranchId: {branchId}");
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

                if(apiResult.Code == 200)
                {
                    try
                    {
                        var data = JObject.Parse(apiResult.Data);
                        
                        // 解析可訂位時段（對應 Framework 的 GetBookingTime）
                        var bookingInfo = data.GetValue("bookingInfo");
                        if(bookingInfo != null)
                        {
                            var defaultBooking = bookingInfo.Value<JObject>("default");
                            if(defaultBooking != null)
                            {
                                var todayBooking = defaultBooking.Value<JObject>(today);
                                if(todayBooking != null)
                                {
                                    var timeProperties = todayBooking.Properties();
                                    foreach(var prop in timeProperties)
                                    {
                                        if(prop.Value.ToString() == "open")
                                        {
                                            availableTimeSlots.Add(prop.Name);
                                        }
                                    }
                                }
                            }
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
                        _fileLogService?.SystemErrorLog_Txt($"解析 API 回應失敗: {ex.Message}");
                    }
                }
                else
                {
                    _fileLogService?.SystemErrorLog_Txt($"API 請求失敗 - Code: {apiResult.Code}, Msg: {apiResult.Msg}");
                }

                // ========== 步驟 4：建立 ViewModel ==========
                var restaurantImageUrl = $"~/IMG/HomePage/{id}/{branchId}.jpg";
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
                    Menus = menus
                };

                // 儲存到 ViewBag 供 View 使用
                ViewBag.GroupId = id;
                ViewBag.CompanyId = companyId;
                ViewBag.BranchId = branchId;

                return View(viewModel);
            }
            catch(Exception ex)
            {
                _fileLogService?.SystemErrorLog_Txt($"訂位頁面發生錯誤: {ex.Message}\r\nStackTrace: {ex.StackTrace}");
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
                    ImageUrl = $"~/IMG/HomePage/{id}/{branchId}.jpg",
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
                _fileLogService?.SystemErrorLog_Txt($"確認頁面發生錯誤: {ex.Message}");
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

                // ========== 步驟 3：將 CustomerTitle (先生/小姐) 轉換為 Gender (0/1/2) ==========
                int memberId = 0;
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
                string dateTimeStr = $"{model.SelectedDate:yyyy-MM-dd} {model.SelectedTimeSlot}:00";
                DateTime bookingDateTime = DateTime.Parse(dateTimeStr);
                DateTime bookingDateTimeUtc = bookingDateTime.AddHours(-8); // 台灣時間轉 UTC
                string datetimeIso = bookingDateTimeUtc.ToString("s") + "Z";

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

                _fileLogService?.SystemLog_Txt($"=== 提交訂位 ===");
                _fileLogService?.SystemLog_Txt($"GroupId: {groupId}, CompanyId: {companyId}, BranchId: {branchId}");
                _fileLogService?.SystemLog_Txt($"訂位資料: {System.Text.Json.JsonSerializer.Serialize(reservationData)}");

                // ========== 步驟 8：POST API ==========
                var apiResult = await _inlineAppsService.PostReservationAsync(
                    companyId, 
                    branchId, 
                    reservationData);

                if(apiResult.Code == 200)
                {
                    _fileLogService?.SystemLog_Txt($"訂位成功 - API 回應: {apiResult.Data}");
                    
                    try
                    {
                       var memberReserveLog=new MemberReserveLog{
                        Status="N",
                        Json=apiResult.Data,
                        Creator=0,
                        CreateDate=DateTime.Now,
                        CreateFrom="FEDS-SYS"
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
                            Creator = 0,
                            CreateDate = DateTime.Now,
                            CreateFrom = "FEDS-SYS",
                            MemberReserveLogs = new List<MemberReserveLog> { memberReserveLog }
                       };

                       _restaurantContext.MemberReserves.Add(memberReserve);
                       await _restaurantContext.SaveChangesAsync();
                       _fileLogService?.SystemLog_Txt($"訂位資料已儲存到資料庫: {memberReserve.Id}");
                    }catch(Exception ex)
                    {
                      _fileLogService?.SystemErrorLog_Txt($"儲存訂位資料到資料庫失敗: {ex.Message}");
                    }

                    return Json(new { success = true, message = "訂位成功" });
                }
                else
                {
                    _fileLogService?.SystemErrorLog_Txt($"訂位失敗 - Code: {apiResult.Code}, Msg: {apiResult.Msg}, Data: {apiResult.Data}");
                    return Json(new { success = false, errors = new[] { "訂位失敗，請聯絡客服單位" } });
                }
            }
            catch(Exception ex)
            {
                _fileLogService?.SystemErrorLog_Txt($"提交訂位發生錯誤: {ex.Message}\r\nStackTrace: {ex.StackTrace}");
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
        /// 取得可訂位時段（AJAX 用）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTimeSlots(string id, string companyId, string branchId, string date)
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
                                            timeSlots.Add(prop.Name);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    return Json(timeSlots.OrderBy(t => t).ToList());
                }
            }
            catch(Exception ex)
            {
                _fileLogService?.SystemErrorLog_Txt($"取得時段失敗: {ex.Message}");
            }

            return Json(new List<string>());
        }
    }
}
