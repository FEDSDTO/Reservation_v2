namespace Reservation.Models.ViewModels
{
    public class WaitingPositionModel
    {
        public RestaurantInfoModel Restaurant { get; set; } = new();
        public BranchModel Branch { get; set; } = new();
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public int AdultCount { get; set; }
        public int ChildCount { get; set; }
        public int CurrentQueueCount { get; set; }
        public int QueueNumber { get; set; }
        public int AheadCount { get; set; }
        public int EstimatedWaitMinutes { get; set; }
    }
}

