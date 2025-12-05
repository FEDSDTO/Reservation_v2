using Reservation.Models.DB;

namespace Reservation.Models.ViewModels
{
    public class WaitingPositionViewModel
    {
        public Restaurant Restaurant { get; set; } = new();
        public Branch Branch { get; set; } = new();
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public int AdultCount { get; set; } = 2;
        public int ChildCount { get; set; } = 0;
        public int CurrentQueueCount { get; set; } = 0;
        public int? QueueNumber { get; set; }
        public int? AheadCount { get; set; }
        public int? EstimatedWaitMinutes { get; set; }
    }
}

