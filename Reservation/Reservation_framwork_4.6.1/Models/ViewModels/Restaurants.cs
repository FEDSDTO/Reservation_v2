using Reservation.ViewModels.WaitingPosition;
using System.Collections.Generic;
using System.ComponentModel;

namespace Reservation.ViewModels.Restaurants
{
    /// <summary>
    ///  RestaurantsIndex ViewModel
    /// </summary>
    public class RestaurantsIndex
    {
        /// <summary>
        /// 餐廳頁面資料
        /// </summary>
        public List<RestaurantCard> Cards { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        public RestaurantsIndex()
        {

        }

        /// <summary>
        /// 付值初始化
        /// </summary>
        /// <param name="cards"></param>
        public RestaurantsIndex(List<RestaurantCard> cards)
        {
            Cards = cards;
        }
    }

    /// <summary>
    /// mall_filter ViewModel
    /// </summary>
    public class MallFilter
    {
        /// <summary>
        /// 分公司列表
        /// </summary>
        public List<Mall> Malls { get; set; }
    }

    /// <summary>
    /// 分館
    /// </summary>
    public class Mall
    {
        /// <summary>
        /// 分館ID
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// 餐廳資訊
    /// </summary>
    public class Company
    {
        /// <summary>
        /// Company ID
        /// </summary>
        [Description("Company ID")]
        public string id { get; set; }

        /// <summary>
        /// 餐廳品牌名稱
        /// </summary>
        [Description("餐廳品牌名稱")]
        public string Name { get; set; }

        /// <summary>
        /// 餐廳品牌Logo
        /// </summary>
        [Description("餐廳品牌Logo")]
        public string Logo { get; set; }

        /// <summary>
        /// 餐廳品牌標籤
        /// </summary>
        [Description("餐廳品牌標籤")]
        public List<Tag> Tags { get; set; }
    }

    /// <summary>
    /// 餐廳類型標籤
    /// </summary>
    public class Tag
    {
        /// <summary>
        /// Tag ID
        /// </summary>
        [Description("Tag ID")]
        public string id { get; set; }

        /// <summary>
        /// 類型名稱
        /// </summary>
        [Description("類型名稱")]
        public string Name { get; set; }
    }

    /// <summary>
    /// 餐廳營業時間
    /// </summary>
    public class OpeningTime
    {
        /// <summary>
        /// 開始時間
        /// </summary>
        [Description("開始時間")]
        public string From { get; set; }

        /// <summary>
        /// 結束時間
        /// </summary>
        [Description("結束時間")]
        public string To { get; set; }
    }

    /// <summary>
    /// 餐廳資料
    /// </summary>
    public class Restaurant
    {
        /// <summary>
        /// 餐廳分店 id
        /// </summary>
        [Description("餐廳分店 id")]
        public string id { get; set; }

        /// <summary>
        /// 分館ID
        /// </summary>
        [Description("分館ID")]
        public string GroupId { get; set; }

        /// <summary>
        /// 餐廳ID
        /// </summary>
        [Description("餐廳ID")]
        public string CompanyId { get; set; }

        /// <summary>
        /// 餐廳資訊
        /// </summary>
        [Description("餐廳資訊")]
        public Company Company { get; set; }

        /// <summary>
        /// 餐廳名稱
        /// </summary>
        [Description("餐廳名稱")]
        public string Name { get; set; }

        /// <summary>
        /// 餐廳圖片
        /// </summary>
        [Description("餐廳圖片")]
        public List<string> Images { get; set; }

        /// <summary>
        /// 餐廳地點
        /// </summary>
        [Description("餐廳地點")]
        public string Address { get; set; }

        /// <summary>
        /// 餐廳分店電話
        /// </summary>
        [Description("餐廳分店電話")]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// 餐廳分店營業時間
        /// </summary>
        [Description("餐廳分店營業時間")]
        public List<OpeningTime> OpeningTimes { get; set; }

        /// <summary>
        /// 菜單
        /// </summary>
        [Description("菜單")]
        public List<string> Menus { get; set; }

    }

    /// <summary>
    /// 餐廳列表資料
    /// </summary>
    public class RestaurantCard : Restaurant
    {
        /// <summary>
        /// 餐廳分店是否開放線上訂位
        /// </summary>
        [Description("餐廳分店是否開放線上訂位")]
        public bool WebBookingEnabled { get; set; }

        /// <summary>
        /// 餐廳分店是否開放線上候位
        /// </summary>
        [Description("餐廳分店是否開放線上候位")]
        public bool WebWaitingEnabled { get; set; }

        /// <summary>
        /// 餐廳分店是否開放線上訂餐外帶
        /// </summary>
        [Description("餐廳分店是否開放線上訂餐外帶")]
        public bool? OnlineOrderEnabled { get; set; }

        /// <summary>
        /// 餐廳分店是否開放訂餐外送
        /// </summary>
        [Description("餐廳分店是否開放訂餐外送")]
        public bool? WebDeliveryEnabled { get; set; }

        /// <summary>
        /// 餐廳分店是否開放速利便
        /// </summary>
        [Description("餐廳分店是否開放速利便")]
        public bool? EasyFastEnabled { get; set; }

        /// <summary>
        /// 目前候位中組數
        /// </summary>
        [Description("目前候位中組數")]
        public int WaitingCount { get; set; }

        /// <summary>
        /// 當前候位資訊
        /// </summary>
        public WaitingInfo WaitingInfo { get; set; }

        /// <summary>
        /// 目前預計等候時間分鐘，由餐廳平板控制
        /// </summary>
        [Description("目前預計等候時間分鐘")]
        public int? EstimatedWaitingMinutes { get; set; }

    }
}