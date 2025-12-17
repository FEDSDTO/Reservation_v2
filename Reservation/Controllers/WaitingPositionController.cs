 using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Reservation.Models.DB;
using Reservation.Models.ViewModels;

namespace Reservation.Controllers
{
    public class WaitingPositionController : Controller
    {
        private static Dictionary<int, Queue<QueueInfo>> _queueData = new();
        
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
                    ImageUrl = "~/Image/1.jpg",
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
                    Name = "新馬辣經典麻辣鍋", 
                    ImageUrl = "~/Image/1.jpg",
                    Location = "台北市信義區松仁路58號4樓",
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
                    Name = "筷炒台式餐館", 
                    ImageUrl = "~/Image/1.jpg",
                    Location = "台北市信義區松仁路58號7樓",
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
                    Name = "四川吳抄手", 
                    ImageUrl = "~/Image/1.jpg",
                    Location = "台北市信義區松仁路58號14樓（遠東信義A13）",
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
                    Name = "寰隆餐飲企業", 
                    ImageUrl = "~/Image/1.jpg",
                    Location = "台北市信義區松仁路58號4樓",
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
                    Name = "香米泰國料理", 
                    ImageUrl = "~/Image/1.jpg",
                    Location = "台北市信義區松仁路58號30樓",
                    Phone = "02-2729-5678",
                    OpeningHours = "10:00-22:00",
                    BranchId = 1,
                    CategoryId = 3,
                    IsPopular = false,
                    IsNew = false
                }
            };
        }

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

            var currentQueueCount = 0;
            if (_queueData.ContainsKey(restaurant.Id))
            {
                currentQueueCount = _queueData[restaurant.Id].Count;
            }

            var viewModel = new WaitingPositionViewModel
            {
                Restaurant = restaurant,
                Branch = branch,
                AdultCount = 2,
                ChildCount = 0,
                CurrentQueueCount = currentQueueCount
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Index(WaitingPositionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });
                }
                
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
