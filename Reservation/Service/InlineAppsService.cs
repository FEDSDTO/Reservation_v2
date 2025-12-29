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
       
        public InlineAppsService(IHttpClientFactory httpClientFactory,IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
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

               string url = $"{domain}/{location}?{queryStr}";
               var client = _httpClientFactory.CreateClient();
               client.DefaultRequestHeaders.Add("X-API-KEY",apiKey);
               var response = await client.GetAsync(url);

               apiResult.Code=(int)response.StatusCode;
               apiResult.Msg=response.ReasonPhrase;
               apiResult.Data=await response.Content.ReadAsStringAsync();

            }
            catch(Exception ex)
            {
               apiResult.Code=-1;
               apiResult.Msg=ex.Message;
               apiResult.Data=string.Empty;
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
                string url = $"{domain}{location}";

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
