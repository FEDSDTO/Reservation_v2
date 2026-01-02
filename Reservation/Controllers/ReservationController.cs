using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Reservation.Models.EFRestaurantModels;
using Reservation.Models.ViewModels;
using Reservation.Service;
using Newtonsoft.Json.Linq; 

namespace Reservation.Controllers
{
    public class ReservationController : Controller
    {
        private readonly RestaurantContext _restaurantContext;
        private readonly RestaurantService _restaurantService;

        public ReservationController(
            RestaurantContext restaurantContext,
            RestaurantService restaurantService)
        {
            _restaurantContext = restaurantContext;
            _restaurantService = restaurantService;
        }

        public async Task<IActionResult> Index(string branchId, string restaurantId)
        {
            if(string.IsNullOrEmpty(branchId) || string.IsNullOrEmpty(restaurantId))
            {
                return NotFound();
            }

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
            var restaurant = await _restaurantService.GetRestaurantDetailAsync(branchId, restaurantId);

            if(restaurant == null)
            {
                return NotFound();
            }

            // ========== 步驟 3：從 API 取得菜單圖片（菜單圖片不儲存在資料庫） ==========
            var menus = new List<MenuModel>();
            string openingHours = "10:00-22:00"; // 預設值

            try
            {
                var inlineAppsService = HttpContext.RequestServices.GetRequiredService<InlineAppsService>();
                var apiResult = await inlineAppsService.GetInlineApps(
                    $"/v2/groups/{branchId}",
                    $"type=all&size=1&date={DateTime.Now:yyyy-MM-dd}");

                if(apiResult.Code == 200)
                {
                    var data = JObject.Parse(apiResult.Data);
                    var jarrCards = (JArray?)data.GetValue("branches");

                    if(jarrCards != null)
                    {
                        var targetBranch = jarrCards.FirstOrDefault(b =>
                            b.Value<string>("id") == restaurantId) as JObject;

                        if(targetBranch != null)
                        {
                            // 取得菜單圖片
                            var menusArray = targetBranch.Value<JArray>("menus");
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
                            var openingTimes = targetBranch.Value<JArray>("openingTimes");
                            if(openingTimes != null && openingTimes.Count > 0)
                            {
                                var today = DateTime.Now.ToString("yyyy-MM-dd");
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
                    }
                }
            }
            catch(Exception)
            {
                // 如果取得菜單失敗，使用預設值
            }

            // ========== 步驟 4：建立 ViewModel ==========
            var restaurantImageUrl = $"~/IMG/HomePage/{branchId}/{restaurantId}.jpg";

            var restaurantInfo = new Reservation.Models.ViewModels.RestaurantInfoModel
            {
                Id = branches.FirstOrDefault(b => branchDict.ContainsKey(branchId) && branchDict[branchId] == b.Id)?.Id ?? 1,
                Name = restaurant.Name,
                ImageUrl = restaurantImageUrl,
                Location = restaurant.Address,
                Phone = restaurant.PhoneNumber,
                OpeningHours = openingHours,
                BranchId = branchDict.ContainsKey(branchId) ? branchDict[branchId] : 1,
                CategoryId = 1,
                IsPopular = false,
                IsNew = false
            };

            var selectedBranch = branches.FirstOrDefault(b => branchDict.ContainsKey(branchId) && branchDict[branchId] == b.Id)
                ?? branches.FirstOrDefault();

            var viewModel = new ReservationModel
            {
                Restaurant = restaurantInfo,
                Branch = selectedBranch ?? new BranchModel(),
                SelectedDate = DateTime.Today,
                AdultCount = 2,
                ChildCount = 0,
                SelectedMealPeriod = "中午",
                AvailableTimeSlots = GetAvailableTimeSlots("中午", DateTime.Today),
                Menus = menus
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Index(ReservationModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableTimeSlots = GetAvailableTimeSlots(model.SelectedMealPeriod, model.SelectedDate ?? DateTime.Today);
                model.Menus = new List<MenuModel>();
                return View(model);
            }

            return RedirectToAction("Confirm", "Reservation", new
            {
                restaurantId = model.Restaurant.Id,
                branchId = model.Branch.Id,
                selectedDate = model.SelectedDate ?? DateTime.Today,
                adultCount = model.AdultCount,
                childCount = model.ChildCount,
                selectedMealPeriod = model.SelectedMealPeriod,
                selectedTimeSlot = model.SelectedTimeSlot ?? string.Empty
            });
        }

        [HttpGet]
        public IActionResult Confirm(int restaurantId, int branchId, DateTime selectedDate, int adultCount, int childCount, string selectedMealPeriod, string selectedTimeSlot)
        {
           if (restaurantId == 0)
           {
               return RedirectToAction("Index", "Restaurant");
           }

           var model = new ReservationConfirmModel
           {
               Restaurant = new Reservation.Models.ViewModels.RestaurantInfoModel { Id = restaurantId },
               Branch = new BranchModel { Id = branchId },
               SelectedDate = selectedDate,
               AdultCount = adultCount,
               ChildCount = childCount,
               SelectedMealPeriod = selectedMealPeriod ?? "中午",
               SelectedTimeSlot = selectedTimeSlot ?? string.Empty
           };

           return View(model);
        }

        [HttpPost]
        public IActionResult Confirm(ReservationConfirmModel model)
        {
           if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
           {
               return Json(new { success = true, message = "訂位成功" });
           }

           TempData["ReservationSuccess"] = "訂位資料已提交";
           return RedirectToAction("Index", "Restaurant");
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

        [HttpGet]
        public IActionResult GetTimeSlots(string mealPeriod, string date)
        {
            if (DateTime.TryParse(date, out DateTime selectedDate))
            {
                var timeSlots = GetAvailableTimeSlots(mealPeriod, selectedDate);
                return Json(timeSlots);
            }
            return Json(new List<string>());
        }
    }
}
