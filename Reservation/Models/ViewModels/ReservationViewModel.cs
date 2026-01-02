namespace Reservation.Models.ViewModels
{
    public class MenuModel
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class ReservationModel
    {
        public RestaurantInfoModel Restaurant { get; set; } = new();
        public BranchModel Branch { get; set; } = new();
        public DateTime? SelectedDate { get; set; }
        public int AdultCount { get; set; }
        public int ChildCount { get; set; }
        public string SelectedMealPeriod { get; set; } = string.Empty;
        public string? SelectedTimeSlot { get; set; }
        public List<string> AvailableTimeSlots { get; set; } = new();
        public List<MenuModel> Menus { get; set; } = new();
    }

    public class ReservationConfirmModel
    {
        public RestaurantInfoModel Restaurant { get; set; } = new();
        public BranchModel Branch { get; set; } = new();
        public DateTime SelectedDate { get; set; }
        public int AdultCount { get; set; }
        public int ChildCount { get; set; }
        public string SelectedMealPeriod { get; set; } = string.Empty;
        public string SelectedTimeSlot { get; set; } = string.Empty;
    }
}


