using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Reservation.Models.DB;
using Reservation.Models.ViewModels;

namespace Reservation.Controllers
{
    public class RestaurantController : Controller
    {
        // 模擬數據服務（後續可替換為資料庫）
        private List<Branch> GetBranches()
        {
            return new List<Branch>
            {
                new Branch { Id = 1, Name = "遠百信義A13" },
                new Branch { Id = 2, Name = "板橋大遠百" },
                new Branch { Id = 3, Name = "台中大遠百" }
            };
        }

        private List<Category> GetCategories()
        {
            return new List<Category>
            {
                new Category { Id = 0, Name = "所有餐廳" },
                new Category { Id = 1, Name = "主題餐廳" },
                new Category { Id = 2, Name = "輕食甜點" },
                new Category { Id = 3, Name = "吃到飽" }
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

        // GET: /Restaurant/Index
        public IActionResult Index(int? branchId, int? categoryId)
        {
            var allBranches = GetBranches();
            var allCategories = GetCategories();
            var allRestaurants = GetRestaurants();

            // 篩選邏輯
            var filteredRestaurants = allRestaurants.AsQueryable();

            // 分店篩選
            if (branchId.HasValue)
            {
                filteredRestaurants = filteredRestaurants.Where(r => r.BranchId == branchId.Value);
            }

            // 類別篩選（0 表示所有餐廳）
            if (categoryId.HasValue && categoryId.Value != 0)
            {
                filteredRestaurants = filteredRestaurants.Where(r => r.CategoryId == categoryId.Value);
            }

            var viewModel = new RestaurantListViewModel
            {
                Branches = allBranches,
                Categories = allCategories,
                Restaurants = filteredRestaurants.ToList(),
                SelectedBranchId = branchId,
                SelectedCategoryId = categoryId
            };

            return View(viewModel);
        }
        
        public IActionResult QueryRecord(int? branchId)
        {
            var allBranches = GetBranches();
            var allRestaurants = GetRestaurants();

            var reservationRecords = new List<ReservationRecord>
            {
                 new ReservationRecord
                {
                    ReservationId = 1,
                    RestaurantId = 1,
                    RestaurantName = "1010湘食堂",
                    RestaurantImageUrl = "~/Image/1.jpg",
                    RestaurantLocation = "4F懷舊食光埕",
                    RestaurantPhone = "02-2729-0597",
                    ReservationDate = new DateTime(2025, 12, 25),
                    DayOfWeek = "星期四",
                    AdultCount = 2,
                    ChildCount = 0,
                    Status = "完成"
                },
                new ReservationRecord
                {
                    ReservationId = 2,
                    RestaurantId = 2,
                    RestaurantName = "新馬辣經典麻辣鍋",
                    RestaurantImageUrl = "~/Image/1.jpg",
                    RestaurantLocation = "台北市信義區松仁路58號4樓",
                    RestaurantPhone = "02-2729-0597",
                    ReservationDate = new DateTime(2025, 12, 26),
                    DayOfWeek = "星期五",
                    AdultCount = 4,
                    ChildCount = 1,
                    Status = "未入座"
                },
                new ReservationRecord
                {
                    ReservationId = 3,
                    RestaurantId = 3,
                    RestaurantName = "筷炒台式餐館",
                    RestaurantImageUrl = "~/Image/1.jpg",
                    RestaurantLocation = "台北市信義區松仁路58號7樓",
                    RestaurantPhone = "02-2729-0597",
                    ReservationDate = new DateTime(2025, 12, 27),
                    DayOfWeek = "星期六",
                    AdultCount = 2,
                    ChildCount = 0,
                    Status = "已取消"
                }
            };

            var WaitingRecords = new List<WaitingRecord>();

            if(branchId.HasValue)
            {
                var branchRestaurantId = allRestaurants
                .Where(r=>r.BranchId == branchId.Value)
                .Select(r=>r.Id)
                .ToList();

                reservationRecords = reservationRecords
                .Where(r=>branchRestaurantId.Contains(r.RestaurantId))
                .ToList();

                WaitingRecords = WaitingRecords
                .Where(w=>branchRestaurantId.Contains(w.RestaurantId))
                .ToList();
            }

            var viewModel = new RecordQueryViewModel
            {
                Branches = allBranches,
                SelectedBranchId = branchId,
                ReservationRecords = reservationRecords,
                WaitingRecords = WaitingRecords
            };

            return View(viewModel);
        }
        // 候位功能
        public IActionResult Queue(int id)
        {
            // TODO: 實作候位邏輯
            return View();
        }
    }
}

