using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reservation.Models;
using Reservation.Models.DB;
using Reservation.ViewModels.WaitingPosition;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Reservation.Controllers
{
    public class WaitingPositionController : Controller
    {
        FEDSRESTAURANTEntities _db = new FEDSRESTAURANTEntities();

        // GET: WaitingPosition
        public ActionResult Index(string groupId, string companyId, string branchId)
        {
            // 驗證 GroupId 若未傳則導回首頁
            if (string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(branchId))
            {
                TempData["ErrorMsg"] = "查無此餐廳";
                return RedirectToAction("Index", "Restaurants", new { id = groupId });
            }

            ApiResult apiResult = InlineApps.GetInlineappsBranch(groupId, companyId, branchId, InlineApps.GroupType.waiting);
            if (apiResult.code != 200)
            {
                TempData["ErrorMsg"] = "查無此餐廳";
                return RedirectToAction("Index", "Restaurants", new { id = groupId });
            }

            try
            {
                WaitingPositionModel model = JsonConvert.DeserializeObject<WaitingPositionModel>(apiResult.data);
                if (!model.WaitingInfo.Status.ToLower().Equals("open"))
                {
                    TempData["ErrorMsg"] = "餐廳目前無提供候位服務";
                    return RedirectToAction("Index", "Restaurants", new { id = groupId });
                }

                LayResult _LayResult = new LayResult(model)
                {
                    ShowMenu = true,
                    ShowAlternateBranches = model.AlternateWaitingBranches.Count > 0
                };
                ViewBag.LayInfor = _LayResult;
                model.AlternateWaitingBranches = model.AlternateWaitingBranches ?? new List<ViewModels.Restaurants.Restaurant>();
                model.MemberAccount = Models.Member.GetMemberAccount();

                return View(model);
            }
            catch
            {
                TempData["ErrorMsg"] = "餐廳資料錯誤";
                return RedirectToAction("Index", "Restaurants", new { id = groupId });
            }
        }

        [HttpGet]
        public ActionResult Waitingposition_info(string groupId, string companyId, string branchId)
        {
            // 驗證 GroupId 若未傳則導回首頁
            if (string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(branchId))
            {
                TempData["ErrorMsg"] = "查無此餐廳";
                return RedirectToAction("Index", "Restaurants", new { id = groupId });
            }

            ApiResult apiResult = InlineApps.GetInlineappsBranch(groupId, companyId, branchId, InlineApps.GroupType.waiting);
            if (apiResult.code != 200)
            {
                TempData["ErrorMsg"] = "查無此餐廳";
                return RedirectToAction("Index", "Restaurants", new { id = groupId });
            }

            try
            {
                WaitingPositionModel model = JsonConvert.DeserializeObject<WaitingPositionModel>(apiResult.data);
                if (!model.WaitingInfo.Status.ToLower().Equals("open"))
                {
                    TempData["ErrorMsg"] = "餐廳目前無提供候位服務";
                    return RedirectToAction("Index", "Restaurants", new { id = groupId });
                }

                LayResult _LayResult = new LayResult(model);
                ViewBag.LayInfor = _LayResult;

                if (model.AlternateWaitingBranches == null)
                    model.AlternateWaitingBranches = new List<ViewModels.Restaurants.Restaurant>();

                return View(model);
            }
            catch
            {
                TempData["ErrorMsg"] = "查無此餐廳";
                return RedirectToAction("Index", "Restaurants", new { id = groupId });
            }
        }

        [HttpPost]
        public ActionResult Waitingposition_info(string groupId, string companyId, string branchId, WaitingPositionOrder waitingPositionOrder)
        {
            if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(branchId) ||
                string.IsNullOrWhiteSpace(waitingPositionOrder.CustomerName))
                return Content(JsonConvert.SerializeObject(new { IsSuccess = false, Msg = "查無此餐廳" }), "application/json");

            waitingPositionOrder.Datetime = DateTime.UtcNow;
            waitingPositionOrder.Language = "zh-TW";
            waitingPositionOrder.Note = waitingPositionOrder.Note ?? string.Empty;
            waitingPositionOrder.CustomerNote = waitingPositionOrder.CustomerNote ?? string.Empty;

            if (!(waitingPositionOrder.Phone.Length == 10 || waitingPositionOrder.Phone.Length == 13))
                return Content(JsonConvert.SerializeObject(new { IsSuccess = false, Msg = "『電話』<br>資訊錯誤" }), "application/json");

            if (waitingPositionOrder.Phone.StartsWith("09"))
                waitingPositionOrder.Phone = $"+886{waitingPositionOrder.Phone.Substring(1)}";

            JObject jData = JObject.FromObject(waitingPositionOrder);
            Common.SetJobjectToCamelCase(ref jData);

            ApiResult apiResult = InlineApps.PostInlineappsApi($"/waitings/{companyId}/{branchId}", jData);
            if (apiResult.code != 200)
                return Content(JsonConvert.SerializeObject(new { IsSuccess = false, Msg = "請聯絡客服單位" }), "application/json");

            JObject result = JObject.Parse(apiResult.data);

            // TODO:
            //  取得會員ID
            int memberId = 0;

            List<MemberWaitingLog> logs = new List<MemberWaitingLog>()
            {
                new MemberWaitingLog()
                {
                    Json = jData.ToString(),
                    Status = "N",
                    Creator = 0,
                    CreateDate = DateTime.Now,
                    CreateFrom = "FEDS-SYS"
                }
            };

            _db.MemberWaitings.Add(new MemberWaiting()
            {
                id = result.GetValue("reservationId").ToString(),
                memberId = memberId,
                CompanyId = companyId,
                BranchId = branchId,
                GroupSize = waitingPositionOrder.GroupSize,
                NumberOfKid = waitingPositionOrder.NumberOfKidChairs,
                ContactName = waitingPositionOrder.CustomerName,
                ContactPhone = waitingPositionOrder.Phone,
                ContactGender = Convert.ToByte(waitingPositionOrder.Gender),
                Datetime = DateTime.Now,
                Note = waitingPositionOrder.CustomerNote,
                Creator = 0,
                CreateDate = DateTime.Now,
                CreateFrom = "FEDS-SYS",
                MemberWaitingLogs = logs
            });

            try
            {
                _db.SaveChanges();
            }
            catch (Exception)
            { }
            return Content(JsonConvert.SerializeObject(new { IsSuccess = true }), "application/json");
        }
    }
}