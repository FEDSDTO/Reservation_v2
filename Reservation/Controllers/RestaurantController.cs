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

        private List<CategoryModel> GetCategories()
        {
            return new List<CategoryModel>
            {
                new CategoryModel { Id = 0, Name = "所有餐廳" },
                new CategoryModel { Id = 1, Name = "主題餐廳" },
                new CategoryModel { Id = 2, Name = "輕食甜點" },
                new CategoryModel { Id = 3, Name = "吃到飽" }
            };
        }

        public async Task<IActionResult> Index(string? groupId, int? categoryId)
        {
            var mallGroups = await _restaurantService.GetMallsAsync();
            var branchDict = new Dictionary<string, int>();
            var groupIdToBranchId = new Dictionary<string, int>();
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
                groupIdToBranchId[mallGroups[i].GroupId] = branch.Id;
            }

            var allCategories = GetCategories();
            var restaurantCards = new List<RestaurantCardModel>();
            
            // ========== 主頁載入時，從 API 同步資料到資料庫 ==========
            if(string.IsNullOrWhiteSpace(groupId))
            {
                // 顯示所有分館：同步所有分館的資料
                foreach(var mallGroup in mallGroups)
                {
                    await _restaurantService.RestaurantApiAsync(mallGroup.GroupId);
                    var groupRestaurants = await _restaurantService.GetRestaurantsAsync(mallGroup.GroupId);
                    restaurantCards.AddRange(groupRestaurants);
                }
            }
            else
            {
                // 顯示特定分館：只同步該分館的資料
                await _restaurantService.RestaurantApiAsync(groupId);
                restaurantCards = await _restaurantService.GetRestaurantsAsync(groupId);
            }
            
            var viewModel = new RestaurantListModel
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

            var reservationRecords = new List<ReservationRecordModel>();
            var waitingRecords = new List<WaitingRecordModel>();

            if (branchId.HasValue)
            {
                var selectedGroupId = branchDict.FirstOrDefault(x => x.Value == branchId.Value).Key;
                if (!string.IsNullOrEmpty(selectedGroupId))
                {
                    var restaurantCards = await _restaurantService.GetRestaurantsAsync(selectedGroupId);
                }
            }

            var viewModel = new RecordQueryModel
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
