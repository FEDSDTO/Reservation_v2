namespace Reservation.Models.ViewModels
{
    /// <summary>
    /// 訂位提交資料模型
    /// </summary>
    public class ReservationSubmitModel
    {
        public string GroupId {get;set;}=string.Empty;
        public string CompanyId {get;set;}=string.Empty;
        public string BranchId {get;set;}=string.Empty;
        public string CustomerName {get;set;}=string.Empty;
        public int Gender {get;set;}=0; // 0=先生, 1=小姐, 2=未指定
        public string Phone {get;set;}=string.Empty;
        public int AdultCount {get;set;}
        public int ChildCount {get;set;}
        public DateTime BookingDate {get;set;}
        public string BookingTime {get;set;}=string.Empty;
        public string? Note {get;set;}
    }
    /// <summary>
    /// API 的訂位資料格式
    /// </summary>
    public class InlineApiPostModel
    {
        public string customerName {get;set;}=string.Empty;
        public int gender {get;set;}
        public string phone {get;set;}=string.Empty;
        public string language{get;set;}="zh-TW";
        public int groupSize{get;set;}
        public int numberOfKidChairs { get; set; }
        public string datetime { get; set; } = string.Empty; // ISO 8601 UTC 格式
        public string customerNote { get; set; } = string.Empty;
        public string createdFrom { get; set; } = "FEDSWEB";
    }
    /// <summary>
    /// 可訂位時段資訊
    /// </summary>
    public class BookingTimeInfo
    {
        public string Time { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string TimePeriod { get; set; } = string.Empty; // "AM" or "PM"
    }
}
