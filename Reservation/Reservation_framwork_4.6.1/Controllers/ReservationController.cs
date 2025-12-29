using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reservation.Models;
using Reservation.Models.DB;
using Reservation.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Reservation.Models.Log;

namespace Reservation.Controllers
{
    public class ReservationController : Controller
    {
        Reservations _rfun = new Reservations();
        Models.Member _mfun = new Models.Member();
        FEDSRESTAURANTEntities _db = new FEDSRESTAURANTEntities();
        public string _Now = DateTime.Now.ToString("yyyy-MM-dd");

        // GET: Reservation 訂位
        public ActionResult Index(string id, string companyId, string branchId, int? memberId)
        {
            // 取得分公司餐廳
            ApiResult apiResult = InlineApps.GetInlineappsBranch(id, companyId, branchId, InlineApps.GroupType.booking, size: 2);
            if (apiResult.code != 200)
                return RedirectToAction("Index", "Restaurants");

            try
            {
                // 解析API資料
                GroupsAPI_result _Result = JsonConvert.DeserializeObject<GroupsAPI_result>(apiResult.data);

                //取得預設今日之訂位時間
                List<bookingInfos> bookings = new List<bookingInfos>();
                bookings = _rfun.GetBookingTime((JObject)_Result.bookingInfo.GetValue("default"), _Now);

                //Layout餐廳資訊
                LayResult _LayResult = new LayResult(_Result) { ShowMenu = true };

                //會員
                //string _mAccount = Models.Member.GetMemberAccount();
                //string _mAccount = "300147";
                //int _mId = _mfun.GetMemberIdByAccount(_mAccount);
                //if (_mId > 0)
                //{
                //    ViewBag.Member = _mId;
                //}
                ViewBag.BookingTime = bookings;
                ViewBag.Infor = _Result;
                ViewBag.LayInfor = _LayResult;
            }
            catch (Exception ex)
            {
                TempData["ErrorMsg"] = "餐廳資料錯誤";
                return RedirectToAction("Index", "Restaurants", new { id = id });
            }

            return View();
        }

