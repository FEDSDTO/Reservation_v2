using Newtonsoft.Json;
using Reservation.Models.DB;
using System;
using System.Linq;
using System.Web;
using System.Web.Security;

namespace Reservation.Models
{
    /// <summary>
    /// 登入會員
    /// </summary>
    public class Member
    {
        FEDSMBREntities _MBdb = new FEDSMBREntities();

        /// <summary>
        /// 透過Cookie取得遠百會員帳號
        /// </summary>
        /// <returns>帳號</returns>
        public static string GetMemberAccount()
        {
            HttpCookie authCookie = HttpContext.Current.Request.Cookies[FEDS_AuthCookie.cookieName];
            if (authCookie != null)
            {
                try
                {
                    FormsAuthenticationTicket authTicket_FEDSMBR = FormsAuthentication.Decrypt(authCookie.Value);
                    FEDS_AuthCookie _Account = JsonConvert.DeserializeObject<FEDS_AuthCookie>(authTicket_FEDSMBR.UserData);
                    return _Account.Account.ToString();
                }
                catch
                { }
            }

            return string.Empty;
        }

        public int GetMemberIdByAccount(string account)
        {
            DB.Member Member = _MBdb.Member.Where(m => m.Account == account && m.Status == 1).FirstOrDefault();
            if (Member != null)
                return Member.Id;

            return 0;
        }
    }

    /// <summary>
    /// 登入 Cookis 物件
    /// </summary>
    public class FEDS_AuthCookie
    {
        public static readonly string cookieName = "FEDS.ASPXAUTH";

        /// <summary>
        /// 使用者帳號
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// 驗證權杖
        /// </summary>
        public string MemberToken { get; set; }

        /// <summary>
        /// 是否過期
        /// </summary>
        public string TokenExpire { get; set; }

        /// <summary>
        /// 到期時間
        /// </summary>
        public string ValidDate { get; set; }

        /// <summary>
        /// 登入IP
        /// </summary>
        public string IP { get; set; }
    }
}