using Reservation.Models.DB;
using Reservation.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Reservation.Controllers
{
    public class RecordController : Controller
    {
        FEDSRESTAURANTEntities _db = new FEDSRESTAURANTEntities();
        public DateTime _Now = DateTime.Now;

        // GET: Record
        public ActionResult Index(int? memberId, string type)
        {
            List<Record> _results = new List<Record>();
            DateTime _shortTime = Convert.ToDateTime(_Now.ToShortDateString());
            if(memberId == 0)
            {
                ViewBag.Type = type;
                ViewBag.Member = memberId;
                return View(_results);
            }

            if(type == "Reserve")
            {
                var _mReserve = _db.MemberReserves.Where(o => o.memberId == memberId).OrderByDescending(o => o.CreateDate).ToList();
                if (_mReserve.Count > 0)
                {
                    foreach (var m in _mReserve)
                    {
                        string _rName = _db.RestaurantBranches.Where(o => o.id == m.BranchId && o.CompanyId == m.CompanyId).Select(o => o.Name).FirstOrDefault();
                        if (m.Datetime >= _Now)
                        {
                            _results.Add(new Record { creatOn = m.CreateDate.ToString("yyyy-MM-dd HH:mm"), adults = m.GroupSize, children = m.NumberOfKid, dateTime = m.Datetime, note = m.Note, status = "已完成訂位", restaurantName = _rName });
                        }
                        else if (m.Datetime < _Now)
                        {
                            _results.Add(new Record { creatOn = m.CreateDate.ToString("yyyy-MM-dd HH:mm"), adults = m.GroupSize, children = m.NumberOfKid, dateTime = m.Datetime, note = m.Note, status = "已過期", restaurantName = _rName });
                        }
                    }
                }
            }
            else if (type == "Wait")
            {
                var _mWait = _db.MemberWaitings.Where(o => o.memberId == memberId).OrderByDescending(o => o.CreateDate).ToList();
                if (_mWait.Count > 0)
                {
                    foreach (var m in _mWait)
                    {
                        string _rName = _db.RestaurantBranches.Where(o => o.id == m.BranchId && o.CompanyId == m.CompanyId).Select(o => o.Name).FirstOrDefault();
                        if (m.Datetime >= _Now)
                        {
                            _results.Add(new Record { creatOn = m.CreateDate.ToString("yyyy-MM-dd HH:mm"), adults = m.GroupSize, children = m.NumberOfKid, dateTime = m.Datetime, note = m.Note, status = "已完成訂位", restaurantName = _rName });
                        }
                        else if (m.Datetime < _Now)
                        {
                            _results.Add(new Record { creatOn = m.CreateDate.ToString("yyyy-MM-dd HH:mm"), adults = m.GroupSize, children = m.NumberOfKid, dateTime = m.Datetime, note = m.Note, status = "已過期", restaurantName = _rName });
                        }
                    }
                }
            }

            ViewBag.Type = type;
            ViewBag.Member = memberId;
            return View(_results);
        }

        public ActionResult Mb_record(int? memberId, string type)
        {
            List<Record> _results = new List<Record>();
            DateTime _shortTime = Convert.ToDateTime(_Now.ToShortDateString());
            if (memberId == 0)
            {
                ViewBag.Type = type;
                ViewBag.Member = memberId;
                return View(_results);
            }

            if (type == "Reserve")
            {
                var _mReserve = _db.MemberReserves.Where(o => o.memberId == memberId).OrderByDescending(o => o.CreateDate).ToList();
                if (_mReserve.Count > 0)
                {
                    foreach (var m in _mReserve)
                    {
                        string _rName = _db.RestaurantBranches.Where(o => o.id == m.BranchId && o.CompanyId == m.CompanyId).Select(o => o.Name).FirstOrDefault();
                        if (m.Datetime >= _Now)
                        {
                            _results.Add(new Record { creatOn = m.CreateDate.ToString("yyyy-MM-dd HH:mm"), adults = m.GroupSize, children = m.NumberOfKid, dateTime = m.Datetime, note = m.Note, status = "已完成訂位", restaurantName = _rName });
                        }
                        else if (m.Datetime < _Now)
                        {
                            _results.Add(new Record { creatOn = m.CreateDate.ToString("yyyy-MM-dd HH:mm"), adults = m.GroupSize, children = m.NumberOfKid, dateTime = m.Datetime, note = m.Note, status = "已過期", restaurantName = _rName });
                        }
                    }
                }
            }
            else if (type == "Wait")
            {
                var _mWait = _db.MemberWaitings.Where(o => o.memberId == memberId).OrderByDescending(o => o.CreateDate).ToList();
                if (_mWait.Count > 0)
                {
                    foreach (var m in _mWait)
                    {
                        string _rName = _db.RestaurantBranches.Where(o => o.id == m.BranchId && o.CompanyId == m.CompanyId).Select(o => o.Name).FirstOrDefault();
                        if (m.Datetime >= _Now)
                        {
                            _results.Add(new Record { creatOn = m.CreateDate.ToString("yyyy-MM-dd HH:mm"), adults = m.GroupSize, children = m.NumberOfKid, dateTime = m.Datetime, note = m.Note, status = "已完成訂位", restaurantName = _rName });
                        }
                        else if (m.Datetime < _Now)
                        {
                            _results.Add(new Record { creatOn = m.CreateDate.ToString("yyyy-MM-dd HH:mm"), adults = m.GroupSize, children = m.NumberOfKid, dateTime = m.Datetime, note = m.Note, status = "已過期", restaurantName = _rName });
                        }
                    }
                }
            }

            ViewBag.Type = type;
            ViewBag.Member = memberId;
            return View(_results);
        }
    }
}