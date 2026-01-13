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
    public class WaitingPositionOrder
    {
        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int Gender { get; set; } = 2;
        public int GroupSize { get; set; }
        public int NumberOfKidChairs { get; set; }
        public string CustomerNote { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string Language { get; set; } = "zh-TW";
        public DateTime Datetime { get; set; }
        public string CreatedFrom { get; set; } = "FEDSWEB";
    }
}

