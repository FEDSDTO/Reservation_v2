using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Reservation.Models.DB;
using Reservation.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Reservation.Models
{
    public class Reservations
    {
        private FEDSRESTAURANTEntities _db = new FEDSRESTAURANTEntities();
        private FEDSMBREntities _mdb = new FEDSMBREntities();

        /// <summary>
        /// 會員查詢
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Memeber_Info GetMember(int Id)
        {
            Memeber_Info _M = _mdb.Member.Where(o => o.Id == Id).Select(o => new Memeber_Info { Id = o.Id, name = o.Name, gender = o.Gender }).FirstOrDefault();
            return _M;
        }

        /// <summary>
        /// 取得狀態open的訂位時間
        /// </summary>
        /// <param name="bookingInfo"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public List<bookingInfos> GetBookingTime(JObject bookingInfo, string date)
        {
            List<bookingInfos> result = new List<bookingInfos>();
            JObject bTime = (JObject)bookingInfo.GetValue(date);

            if(bTime != null)
            {
                var timeProperties = bTime.Properties();
                foreach (var k in timeProperties)
                {
                    if (k.Value.ToString() == "open")
                    {
                        string[] kArray = k.Name.Split(':');
                        if (Convert.ToInt32(kArray[0]) >= 17)
                        {
                            result.Add(new bookingInfos { time = k.Name, status = k.Value.ToString(), timePeriod = "PM" });
                        }
                        else
                        {
                            result.Add(new bookingInfos { time = k.Name, status = k.Value.ToString(), timePeriod = "AM" });
                        }
                    }
                }
            }

            return result;
        }
    }
}