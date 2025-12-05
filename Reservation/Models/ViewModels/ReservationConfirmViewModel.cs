using Reservation.Models.DB;

namespace Reservation.Models.ViewModels
{
    public class ReservationConfirmViewModel
    {
        public Restaurant Restaurant { get; set; } = new();
        public Branch Branch { get; set; } = new();
        public DateTime SelectedDate { get; set; }
        public int AdultCount { get; set; }
        public int ChildCount { get; set; }
        public string SelectedMealPeriod { get; set; } = string.Empty;
        public string SelectedTimeSlot { get; set; } = string.Empty;
        
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerTitle { get; set; } = "先生";
        public string CustomerPhone { get; set; } = string.Empty;
        public string DiningPurpose { get; set; } = "聚餐";
        public string? Remarks { get; set; }
    }
}

