namespace Reservation.Models.ViewModels
{
    public class Branch
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Restaurant
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

    public class RestaurantListViewModel
    {
        public List<Branch> Branches { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<RestaurantCardModel> RestaurantCards { get; set; } = new();
        public string? SelectedGroupId { get; set; }
        public int? SelectedBranchId { get; set; }
        public int? SelectedCategoryId { get; set; }
        public Dictionary<string, int> GroupIdToBranchId { get; set; } = new();
    }
}