        public ActionResult CheckInfo(int num, string date, string loc)
        {
            string[] sArray = loc.Split(new char[2] { '=', '&' });
            string id = sArray[1];
            string companyId = sArray[3];
            string branchId = sArray[5];

            // 取得分公司餐廳
            ApiResult apiResult = InlineApps.GetInlineappsBranch(id, companyId, branchId, InlineApps.GroupType.booking, size: num);
            if (apiResult.code != 200)
                return RedirectToAction("Index", "Restaurants");
            // 解析API資料
            JObject data = JObject.Parse(apiResult.data);

            //所選日期下可訂位時間
            JObject bookingInfo = (JObject)data.GetValue("bookingInfo").First().First();
            List<bookingInfos> bookings = new List<bookingInfos>();
            bookings = _rfun.GetBookingTime(bookingInfo, date);
            
            return Json(bookings, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Reservation_info(ReservationIndex result, string loc)
        {
            string[] sArray = loc.Split(new char[2] { '=', '&' });
            int memberId = _mfun.GetMemberIdByAccount(Models.Member.GetMemberAccount());
            string id = sArray[1];
            string companyId = sArray[3];
            string branchId = sArray[5];

            result.memberId = memberId;
            result.CompanyId = companyId;
            result.branchId = branchId;
            //會員身分
            Memeber_Info _mInfo = new Memeber_Info();
            if (memberId > 0)
            {
                _mInfo = _rfun.GetMember(Convert.ToInt32(memberId));
                result.customerName = _mInfo.name;
            }

            // 取得分公司餐廳
            ApiResult apiResult = InlineApps.GetInlineappsBranch(id, companyId, branchId, InlineApps.GroupType.all, size: 2);
            if (apiResult.code != 200)
                return RedirectToAction("Index", "Restaurants");
            //Layout餐廳資訊
            GroupsAPI_result model = JsonConvert.DeserializeObject<GroupsAPI_result>(apiResult.data);
            LayResult _LayResult = new LayResult(model) { ShowMenu = true };
            ViewBag.LayInfor = _LayResult;
            ViewBag.Member = Convert.ToInt32(memberId);
            ViewBag.ReservationInfo = result;
            return View();
        }

        public ActionResult Reservation_submit(ReservationIndex reservation)
        {

            //string log = $@"
            //===== 訂位資料 =====
            //memberId: {reservation.memberId}
            //branchId: {reservation.branchId}
            //adults: {reservation.adults}
            //children: {reservation.children}
            //bookingDate: {reservation.bookingDate}
            //bookingTime: {reservation.bookingTime}
            //customerName: {reservation.customerName}
            //customerPhone: {reservation.customerPhone}
            //customerSex: {reservation.customerSex}
            //customerNode: {reservation.customerNode}
            //=====================
            //";

            //Func_Log.InsertLog(LogType.Info,log);

            DateTime _Now = DateTime.Now;
            int sex = 2;
            if (reservation.customerSex == "0")
                sex = 0;
            else if (reservation.customerSex == "1")
                sex = 1;

            if (reservation != null)
            {
                if(reservation.customerNode == null)
                    reservation.customerNode = "";

                string dt = reservation.bookingDate + " " + reservation.bookingTime + ":00";
                DateTime b_datetime = Convert.ToDateTime(dt).AddHours(-8);
                string ddt = b_datetime.ToString("s") + "Z";
                bool _p = false;
                _p = System.Text.RegularExpressions.Regex.IsMatch(reservation.customerPhone, @"^09[0-9]{8}$");
                if(_p == true)
                    reservation.customerPhone = "+886" + reservation.customerPhone.Substring(1);
                else
                {
                    var _presult = new
                    {
                        IsSuccess = false,
                        Message = "手機電話格式不正確"
                    };
                    return Content(Newtonsoft.Json.JsonConvert.SerializeObject(_presult), "application/json");
                }

                //inline餐訂參數
                InlineAPI_Post _r = new InlineAPI_Post
                {
                    customerName = reservation.customerName,
                    gender = sex,
                    phone = reservation.customerPhone,
                    language = "zh-TW",
                    groupSize = reservation.adults,
                    numberOfKidChairs = reservation.children,
                    datetime = ddt,
                    customerNote = reservation.customerNode,
                    createdFrom = "FEDSWEB"
                } ;

                Func_Log.InsertLog(LogType.Info, Newtonsoft.Json.JsonConvert.SerializeObject(_r));

                //傳送餐廳訂位資訊
                ApiResult apiResult = InlineApps.PostInlineappsApi($"/reservations/{reservation.CompanyId}/{reservation.branchId}", _r);
                if (apiResult.code != 200)
                {
                    var _sresult = new
                    {
                        IsSuccess = false,
                        Message = "聯絡客服單位"
                    };
                    return Content(Newtonsoft.Json.JsonConvert.SerializeObject(_sresult), "application/json");
                }

                List<MemberReserveLog> _log = new List<MemberReserveLog>();
                MemberReserveLog memberReserveLog = new MemberReserveLog
                {
                    Status = "N",
                    Json = "",
                    Creator = 0,
                    CreateDate = DateTime.Now,
                    CreateFrom = "FEDS-SYS"
                };
                _log.Add(memberReserveLog);

                _db.MemberReserves.Add(new MemberReserve
                {
                    memberId = reservation.memberId,
                    CompanyId = reservation.CompanyId,
                    BranchId = reservation.branchId,
                    GroupSize = reservation.adults,
                    NumberOfKid = reservation.children,
                    ContactName = reservation.customerName,
                    ContactPhone = reservation.customerPhone,
                    ContactGender = Convert.ToByte(sex),
                    Datetime = Convert.ToDateTime(dt),
                    Note = reservation.customerNode,
                    Creator = 0,
                    CreateDate = DateTime.Now,
                    CreateFrom = "FEDS-SYS",
                    MemberReserveLogs = _log
                });
                _db.SaveChanges();

                var result = new
                {
                    IsSuccess = true
                };
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(result), "application/json");
            }
            else
            {
                var result = new
                {
                    IsSuccess = false,
                    Message = "聯絡客服單位"
                };
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(result), "application/json");
            }
        }
    }
}