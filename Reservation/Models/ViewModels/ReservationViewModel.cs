namespace Reservation.Models.ViewModels
{
    public class Menu
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class ReservationViewModel
    {
        public Restaurant Restaurant { get; set; } = new();
        public Branch Branch { get; set; } = new();
        public DateTime? SelectedDate { get; set; }
        public int AdultCount { get; set; }
        public int ChildCount { get; set; }
        public string SelectedMealPeriod { get; set; } = string.Empty;
        public string? SelectedTimeSlot { get; set; }
        public List<string> AvailableTimeSlots { get; set; } = new();
        public List<Menu> Menus { get; set; } = new();
    }

    public class ReservationConfirmViewModel
    {
        public Restaurant Restaurant { get; set; } = new();
        public Branch Branch { get; set; } = new();
        public DateTime SelectedDate { get; set; }
        public int AdultCount { get; set; }
        public int ChildCount { get; set; }
        public string SelectedMealPeriod { get; set; } = string.Empty;
        public string SelectedTimeSlot { get; set; } = string.Empty;
    }
}


