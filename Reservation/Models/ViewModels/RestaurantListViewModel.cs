namespace Reservation.Models.ViewModels
{
    public class BranchModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CategoryModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class RestaurantInfoModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string OpeningHours { get; set; } = "10:00-22:00";
        public int BranchId { get; set; }
        public int CategoryId { get; set; }
        public bool IsPopular { get; set; }
        public bool IsNew { get; set; }
    }

    public class RestaurantListModel
    {
        public List<BranchModel> Branches { get; set; } = new();
        public List<CategoryModel> Categories { get; set; } = new();
        public List<RestaurantCardModel> RestaurantCards { get; set; } = new();
        public string? SelectedGroupId { get; set; }
        public int? SelectedBranchId { get; set; }
        public int? SelectedCategoryId { get; set; }
        public Dictionary<string, int> GroupIdToBranchId { get; set; } = new();
    }

    public class RestaurantListViewModel
    {
        public List<BranchModel> Branches { get; set; } = new();
        public List<CategoryModel> Categories { get; set; } = new();
        public List<RestaurantCardModel> RestaurantCards { get; set; } = new();
        public string? SelectedBranchId { get; set; }
        public int? SelectedCategoryId { get; set; }
        public Dictionary<string, string> GroupIdToBranchId { get; set; } = new();
    }
}
