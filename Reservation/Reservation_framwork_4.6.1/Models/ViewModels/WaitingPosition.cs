using Reservation.ViewModels.Restaurants;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace Reservation.ViewModels.WaitingPosition
{
    public class WaitingPositionModel : Restaurant
    {
        /// <summary>
        /// 已登入會員帳號
        /// </summary>
        public string MemberAccount { get; set; }

        /// <summary>
        /// 等候人數
        /// </summary>
        public int WaitingCount { get; set; }

        /// <summary>
        /// 最大候位人數
        /// </summary>
        public int MaxWaitingGroupSize { get; set; }

        /// <summary>
        /// 當前候位資訊
        /// </summary>
        public WaitingInfo WaitingInfo { get; set; }

        /// <summary>
        /// 推薦餐廳
        /// </summary>
        public List<Restaurant> AlternateWaitingBranches { get; set; }
    }

    /// <summary>
    /// 餐廳分店候位資訊
    /// </summary>
    public class WaitingInfo
    {
        /// <summary>
        /// 預計等候時間分鐘
        /// </summary>
        public int EstimatedWaitingMinutes { get; set; }

        /// <summary>
        /// 候位中組數
        /// </summary>
        public int WaitingCount { get; set; }

        /// <summary>
        /// 線上候位狀態
        /// </summary>
        public string Status { get; set; }
    }

    /// <summary>
    /// 預約候位資料
    /// </summary>
    public class WaitingPositionOrder
    {
        /// <summary>
        /// 顧客名稱
        /// </summary>
        public string CustomerName { get; set; }

        /// <summary>
        /// 性別 0=男, 1=女, 2=無
        /// </summary>
        public int Gender { get; set; }

        /// <summary>
        /// 顧客電話
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// 應用語系
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// 訂位大人人數
        /// </summary>
        public int GroupSize { get; set; }

        /// <summary>
        /// 需求兒童以數量(本系統未提供)
        /// </summary>
        public int NumberOfKidSets { get; set; }

        /// <summary>
        /// 訂位小孩人數
        /// </summary>
        public int NumberOfKidChairs { get; set; }

        /// <summary>
        /// 候位時間
        /// </summary>
        public DateTime? Datetime { get; set; }

        /// <summary>
        /// 候位順序標籤(本系統不需傳送)
        /// </summary>
        //public string PositionInLineTag { get; set; }

        /// <summary>
        /// 後衛順序顯示文字(本系統不需傳送)
        /// </summary>
        //public string PositionInLineTagText { get; set; }

        /// <summary>
        /// 對應的FB粉絲頁ID(本系統不需傳送)
        /// </summary>
        //public string FbPageId { get; set; }

        /// <summary>
        /// 對應的FB粉絲頁user ID(本系統不需傳送)
        /// </summary>
        //public string FbUserId { get; set; }

        /// <summary>
        /// 系統備註
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// 顧客備註
        /// </summary>
        public string CustomerNote { get; set; }

        /// <summary>
        /// 訂單來源
        /// </summary>
        public string CreatedFrom => ConfigurationManager.AppSettings["ProviderId"].ToString();
    }
}