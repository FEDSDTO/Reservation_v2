using System.Linq;
using Microsoft.AspNetCore.Mvc;
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
                new Branch { Id = 2, Name = "遠百板橋" },
                new Branch { Id = 3, Name = "遠百台中" }
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
                    Name = "1010湘食堂", 
                    ImageUrl = "~/Image/1.jpg",
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
                    ImageUrl = "~/Image/1.jpg",
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
                    ImageUrl = "~/Image/1.jpg",
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
                    ImageUrl = "~/Image/1.jpg",
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
                    ImageUrl = "~/Image/1.jpg",
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

        // GET: /Restaurant/Index
        public IActionResult Index(int? branchId, int? categoryId, string? keyword)
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

            // 關鍵字搜尋
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                filteredRestaurants = filteredRestaurants.Where(r => 
                    r.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            var viewModel = new RestaurantListViewModel
            {
                Branches = allBranches,
                Categories = allCategories,
                Restaurants = filteredRestaurants.ToList(),
                SelectedBranchId = branchId,
                SelectedCategoryId = categoryId,
                SearchKeyword = keyword
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

