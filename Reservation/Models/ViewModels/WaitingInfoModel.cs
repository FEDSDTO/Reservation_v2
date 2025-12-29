namespace Reservation.Models.ViewModels
{
    /// <summary>
    /// 餐廳分店候位資訊
    /// </summary>
    public class WaitingInfoModel
    {
        /// <summary>預計等候時間（分鐘）</summary>
        public int EstimatedMinutes { get; set; }

        /// <summary>候位中組數</summary>
        public int WaitingCount { get; set; }
    }
}
