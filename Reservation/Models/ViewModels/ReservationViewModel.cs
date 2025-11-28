using Reservation.Models.DB;

namespace Reservation.Models.ViewModels
{
    public class ReservationViewModel
    {
        public Restaurant Restaurant { get; set; } = new();
        public Branch Branch { get; set; } = new();
        public DateTime? SelectedDate { get; set; }
        public int AdultCount { get; set; } = 2;
        public int ChildCount { get; set; } = 0;
        public string SelectedMealPeriod { get; set; } = "中午"; // "中午" 或 "晚上"
        public string? SelectedTimeSlot { get; set; } // 如 "11:00"
        public List<string> AvailableTimeSlots { get; set; } = new(); // 可用時段列表
    }
}

