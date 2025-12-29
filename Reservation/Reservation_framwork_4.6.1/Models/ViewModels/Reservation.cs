using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using Reservation.ViewModels.Restaurants;
using Newtonsoft.Json.Linq;

namespace Reservation.Models.ViewModels
{
    //餐廳訂位資訊
    public class ReservationIndex : Restaurant
    {
        public int memberId { get; set; }
        public string branchId { get; set; }
        public int adults { get; set; }
        public int children { get; set; }
        public string bookingDate { get; set; }
        public string bookingTime { get; set; }
        public string customerName { get; set; }
        public string customerPhone { get; set; }
        public string customerSex { get; set; }
        public string customerNode { get; set; }
    }

    //餐廳訂位限制條件
    public class GroupsAPI_result : Restaurant
    {
        public bool webBookingEnabled { get; set; }
        public int maxBookingGroupSize { get; set; }
        public int minBookingGroupSize { get; set; }
        public int minReservationOffset { get; set; }
        public int maxReservationOffset { get; set; }
        public bool webKidsSelectorHidden { get; set; }

        public JObject bookingInfo { get; set; }
    }
   

    public class bookingInfos
    {
        public  string time{ get; set; }
        public string status { get; set; }
        public string timePeriod { get; set; }
    }

    //會員登入Info
    public class Memeber_Info
    {
        public int Id { get; set; }
        public string name { get; set; }
        public string gender { get; set; }
    }

    //Inline 訂位API參數
    public class InlineAPI_Post
    {
        public string customerName { get; set; }
        public int gender { get; set; }
        public string phone { get; set; }
        public string language { get; set; }
        public int groupSize { get; set; }
        public int numberOfKidChairs { get; set; }
        public string datetime { get; set; }
        public string createdFrom { get; set; }
        public string customerNote { get; set; }
    }

    //Inline 訂位API時間和人數
    public class WaitingInfo
    {
        public int WaitingCount { get; set; }
        public int EstimatedMinutes { get; set; }
    }
}