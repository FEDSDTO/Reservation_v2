using System.ComponentModel;

namespace Reservation.Models.ViewModels
{
    public class RestaurantModel
    {
        [Description("餐廳分店 id")]
        public string id { get; set; } = string.Empty;

        [Description("分館ID")]
        public string GroupId { get; set; } = string.Empty;

        [Description("餐廳ID")]
        public string CompanyId { get; set; } = string.Empty;

        [Description("餐廳名稱")]
        public string Name { get; set; } = string.Empty;

        [Description("餐廳圖片")]
        public List<string> Images { get; set; } = new();

        [Description("餐廳地點")]
        public string Address { get; set; } = string.Empty;

        [Description("餐廳電話")]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class RestaurantCardModel : RestaurantModel
    {
        [Description("餐廳分店是否開放線上訂位")]
        public bool WebBookingEnabled{get;set;}
        [Description("餐廳分店是否開放線上候位")]
        public bool WebWaitingEnabled{get;set;}

         [Description("目前候位中組數")]
        public int WaitingCount { get; set; }

        [Description("目前預計等候時間分鐘")]
        public int? EstimatedWaitingMinutes { get; set; }
    }
}
