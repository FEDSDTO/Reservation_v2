using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Reservation.Models.EFRestaurantModels;
using Reservation.Models.ViewModels;
using Reservation.Service;
using Microsoft.EntityFrameworkCore; 
using Newtonsoft.Json.Linq;

namespace Reservation.Controllers
{
    public class WaitingPositionController : Controller
    {
        private readonly RestaurantContext _restaurantContext;
        private readonly RestaurantService _restaurantService;
        private readonly InlineAppsService _inlineAppsService;
        private readonly Func_Log _Log;

        public WaitingPositionController(
            RestaurantContext restaurantContext,
            RestaurantService restaurantService,
            InlineAppsService inlineAppsService,
            Func_Log fileLogService)
        {
            _restaurantContext = restaurantContext;
            _restaurantService = restaurantService;
            _inlineAppsService = inlineAppsService;
            _Log = fileLogService;
        }

        public async Task<IActionResult> Index(string branchId, string restaurantId)
        {
           if(string.IsNullOrEmpty(restaurantId) || string.IsNullOrEmpty(branchId))
           {
              return NotFound();
           }
            var mallGroups = await _restaurantService.GetMallsAsync();
            var branchDict = new Dictionary<string, int>();
            var branches = new List<BranchModel>();
            
            for (int i = 0; i < mallGroups.Count; i++)
            {
                var branch = new BranchModel 
                { 
                    Id = i + 1,
                    Name = mallGroups[i].Name 
                };
                branches.Add(branch);
                branchDict[mallGroups[i].GroupId] = branch.Id;
            }

            var restaurantCards = await _restaurantService.GetRestaurantsAsync(branchId);
            var restaurantCard = restaurantCards.FirstOrDefault(r=>r.id == restaurantId);

            if(restaurantCard == null)
            {
                return NotFound();
            }
            var currentQueueCount = 0;
            int estimatedWaitMinutes = 0;
            string waitingStatus = "open";
            int maxWaitingGroupSize= 8;
            try
            {
                var apiResult = await _inlineAppsService.GetBranchAsync(
                    branchId,
                    restaurantCard.CompanyId,
                    restaurantId,
                    "waiting",
                    size: 1,
                    date: DateTime.UtcNow.Date);

                if(apiResult.Code == 200)
                {
                    try
                    {
                        var data = JObject.Parse(apiResult.Data);
                        var waitingInfo = data.GetValue("waitingInfo");
                        if(waitingInfo != null)
                        {
                            currentQueueCount = waitingInfo.Value<int?>("waitingCount") ?? 0;
                            estimatedWaitMinutes = waitingInfo.Value<int?>("estimatedWaitingMinutes") ?? 0;
                            waitingStatus = waitingInfo.Value<string>("status")?.ToLower() ?? "open";
                        }
                        var maxGroupSize = data.GetValue("maxWaitingGroupSize");
                        if(maxGroupSize != null)
                        {
                            maxWaitingGroupSize = maxGroupSize.Value<int>();
                        }
                    }
                    catch(Exception ex)
                    {
                        _Log?.SystemErrorLog_Txt($"解析候位資訊失敗: {ex.Message}");
                    }
                }
                else
                {
                    _Log?.SystemErrorLog_Txt($"取得候位資訊失敗 - Code: {apiResult.Code}, Msg: {apiResult.Msg}");
                }
            }
            catch(Exception ex)
            {
                _Log?.SystemErrorLog_Txt($"取得候位資訊異常: {ex.Message}");
            }

            if(waitingStatus != "open")
            {
                TempData["ErrorMsg"] = "餐廳目前無提供候位服務";
               return RedirectToAction("Index", "Restaurant", new { groupId = branchId });
            }
            var restaurant = new Reservation.Models.ViewModels.RestaurantInfoModel
            {
                Id = branches.FirstOrDefault(b => branchDict.ContainsKey(branchId) && branchDict[branchId] == b.Id)?.Id ?? 1,
                Name = restaurantCard.Name,
                ImageUrl = restaurantCard.Images?.FirstOrDefault() ?? "~/Image/1.jpg",
                Location = restaurantCard.Address,
                Phone = restaurantCard.PhoneNumber,
                OpeningHours = "10:00-22:00",
                BranchId = branchDict.ContainsKey(branchId) ? branchDict[branchId] : 1,
                CategoryId = 1,
                IsPopular = false,
                IsNew = false
            };

            var selectedBranch = branches.FirstOrDefault(b => branchDict.ContainsKey(branchId) && branchDict[branchId] == b.Id) 
                ?? branches.FirstOrDefault();
            
            var viewModel = new WaitingPositionModel
            {
                Restaurant = restaurant,
                Branch = selectedBranch ?? new BranchModel(),
                AdultCount = 2,
                ChildCount = 0,
                CurrentQueueCount = currentQueueCount,
                QueueNumber = 0,
                AheadCount = 0,
                EstimatedWaitMinutes = estimatedWaitMinutes,
                MaxWaitingGroupSize = maxWaitingGroupSize
            };
            ViewBag.GroupId = branchId;
            ViewBag.CompanyId = restaurantCard.CompanyId;
            ViewBag.BranchId = restaurantId;
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Index(WaitingPositionModel model)
        {
           try
           {
            //驗證資料
            var errors = new List<string>();

            if(string.IsNullOrWhiteSpace(model.CustomerName))
            {
                errors.Add("請輸入姓名");
            }

            if(string.IsNullOrWhiteSpace(model.CustomerPhone))
            {
                errors.Add("請輸入電話號碼");
            }

            if(errors.Any())
            {
                if(Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new {success = false,errors =errors});
                }
                return View(model);
            }
            //從Form取的資料
            var groupId = Request.Form["GroupId"].ToString();
            var companyId = Request.Form["CompanyId"].ToString();
            var branchId = Request.Form["BranchId"].ToString();

            if(string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(branchId))
            {
                return Json(new { success = false, errors = new[] { "查無此餐廳" } });
            }

            int memberId = 0;
            int gender = 2;
            var salutation = Request.Form["Salutation"].ToString();
            if(salutation == "先生")
            {
                gender = 0;
            }
            else if(salutation == "小姐")
            {
                gender = 1;
            }

            string formattedPhone = model.CustomerPhone;
            if(!(formattedPhone.Length == 10 || formattedPhone.Length == 13))
            {
                return Json(new{success = false,errors = new[] { "請輸入正確的電話號碼" } });
            }
            
            if(formattedPhone.StartsWith("09"))
            {
                formattedPhone = $"+886{formattedPhone.Substring(1)}";
            }

            var waitingPositionOrder =  new WaitingPositionOrder
            {
              CustomerName = model.CustomerName,
              Phone = formattedPhone,
              Gender = gender,
              GroupSize = model.AdultCount,
              NumberOfKidChairs=model.ChildCount,
              CustomerNote = Request.Form["CustomerNote"].ToString()??string.Empty,
              Note=string.Empty,
              Language = "zh-TW",
              Datetime = DateTime.UtcNow,
              CreatedFrom = "FEDSWEB"
            };
            _Log?.SystemLog_Txt($"=== 提交候位 ===");
            _Log?.SystemLog_Txt($"GroupId: {groupId}, CompanyId: {companyId}, BranchId: {branchId}");
            _Log?.SystemLog_Txt($"候位資料: {System.Text.Json.JsonSerializer.Serialize(waitingPositionOrder)}");
            

            var apiResult = await _inlineAppsService.PostWaitingAsync(companyId,branchId,waitingPositionOrder);
            if(apiResult.Code !=200)
            {
                _Log?.SystemErrorLog_Txt($"候位失敗 - code:{apiResult.Code}, message:{apiResult.Msg},Data:{apiResult.Data}");
                string errorMessage = "候位失敗，請稍後再試";
                string errorType ="general";
                try
                {
                    var errorData = Newtonsoft.Json.Linq.JObject.Parse(apiResult.Data);
                    var message = errorData.GetValue("message")?.ToString();
                    var reason = errorData.GetValue("reason")?.ToString();

                    if(message == "Web waiting is disabled" || reason?.Contains("Web Waiting is not available") == true)
                    {
                        errorMessage = "目前無法進行線上候位，請稍後再試";
                        errorType = "waiting_disabled";
                    }
                    else if(!string.IsNullOrEmpty(message))
                    {
                        errorMessage = message;
                    }
                }
                catch(Exception ex)
                {
                    _Log?.SystemErrorLog_Txt($"候位失敗 - 解析錯誤: {ex.Message}");
                }
                return Json(new {
                    success = false,
                    errors = new[] { errorMessage },
                    errorType = errorType
                });
            };
            string reservationId = string.Empty;
            try
            {
                var result = Newtonsoft.Json.Linq.JObject.Parse(apiResult.Data);
                var reservationIdToken = result.GetValue("reservationId");
                reservationId = reservationIdToken?.ToString() ?? string.Empty;
            }
            catch(Exception ex)
            {
               _Log?.SystemErrorLog_Txt($"候位失敗 - 解析ReservationId失敗: {ex.Message}");
            }

            try
            {
               string waitingId = reservationId;
               if(string.IsNullOrEmpty(waitingId))
               {
                  waitingId = $"WAIT_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                   _Log?.SystemLog_Txt($"API 未返回 reservationId，產生臨時 ID: {waitingId}");
               }

               var memberWaiting = new MemberWaiting
               {
                 Id = waitingId,
                 MemberId = memberId,
                 CompanyId = companyId,
                 BranchId = branchId,
                 GroupSize = model.AdultCount,
                 NumberOfKid=model.ChildCount,
                 ContactName = model.CustomerName,
                 ContactPhone = formattedPhone,
                 ContactGender = (byte)gender,
                 Datetime = DateTime.Now,
                 Note = waitingPositionOrder.CustomerNote ?? string.Empty,
                 Remark = !string.IsNullOrEmpty(reservationId)
                 ? $"API 返回 reservationId: {reservationId}"
                 : string.Empty,
                 Creator = 0,
                 CreateDate = DateTime.Now,
                 CreateFrom = "FEDS-SYS"
               };

               _restaurantContext.MemberWaitings.Add(memberWaiting);
               await _restaurantContext.SaveChangesAsync();
               
                var memberWaitingLog = new MemberWaitingLog
                {
                    ReserveId = memberWaiting.Id,
                    Status = "N",
                    Json = System.Text.Json.JsonSerializer.Serialize(waitingPositionOrder),
                    Creator = 0,
                    CreateDate = DateTime.Now,
                    CreateFrom = "FEDS-SYS"
                };
                memberWaiting.MemberWaitingLogs.Add(memberWaitingLog);
                await _restaurantContext.SaveChangesAsync();
                _Log?.SystemLog_Txt($"候位資料已儲存到資料庫 - WaitingId: {memberWaiting.Id}, API ReservationId: {reservationId}");
            }
            catch(Exception ex)
            {
                _Log?.SystemErrorLog_Txt($"儲存候位資料到資料庫失敗: {ex.Message}\r\nStackTrace: {ex.StackTrace}");
            }

            if(Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new {
                    success = true,
                    message = "候位成功",
                    reservationId = reservationId                 
                });
            }

            model.QueueNumber = 0;
            model.AheadCount = 0;
            model.EstimatedWaitMinutes = 0;
            model.CurrentQueueCount = 0;

            TempData["QueueSuccess"] = "候位成功";
            return View(model);

           }catch(Exception ex)
           {
            _Log?.SystemErrorLog_Txt($"候位失敗: {ex.Message}");
            return Json(new { success = false, message = "候位失敗，請稍後再試" });
           }
        }

        [HttpGet]
        public async Task<IActionResult> GetQueueStatus(string groupId, string companyId, string branchId)
        {
            var currentQueueCount = 0;
            try
            {
                var apiResult = await _inlineAppsService.GetBranchAsync(
                    groupId, 
                    companyId, 
                    branchId, 
                    "waiting",
                    size: 1,
                    date: DateTime.UtcNow.Date);

                if(apiResult.Code == 200)
                {
                    try
                    {
                        var data = JObject.Parse(apiResult.Data);
                        var waitingInfo = data.GetValue("waitingInfo");
                        if(waitingInfo != null)
                        {
                            currentQueueCount = waitingInfo.Value<int?>("waitingCount") ?? 0;
                        }
                    }
                    catch(Exception ex)
                    {
                        _Log?.SystemErrorLog_Txt($"解析候位資訊失敗: {ex.Message}");
                    }
                }
            }
            catch(Exception ex)
            {
                _Log?.SystemErrorLog_Txt($"取得候位資訊失敗: {ex.Message}");
            }
            return Json(new { currentQueueCount = currentQueueCount });
        }
    }
}
