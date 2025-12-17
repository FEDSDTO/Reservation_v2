using Reservation.Models.DB;

namespace Reservation.Models.ViewModels
{
    public class ReservationRecord
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
        public string Status { get; set; } = "完成"; // 完成/未入座/已取消
    }

    public class WaitingRecord
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

    public class RecordQueryViewModel
    {
        public List<Branch> Branches { get; set; } = new();
        public int? SelectedBranchId { get; set; }
        public List<ReservationRecord> ReservationRecords { get; set; } = new();
        public List<WaitingRecord> WaitingRecords { get; set; } = new();
        public string MemberName { get; set; } = "黃O明"; // 模擬會員名稱
    }
}