using Newtonsoft.Json.Linq;
using Reservation.ViewModels.Restaurants;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace Reservation.Models
{
    /// <summary>
    /// Layout 控制項
    /// </summary>
    public class LayResult : Restaurant
    {
        /// <summary>
        /// 顯示推薦餐廳按鈕
        /// </summary>
        public bool ShowAlternateBranches { get; set; }

        /// <summary>
        /// 顯示菜單按鈕
        /// </summary>
        public bool ShowMenu { get; set; }

        /// <summary>
        /// 初始化 Layout 控制項
        /// </summary>
        public LayResult()
        {
            ShowAlternateBranches = false;
            ShowMenu = false;
        }

        /// <summary>
        /// 以 Restaurant 物件初始化 Layout 控制項
        /// </summary>
        /// <param name="restaurant"></param>
        public LayResult(Restaurant restaurant)
        {
            id = restaurant.id;
            GroupId = restaurant.GroupId;
            CompanyId = restaurant.CompanyId;
            Name = restaurant.Company.Name;
            Address = restaurant.Address;
            PhoneNumber = restaurant.PhoneNumber;
            OpeningTimes = restaurant.OpeningTimes;
            Company = restaurant.Company;
        }

        /// <summary>
        /// 取得Layout餐廳資訊
        /// </summary>
        /// <param name="apiResult"></param>
        /// <returns></returns>
        public static LayResult GetLayInfo(ApiResult api)
        {
            // XXX:
            //  傳入參數建議使用強行別會比較好，這裡用 JObject 去解析可能會導致後來維護者不清楚傳入參數中應該要包含哪些數值
            LayResult result = new LayResult();

            // 解析API資料
            JObject data = JObject.Parse(api.data);
            result.Name = (string)data.GetValue("name");
            result.Address = (string)data.GetValue("address");
            result.PhoneNumber = (string)data.GetValue("phoneNumber");

            //餐廳類別
            result.Company = data.GetValue("company").ToObject<Company>();

            //餐廳營業時間
            result.OpeningTimes = new List<OpeningTime>();
            JArray oTime = data.GetValue("openingTimes") as JArray;
            foreach (JObject open in oTime)
            {
                OpeningTime fromto = new OpeningTime
                {
                    From = (string)open.GetValue("from"),
                    To = (string)open.GetValue("to")
                };
                result.OpeningTimes.Add(fromto);
            }

            return result;
        }
    }

    /// <summary>
    /// API 回傳共用
    /// </summary>
    public class ApiResult
    {
        /// <summary>
        /// 狀態碼
        /// </summary>
        public int code { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string msg { get; set; }

        /// <summary>
        /// 回傳資料
        /// </summary>
        public string data { get; set; }
    }

    /// <summary>
    /// 共用類別
    /// </summary>
    public class Common
    {
        #region API
        /// <summary>
        /// GET third API
        /// </summary>
        /// <param name="url"></param>
        /// <param name="requestHeaders"></param>
        /// <returns></returns>
        public static ApiResult GetApi(string url, Dictionary<string, string> requestHeaders = null)
        {
            ApiResult result = new ApiResult();
            requestHeaders = requestHeaders ?? new Dictionary<string, string>();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            foreach (KeyValuePair<string, string> header in requestHeaders)
                request.Headers.Add(header.Key, header.Value);

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    result.code = (int)response.StatusCode;
                    result.msg = response.StatusDescription;
                    result.data = reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name == "WebException")
                {
                    WebException webException = (WebException)ex;
                    try
                    {
                        using (HttpWebResponse response = (HttpWebResponse)webException.Response)
                        using (Stream stream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            result.code = (int)response.StatusCode;
                            result.msg = response.StatusDescription;
                            result.data = reader.ReadToEnd();
                        }
                    }
                    catch (Exception e)
                    {
                        string exName = ex.GetType().Name;
                        bool hasResponse = webException.Response != null;
                        Log.Func_Log.InsertLog(Log.LogType.Error, $"GET API({url}) failed;\r\n  exName = {exName}, hasResponse = {hasResponse} \r\n  {e.Message}");
                    }
                }

                if (result.code == 0)
                {
                    result.code = -1;
                    result.msg = ex.Message;
                }
            }
            return result;
        }

        /// <summary>
        /// POST third API
        /// </summary>
        /// <param name="url"></param>
        /// <param name="requestBody"></param>
        /// <param name="requestHeaders"></param>
        /// <returns></returns>
        public static ApiResult PostApi(string url, object requestBody, Dictionary<string, string> requestHeaders = null)
        {
            ApiResult result = new ApiResult();
            requestHeaders = requestHeaders ?? new Dictionary<string, string>();

            HttpResponseMessage _response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = "application/json";
            request.Method = "POST";
            foreach (KeyValuePair<string, string> header in requestHeaders)
                request.Headers.Add(header.Key, header.Value);

            // Set reqeustBody
            string jsonText = string.Empty;
            Type requestBodyType = requestBody.GetType();
            if (requestBodyType.FullName.Contains("JObject") || requestBodyType.FullName.Contains("JToken") ||
                requestBodyType.FullName.Contains("String"))
            {
                jsonText = requestBody.ToString();
            }
            else
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                jsonText = serializer.Serialize(requestBody);
            }
            byte[] payload = Encoding.UTF8.GetBytes(jsonText);
            request.ContentLength = payload.Length;

            try
            {
                using (Stream writer = request.GetRequestStream())
                    writer.Write(payload, 0, payload.Length);

                // POST request
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    result.code = (int)response.StatusCode;
                    result.msg = response.StatusDescription;
                    result.data = reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name == "WebException")
                {
                    WebException webException = (WebException)ex;
                    try
                    {
                        using (HttpWebResponse response = (HttpWebResponse)webException.Response)
                        using (Stream stream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            result.code = (int)response.StatusCode;
                            result.msg = response.StatusDescription;
                            result.data = reader.ReadToEnd();
                        }
                    }
                    catch (Exception e)
                    {
                        string exName = ex.GetType().Name;
                        bool hasResponse = webException.Response != null;
                        Log.Func_Log.InsertLog(Log.LogType.Error, $"POST API({url}) failed;\r\n  RequestBody:{jsonText}\r\n  exName = {exName}, hasResponse = {hasResponse} \r\n  {e.Message}");
                    }
                }

                if (result.code == 0)
                {
                    result.code = -1;
                    result.msg = ex.Message;
                }
            }
            return result;
        }
        #endregion

        /// <summary>
        /// 取得版本號
        /// </summary>
        /// <returns></returns>
        public static string GetClineVersion()
        {
            return ConfigurationManager.AppSettings["ClineVersion"];
        }

        /// <summary>
        /// 取得登入網址
        /// </summary>
        /// <returns></returns>
        public static string GetLoginUrl()
        {
            return $"https://member.feds.com.tw/login/?returnUrl={HttpUtility.UrlEncode(HttpContext.Current.Request.Url.AbsoluteUri)}";
        }

        /// <summary>
        /// 修改 JObject 節點名稱為駝峰式命名
        /// (使用遞迴寫法 請小心使用)
        /// </summary>
        /// <param name="jData"></param>
        /// <returns></returns>
        public static void SetJobjectToCamelCase(ref JObject jData)
        {
            JObject result = new JObject();
            foreach (JProperty item in jData.Properties())
            {
                string name = string.Empty;
                foreach (string par in item.Name.Split('-'))
                    name += par.Substring(0, 1).ToUpper() + par.Substring(1);

                name = item.Name.Substring(0, 1).ToLower() + item.Name.Substring(1);
                JToken property = SetTokenToCamelCase(item.Value);

                result.Add(name, property);
            }

            jData = result;
        }

        /// <summary>
        /// 判斷 JToken 類型以決定節點名稱處理方式
        /// </summary>
        /// <param name="jToken"></param>
        /// <returns></returns>
        private static JToken SetTokenToCamelCase(JToken jToken)
        {
            if (jToken.Type == JTokenType.Object)
            {
                JObject propertyObj = (JObject)jToken;
                SetJobjectToCamelCase(ref propertyObj);
                jToken = propertyObj;
            }
            else if (jToken.Type == JTokenType.Array && jToken.HasValues)
            {
                JArray properties = (JArray)jToken;
                JArray returnArray = new JArray();
                foreach (JToken jProp in properties)
                    returnArray.Add(SetTokenToCamelCase(jProp));

                jToken = returnArray;
            }

            return jToken;
        }
    }
}