using System;
using System.Collections.Generic;
using System.Web.Configuration;

namespace Reservation.Models
{
    public class InlineApps
    {
        /// <summary>
        /// Group餐廳查詢類型
        /// </summary>
        public enum GroupType
        {
            all,
            booking,
            waiting
        }

        /// <summary>
        /// Get Restaurant branch in Group
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="companyId"></param>
        /// <param name="branchId"></param>
        /// <param name="type"></param>
        /// <param name="size"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static ApiResult GetInlineappsBranch(string groupId, string companyId, string branchId, GroupType type, int size = 1, DateTime? date = null)
        {
            if (date == null)
                date = DateTime.Now.Date;

            return GetInlineappsApi($"/v2/groups/{groupId}/companies/{companyId}/branches/{branchId}", $"type={type.ToString()}&size={size}&date={date.Value.ToString("yyyy-MM-dd")}");
        }

        #region Common
        /// <summary>
        /// GET Inline API
        /// </summary>
        /// <param name="location"></param>
        /// <param name="queryStr"></param>
        /// <returns></returns>
        public static ApiResult GetInlineappsApi(string location, string queryStr)
        {
            Dictionary<string, string> Headers = new Dictionary<string, string>()
            {
                { "X-API-Key", WebConfigurationManager.AppSettings["InlineAppKey"] }
            };
            location = location.TrimStart('/');
            location = location.TrimEnd('?');
            queryStr = queryStr.TrimStart('?');
            string Domain = WebConfigurationManager.AppSettings["InlineDomain"];
            string url = $"{Domain}{location}?{queryStr}";

            return Common.GetApi(url, Headers);
        }

        /// <summary>
        /// POST Inline reservations API
        /// </summary>
        /// <param name="location"></param>
        /// <param name=" reqeustBody"></param>
        /// <returns></returns>
        public static ApiResult PostInlineappsApi(string location, object reqeustBody)
        {
            Dictionary<string, string> Headers = new Dictionary<string, string>()
            {
                { "X-API-Key", WebConfigurationManager.AppSettings["InlineAppKey"] }
            };
            location = location.StartsWith("/") ? location.TrimStart('/') : location;
            location = location.EndsWith("?") ? location.TrimEnd('?') : location;
            //queryStr = queryStr.StartsWith("?") ? queryStr.TrimStart('?') : queryStr;
            string Domain = WebConfigurationManager.AppSettings["InlineDomain"];
            string url = $"{Domain}{location}";

            return Common.PostApi(url, reqeustBody, Headers);
        }
        #endregion
    }
}