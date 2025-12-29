using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Reservation.Models.EFRestaurantModels;
using Reservation.Models.ViewModels;
using Reservation.Service;

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
            if (string.IsNullOrEmpty(restaurantId) || string.IsNullOrEmpty(branchId))
            {
                return NotFound();
            }

            var mallGroups = await _restaurantService.GetMallsAsync();
            var branchDict = new Dictionary<string, int>();
            var branches = new List<Branch>();
            
            for (int i = 0; i < mallGroups.Count; i++)
            {
                var branch = new Branch 
                { 
                    Id = i + 1,
                    Name = mallGroups[i].Name 
                };
                branches.Add(branch);
                branchDict[mallGroups[i].GroupId] = branch.Id;
            }

            var restaurantCards = await _restaurantService.GetRestaurantsAsync(branchId);
            var restaurantCard = restaurantCards.FirstOrDefault(r => r.id == restaurantId);
            
            if (restaurantCard == null)
            {
                return NotFound();
            }

            var restaurant = new Reservation.Models.ViewModels.Restaurant
            {
                Id = branches.FirstOrDefault(b => branchDict.ContainsKey(branchId) && branchDict[branchId] == b.Id)?.Id ?? 1,
                Name = restaurantCard.Name,
                ImageUrl = restaurantCard.Images?.FirstOrDefault() ?? "~/Image/1.jpg",
                Location = restaurantCard.Address,
                Phone = restaurantCard.PhoneNumber,
                OpeningHours = "10:00-22:00",
                BranchId = branchDict.ContainsKey(branchId) ? branchDict[branchId] : 1,
                CategoryId = 1,
                IsPopular = false,
                IsNew = false
            };

            var selectedBranch = branches.FirstOrDefault(b => branchDict.ContainsKey(branchId) && branchDict[branchId] == b.Id) 
                ?? branches.FirstOrDefault();

            var viewModel = new ReservationViewModel
            {
                Restaurant = restaurant,
                Branch = selectedBranch ?? new Branch(),
                SelectedDate = DateTime.Today,
                AdultCount = 2,
                ChildCount = 0,
                SelectedMealPeriod = "中午",
                AvailableTimeSlots = GetAvailableTimeSlots("中午", DateTime.Today),
                Menus = new List<Menu>()
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Index(ReservationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableTimeSlots = GetAvailableTimeSlots(model.SelectedMealPeriod, model.SelectedDate ?? DateTime.Today);
                model.Menus = new List<Menu>();
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

        //[HttpGet]
        //public IActionResult Confirm(int restaurantId, int branchId, DateTime selectedDate, int adultCount, int childCount, string selectedMealPeriod, string selectedTimeSlot)
        //{
        //    if (restaurantId == 0)
        //    {
        //        return RedirectToAction("Index", "Restaurant");
        //    }

        //    var model = new ReservationConfirmViewModel
        //    {
        //        Restaurant = new Reservation.Models.ViewModels.Restaurant { Id = restaurantId },
        //        Branch = new Branch { Id = branchId },
        //        SelectedDate = selectedDate,
        //        AdultCount = adultCount,
        //        ChildCount = childCount,
        //        SelectedMealPeriod = selectedMealPeriod ?? "中午",
        //        SelectedTimeSlot = selectedTimeSlot ?? string.Empty
        //    };

        //    return View(model);
        //}

        //[HttpPost]
        //public IActionResult Confirm(ReservationConfirmViewModel model)
        //{
        //    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        //    {
        //        return Json(new { success = true, message = "訂位成功" });
        //    }

        //    TempData["ReservationSuccess"] = "訂位資料已提交";
        //    return RedirectToAction("Index", "Restaurant");
        //}

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
