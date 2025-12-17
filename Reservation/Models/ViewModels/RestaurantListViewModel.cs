using Reservation.Models.DB;

namespace Reservation.Models.ViewModels
{
    public class RestaurantListViewModel
    {
        public List<Branch> Branches { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Restaurant> Restaurants { get; set; } = new();
        public int? SelectedBranchId { get; set; }
        public int? SelectedCategoryId { get; set; }
    }
}


