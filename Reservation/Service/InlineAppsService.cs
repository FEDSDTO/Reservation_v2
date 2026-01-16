using System.Net.Http;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using Reservation.Models;
namespace Reservation.Service
{
    public class InlineAppsService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly Func_Log _Log;
       
        public InlineAppsService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            Func_Log fileLogService)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _Log = fileLogService;
        }

        public enum GroupType
        {
            //所有餐廳
            all,
            //預約餐廳
            booking,
            //等待餐廳
            waiting,
        }

        /// <summary>
        /// 封裝 Framework 的 GetInlineappsBranch - 取得單一餐廳分店的詳細資訊
        /// </summary>
        public async Task<ApiResult> GetBranchAsync(
            string groupId, 
            string companyId, 
            string branchId, 
            string type,  // "all", "booking", "waiting"
            int size = 1, 
            DateTime? date = null)
        {
            date ??= DateTime.UtcNow.Date;
            var queryStr = $"type={type}&size={size}&date={date:yyyy-MM-dd}";
            return await GetInlineApps(
                $"/v2/groups/{groupId}/companies/{companyId}/branches/{branchId}", 
                queryStr);
        }

       public async Task<ApiResult> PostReservationAsync(
            string companyId, 
            string branchId, 
            object requestBody)
        {
            return await PostInlineApps(
                $"/reservations/{companyId}/{branchId}", 
                requestBody);
        }

        /// <summary>
        /// 執行 GET 請求到 Inline Apps API
        /// </summary>
        /// <param name="location"></param>
        /// <param name="queryStr"></param>
        /// <returns></returns>
        public async Task<ApiResult> GetInlineApps(string location,string queryStr)
        {
           var apiResult = new ApiResult();
           try
           {
              location=location.TrimStart('/');
              location=location.TrimEnd('?');
              queryStr=queryStr.TrimStart('?');

              string domain = _configuration["InlineDomain"] ?? string.Empty;
              string apiKey = _configuration["InlineApiKey"] ?? string.Empty;

              domain = domain.TrimEnd('/');

              string url = $"{domain}/{location}?{queryStr}";

            _Log.ApiResponseLog_Txt($"=== API 請求 ===");
            _Log.ApiResponseLog_Txt($"URL: {url}");
            _Log.ApiResponseLog_Txt($"Location: {location}");
            _Log.ApiResponseLog_Txt($"Query: {queryStr}");
            _Log.ApiResponseLog_Txt($"Domain: {domain}");
            _Log.ApiResponseLog_Txt($"API Key: {(string.IsNullOrEmpty(apiKey) ? "未設定" : "已設定")}");
            
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-API-KEY", apiKey);

            var startTime = DateTime.Now;
            var response = await client.GetAsync(url);
            var elapsedTime = (DateTime.Now - startTime).TotalMilliseconds;

            apiResult.Code = (int)response.StatusCode;
            apiResult.Msg = response.ReasonPhrase ?? string.Empty;
            apiResult.Data = await response.Content.ReadAsStringAsync();

            _Log.ApiResponseLog_Txt($"=== API 回應 ===");
            _Log.ApiResponseLog_Txt($"狀態碼: {apiResult.Code}");
            _Log.ApiResponseLog_Txt($"狀態訊息: {apiResult.Msg}");
            _Log.ApiResponseLog_Txt($"回應時間: {elapsedTime}ms");
            _Log.ApiResponseLog_Txt($"回應 Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");
            _Log.ApiResponseLog_Txt($"回應內容長度: {apiResult.Data?.Length ?? 0} 字元");
           
            if(apiResult.Code !=200)
            {
                _Log.SystemErrorLog_Txt($"=== API 錯誤回應 ===");
                _Log.SystemErrorLog_Txt($"URL: {url}");
                _Log.SystemErrorLog_Txt($"狀態碼: {apiResult.Code}");
                _Log.SystemErrorLog_Txt($"狀態訊息: {apiResult.Msg}");
                _Log.SystemErrorLog_Txt($"完整回應內容: {apiResult.Data}");

                if(!string.IsNullOrEmpty(apiResult.Data))
                {
                    try
                    {
                        var errorData = JObject.Parse(apiResult.Data);
                        _Log.SystemErrorLog_Txt($"錯誤 JSON 解析:");
                        foreach(var prop in errorData.Properties())
                        {
                            _Log.SystemErrorLog_Txt($"屬性: {prop.Name}, 值: {prop.Value}");
                        }
                    }catch
                    {
                         _Log.SystemErrorLog_Txt($"回應內容不是有效的 JSON");
                    }
                }
            }
            else 
            {
                var dataPreview = apiResult.Data?.Length > 500 
                ? apiResult.Data.Substring(0, 500) + "..." 
                : apiResult.Data;
                _Log.ApiResponseLog_Txt($"回應內容預覽: {dataPreview}");
            }
           }
           catch(Exception ex)
           {
                apiResult.Code = -1;
                apiResult.Msg = ex.Message;
                apiResult.Data = string.Empty;

                string domain = _configuration["InlineDomain"] ?? string.Empty;
                domain = domain.TrimEnd('/');
                string url = $"{domain}/{location}?{queryStr}";

                _Log.SystemErrorLog_Txt($"=== Inline API 請求異常 ===");
                _Log.SystemErrorLog_Txt($"Location: {location}");
                _Log.SystemErrorLog_Txt($"Query: {queryStr}");
                _Log.SystemErrorLog_Txt($"URL: {url}");
                _Log.SystemErrorLog_Txt($"異常類型: {ex.GetType().Name}");
                _Log.SystemErrorLog_Txt($"異常訊息: {ex.Message}");
                _Log.SystemErrorLog_Txt($"堆疊追蹤: {ex.StackTrace}");
                
                if(ex.InnerException != null)
                {
                    _Log.SystemErrorLog_Txt($"內部異常: {ex.InnerException.Message}");
                    _Log.SystemErrorLog_Txt($"內部異常堆疊: {ex.InnerException.StackTrace}");
                }
           }
           return apiResult;
        }
        /// <summary>
        /// 執行 POST 請求到 Inline Apps API
        /// </summary>
        /// <param name="location"></param>
        /// <param name="requestBody"></param>
        /// <returns></returns>
        public async Task<ApiResult> PostInlineApps(string location, object requestBody)
        {
            var apiResult = new ApiResult();
            try
            {
                location = location.TrimStart('/');
                location = location.TrimEnd('?');

                string domain = _configuration["InlineDomain"] ?? string.Empty;
                string apiKey = _configuration["InlineApiKey"] ?? string.Empty;
                
                // 移除 domain 結尾的斜線，避免雙斜線問題
                domain = domain.TrimEnd('/');
                
                string url = $"{domain}/{location}";

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-API-KEY", apiKey);

                // 處理 requestBody：如果是 string 就直接使用，否則序列化
                string json;
                if (requestBody is string str)
                {
                    json = str;
                }
                else
                {
                    json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                }

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _Log?.ApiResponseLog_Txt($"=== API POST 請求 ===");
                _Log?.ApiResponseLog_Txt($"URL: {url}");
                _Log?.ApiResponseLog_Txt($"Request Body: {json}");

                var startTime = DateTime.Now;
                var response = await client.PostAsync(url, content);
                var elapsedTime = (DateTime.Now - startTime).TotalMilliseconds;

                apiResult.Code = (int)response.StatusCode;
                apiResult.Msg = response.ReasonPhrase ?? string.Empty;
                apiResult.Data = await response.Content.ReadAsStringAsync();

                _Log?.ApiResponseLog_Txt($"回應狀態碼: {apiResult.Code}");
                _Log?.ApiResponseLog_Txt($"回應時間: {elapsedTime}ms");
                _Log?.ApiResponseLog_Txt($"回應內容: {apiResult.Data}");
            }
            catch(Exception ex)
            {
                apiResult.Code = -1;
                apiResult.Msg = ex.Message;
                apiResult.Data = string.Empty;
                
                _Log?.SystemErrorLog_Txt($"Inline API POST 請求失敗 - Location: {location}, Error: {ex.Message}");
                _Log?.SystemErrorLog_Txt($"StackTrace: {ex.StackTrace}");
            }
            return apiResult;
        }
        
        public async Task<ApiResult> PostWaitingAsync(string companyId,string branchId,object requestBody)
        {
            return await PostInlineApps( $"/waitings/{companyId}/{branchId}", requestBody);
        }
       
    }
}
