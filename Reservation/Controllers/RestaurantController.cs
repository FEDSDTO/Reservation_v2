using System.Linq;
using Microsoft.AspNetCore.Mvc;
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
        private const string TokenCookieName = "MemberToken";

        public RestaurantController(
            RestaurantContext restaurantContext,
            MemberContext memberContext,
            RestaurantService restaurantService)
        {
            _restaurantContext = restaurantContext;
            _memberContext = memberContext;
            _restaurantService = restaurantService;
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

        public async Task<IActionResult> Index(string? groupId, int? categoryId)
        {
            var mallGroups = await _restaurantService.GetMallsAsync();
            
            var branchDict = new Dictionary<string, int>();
            var groupIdToBranchId = new Dictionary<string, int>();
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
                groupIdToBranchId[mallGroups[i].GroupId] = branch.Id;
            }

            var allCategories = GetCategories();
            var restaurantCards = new List<RestaurantCardModel>();
            
            // 直接使用 groupId，不需要轉換
            if (!string.IsNullOrWhiteSpace(groupId))
            {
                restaurantCards = await _restaurantService.GetRestaurantsAsync(groupId);
            }

            var viewModel = new RestaurantListViewModel
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

            var reservationRecords = new List<ReservationRecord>();
            var waitingRecords = new List<WaitingRecord>();

            if (branchId.HasValue)
            {
                var selectedGroupId = branchDict.FirstOrDefault(x => x.Value == branchId.Value).Key;
                if (!string.IsNullOrEmpty(selectedGroupId))
                {
                    var restaurantCards = await _restaurantService.GetRestaurantsAsync(selectedGroupId);
                }
            }

            var viewModel = new RecordQueryViewModel
            {
                Branches = branches,
                SelectedBranchId = branchId,
                ReservationRecords = reservationRecords,
                WaitingRecords = waitingRecords
            };

            return View(viewModel);
        }

        public IActionResult Queue(int id)
        {
            return View();
        }
    }
}
