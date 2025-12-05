using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Reservation.Models.DB;
using Reservation.Models.ViewModels;
using System.Globalization;

namespace Reservation.Controllers
{
    public class ReservationController : Controller
    {
        // 模擬數據服務（後續可替換為資料庫）
        private List<Branch> GetBranches()
        {
            return new List<Branch>
            {
                new Branch { Id = 1, Name = "遠百信義A13" },
                new Branch { Id = 2, Name = "遠百板橋" },
                new Branch { Id = 3, Name = "遠百台中" }
            };
        }

        private List<Restaurant> GetRestaurants()
        {
            return new List<Restaurant>
            {
                new Restaurant 
                { 
                    Id = 1, 
                    Name = "1010湘食堂", 
                    ImageUrl = "/Image/1.jpg",
                    Location = "4F懷舊食光埕",
                    Phone = "02-2729-0597",
                    OpeningHours = "10:00-22:00",
                    BranchId = 1,
                    CategoryId = 1,
                    IsPopular = true,
                    IsNew = false
                },
                new Restaurant 
                { 
                    Id = 2, 
                    Name = "1010湘食堂", 
                    ImageUrl = "/Image/1.jpg",
                    Location = "4F懷舊食光埕",
                    Phone = "02-2729-0597",
                    OpeningHours = "10:00-22:00",
                    BranchId = 1,
                    CategoryId = 1,
                    IsPopular = false,
                    IsNew = true
                },
                new Restaurant 
                { 
                    Id = 3, 
                    Name = "1010湘食堂", 
                    ImageUrl = "/Image/1.jpg",
                    Location = "4F懷舊食光埕",
                    Phone = "02-2729-0597",
                    OpeningHours = "10:00-22:00",
                    BranchId = 1,
                    CategoryId = 1,
                    IsPopular = false,
                    IsNew = false
                },
                new Restaurant 
                { 
                    Id = 4, 
                    Name = "1010湘食堂", 
                    ImageUrl = "/Image/1.jpg",
                    Location = "4F懷舊食光埕",
                    Phone = "02-2729-0597",
                    OpeningHours = "10:00-22:00",
                    BranchId = 1,
                    CategoryId = 1,
                    IsPopular = false,
                    IsNew = false
                },
                new Restaurant 
                { 
                    Id = 5, 
                    Name = "輕食咖啡廳", 
                    ImageUrl = "/Image/1.jpg",
                    Location = "3F美食廣場",
                    Phone = "02-2729-1234",
                    OpeningHours = "10:00-22:00",
                    BranchId = 1,
                    CategoryId = 2,
                    IsPopular = false,
                    IsNew = false
                },
                new Restaurant 
                { 
                    Id = 6, 
                    Name = "吃到飽火鍋", 
                    ImageUrl = "/Image/1.jpg",
                    Location = "5F主題餐廳",
                    Phone = "02-2729-5678",
                    OpeningHours = "10:00-22:00",
                    BranchId = 1,
                    CategoryId = 3,
                    IsPopular = false,
                    IsNew = false
                }
            };
        }

        // 取得菜單資料（只有圖片）
        private List<Menu> GetMenus(int restaurantId)
        {
            return new List<Menu>
            {
                new Menu
                {
                    Id = 1,
                    RestaurantId = restaurantId,
                    ImageUrl = "/Image/menu1.jpg"
                },
                new Menu
                {
                    Id = 2,
                    RestaurantId = restaurantId,
                    ImageUrl = "/Image/menu2.jpg"
                },
                new Menu
                {
                    Id = 3,
                    RestaurantId = restaurantId,
                    ImageUrl = "/Image/menu3.jpg"
                }
            };
        }

        // 訂位功能 - GET
        public IActionResult Index(int id)
        {
            var restaurant = GetRestaurants().FirstOrDefault(r => r.Id == id);
            if (restaurant == null)
            {
                return NotFound();
            }

            var branch = GetBranches().FirstOrDefault(b => b.Id == restaurant.BranchId);
            if (branch == null)
            {
                branch = GetBranches().First();
            }

            var viewModel = new ReservationViewModel
            {
                Restaurant = restaurant,
                Branch = branch,
                SelectedDate = DateTime.Today,
                AdultCount = 2,
                ChildCount = 0,
                SelectedMealPeriod = "中午",
                AvailableTimeSlots = GetAvailableTimeSlots("中午", DateTime.Today),
                Menus = GetMenus(restaurant.Id)
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Index(ReservationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var restaurant = GetRestaurants().FirstOrDefault(r => r.Id == model.Restaurant.Id);
                if (restaurant != null)
                {
                    model.Restaurant = restaurant;
                    var branch = GetBranches().FirstOrDefault(b => b.Id == restaurant.BranchId);
                    if (branch != null)
                    {
                        model.Branch = branch;
                    }
                }
                model.AvailableTimeSlots = GetAvailableTimeSlots(model.SelectedMealPeriod, model.SelectedDate ?? DateTime.Today);
                model.Menus = GetMenus(model.Restaurant.Id);
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

            var restaurant = GetRestaurants().FirstOrDefault(r => r.Id == restaurantId);
            if (restaurant == null)
            {
                return NotFound();
            }

            var branch = GetBranches().FirstOrDefault(b => b.Id == branchId);
            if (branch == null)
            {
                branch = GetBranches().FirstOrDefault(b => b.Id == restaurant.BranchId) ?? GetBranches().First();
            }

            var model = new ReservationConfirmViewModel
            {
                Restaurant = restaurant,
                Branch = branch,
                SelectedDate = selectedDate,
                AdultCount = adultCount,
                ChildCount = childCount,
                SelectedMealPeriod = selectedMealPeriod ?? "中午",
                SelectedTimeSlot = selectedTimeSlot ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Confirm(ReservationConfirmViewModel model)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "訂位成功" });
            }

            TempData["ReservationSuccess"] = "訂位資料已提交";
            return RedirectToAction("Index", "Restaurant");
        }

        // 取得可用時段
        private List<string> GetAvailableTimeSlots(string mealPeriod, DateTime selectedDate)
        {
            var timeSlots = new List<string>();
            var now = DateTime.Now;
            var isToday = selectedDate.Date == now.Date;

            if (mealPeriod == "中午")
            {
                // 中午時段：11:00-16:00，每 30 分鐘一個
                for (int hour = 11; hour <= 15; hour++)
                {
                    timeSlots.Add($"{hour:00}:00");
                    timeSlots.Add($"{hour:00}:30");
                }
                timeSlots.Add("16:00");

                // 如果是今天，過濾已過時段
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
                // 晚上時段：17:00-22:00，每 30 分鐘一個
                for (int hour = 17; hour <= 21; hour++)
                {
                    timeSlots.Add($"{hour:00}:00");
                    timeSlots.Add($"{hour:00}:30");
                }
                timeSlots.Add("22:00");

                // 如果是今天，過濾已過時段
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

        // API: 取得可用時段
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
