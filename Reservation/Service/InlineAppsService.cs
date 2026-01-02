using System.Net.Http;
using System.Text;
using System.Text.Json;
using Reservation.Models;
namespace Reservation.Service
{
    public class InlineAppsService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly Func_Log _fileLogService;
       
        public InlineAppsService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            Func_Log fileLogService)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _fileLogService = fileLogService;
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

               string domain = _configuration["InlineDomain"]??string.Empty;
               string apiKey = _configuration["InlineApiKey"]??string.Empty;

               // 移除 domain 結尾的斜線，避免雙斜線問題
               domain = domain.TrimEnd('/');
               
               string url = $"{domain}/{location}?{queryStr}";
               
               // 記錄請求資訊
               _fileLogService.ApiResponseLog_Txt($"=== API 請求 ===");
               _fileLogService.ApiResponseLog_Txt($"URL: {url}");
               _fileLogService.ApiResponseLog_Txt($"Location: {location}");
               _fileLogService.ApiResponseLog_Txt($"Query: {queryStr}");
               
               var client = _httpClientFactory.CreateClient();
               client.DefaultRequestHeaders.Add("X-API-KEY",apiKey);
               
               var startTime = DateTime.Now;
               var response = await client.GetAsync(url);
               var elapsedTime = (DateTime.Now - startTime).TotalMilliseconds;

               apiResult.Code=(int)response.StatusCode;
               apiResult.Msg=response.ReasonPhrase ?? string.Empty;
               apiResult.Data=await response.Content.ReadAsStringAsync();

               // 記錄 API 回應
               _fileLogService.ApiResponseLog_Txt($"=== API 回應 ===");
               _fileLogService.ApiResponseLog_Txt($"狀態碼: {apiResult.Code}");
               _fileLogService.ApiResponseLog_Txt($"回應時間: {elapsedTime} ms");
               _fileLogService.ApiResponseLog_Txt($"回應內容: {apiResult.Data}");
               _fileLogService.ApiResponseLog_Txt($"================\r\n");

            }
            catch(Exception ex)
            {
               apiResult.Code=-1;
               apiResult.Msg=ex.Message;
               apiResult.Data=string.Empty;
               
               // 記錄錯誤
               _fileLogService.SystemErrorLog_Txt($"Inline API 請求失敗 - Location: {location}, Error: {ex.Message}");
               _fileLogService.SystemErrorLog_Txt($"StackTrace: {ex.StackTrace}");
            }
            return apiResult;
        }
        /// <summary>
        /// 執行 POST 請求到 Inline Apps API
        /// </summary>
        /// <param name="location"></param>
        /// <param name="requestBody"></param>
        /// <returns></returns>
        public async Task<ApiResult> PostInlineApps(string location,string requestBody)
        {
            var apiResult = new ApiResult();
            try
            {
                location=location.TrimStart('/');
                location=location.TrimEnd('?');

                string domain = _configuration["InlineDomain"]??string.Empty;
                string apiKey = _configuration["InlineApiKey"]??string.Empty;
                
                // 移除 domain 結尾的斜線，避免雙斜線問題
                domain = domain.TrimEnd('/');
                
                string url = $"{domain}/{location}";

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("X-API-KEY",apiKey);

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json,Encoding.UTF8,"application/json");

                var response = await client.PostAsync(url,content);

                apiResult.Code=(int)response.StatusCode;
                apiResult.Msg=response.ReasonPhrase??string.Empty;
                apiResult.Data=await response.Content.ReadAsStringAsync();
                
            }catch(Exception ex)
            {
                apiResult.Code=-1;
                apiResult.Msg=ex.Message;
                apiResult.Data=string.Empty;
            }
            return apiResult;
        }
        
    }
}
