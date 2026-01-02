namespace Reservation.Models.ViewModels
{
    public class ReservationRecordModel
    {
        public int ReservationId { get; set; }
        public int RestaurantId { get; set; }
        public string RestaurantName { get; set; } = string.Empty;
        public string RestaurantImageUrl { get; set; } = string.Empty;
        public string RestaurantLocation { get; set; } = string.Empty;
        public string RestaurantPhone { get; set; } = string.Empty;
        public DateTime ReservationDate { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public int AdultCount { get; set; }
        public int ChildCount { get; set; }
        public string Status { get; set; } = "完成";
    }

    public class WaitingRecordModel
    {
        public int WaitingId { get; set; }
        public int RestaurantId { get; set; }
        public string RestaurantName { get; set; } = string.Empty;
        public string RestaurantImageUrl { get; set; } = string.Empty;
        public string RestaurantLocation { get; set; } = string.Empty;
        public string RestaurantPhone { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
        public int AdultCount { get; set; }
        public int ChildCount { get; set; }
        public int QueueNumber { get; set; }
        public string Status { get; set; } = "等待中";
    }

    public class RecordQueryModel
    {
        public List<BranchModel> Branches { get; set; } = new();
        public int? SelectedBranchId { get; set; }
        public List<ReservationRecordModel> ReservationRecords { get; set; } = new();
        public List<WaitingRecordModel> WaitingRecords { get; set; } = new();
        public string MemberName { get; set; } = "黃O明";
    }
}


