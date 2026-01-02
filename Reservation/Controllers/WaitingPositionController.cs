using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Reservation.Models.EFRestaurantModels;
using Reservation.Models.ViewModels;
using Reservation.Service;

namespace Reservation.Controllers
{
    public class WaitingPositionController : Controller
    {
        private static Dictionary<int, Queue<QueueInfo>> _queueData = new();
        private readonly RestaurantContext _restaurantContext;
        private readonly RestaurantService _restaurantService;
        
        private class QueueInfo
        {
            public int QueueNumber { get; set; }
            public int RestaurantId { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public string CustomerPhone { get; set; } = string.Empty;
            public int AdultCount { get; set; }
            public int ChildCount { get; set; }
            public DateTime JoinTime { get; set; }
        }

        public WaitingPositionController(
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

            var restaurantCards = await _restaurantService.GetRestaurantsAsync(branchId);
            var restaurantCard = restaurantCards.FirstOrDefault(r => r.id == restaurantId);
            
            if (restaurantCard == null)
            {
                return NotFound();
            }

            var restaurant = new Reservation.Models.ViewModels.RestaurantInfoModel
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

            var currentQueueCount = 0;
            if (_queueData.ContainsKey(restaurant.Id))
            {
                currentQueueCount = _queueData[restaurant.Id].Count;
            }

            var viewModel = new WaitingPositionModel
            {
                Restaurant = restaurant,
                Branch = selectedBranch ?? new BranchModel(),
                AdultCount = 2,
                ChildCount = 0,
                CurrentQueueCount = currentQueueCount
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Index(WaitingPositionModel model)
        {
            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });
                }
                
                if (_queueData.ContainsKey(model.Restaurant.Id))
                {
                    model.CurrentQueueCount = _queueData[model.Restaurant.Id].Count;
                }
                
                return View(model);
            }

            if (!_queueData.ContainsKey(model.Restaurant.Id))
            {
                _queueData[model.Restaurant.Id] = new Queue<QueueInfo>();
            }

            var queue = _queueData[model.Restaurant.Id];
            var queueNumber = queue.Count + 1;
            var aheadCount = queue.Count;

            var queueInfo = new QueueInfo
            {
                QueueNumber = queueNumber,
                RestaurantId = model.Restaurant.Id,
                CustomerName = model.CustomerName,
                CustomerPhone = model.CustomerPhone,
                AdultCount = model.AdultCount,
                ChildCount = model.ChildCount,
                JoinTime = DateTime.Now
            };

            queue.Enqueue(queueInfo);

            var estimatedWaitMinutes = aheadCount * 30;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new 
                { 
                    success = true, 
                    message = "候位成功",
                    queueNumber = queueNumber,
                    aheadCount = aheadCount,
                    estimatedWaitMinutes = estimatedWaitMinutes,
                    currentQueueCount = queue.Count
                });
            }

            model.QueueNumber = queueNumber;
            model.AheadCount = aheadCount;
            model.EstimatedWaitMinutes = estimatedWaitMinutes;
            model.CurrentQueueCount = queue.Count;

            TempData["QueueSuccess"] = "候位成功";
            return View(model);
        }

        [HttpGet]
        public IActionResult GetQueueStatus(int restaurantId)
        {
            var currentQueueCount = 0;
            if (_queueData.ContainsKey(restaurantId))
            {
                currentQueueCount = _queueData[restaurantId].Count;
            }
            return Json(new { currentQueueCount = currentQueueCount });
        }
    }
}
