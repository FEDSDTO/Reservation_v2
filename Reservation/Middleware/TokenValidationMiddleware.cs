using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Reservation.Models.EFMemeberModels;
using Reservation.Service;

namespace Reservation.Middleware
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenValidationMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly Func_Log _log;
        private const string TokenCookieName = "MemberToken";
        // 開發環境預設 Token（後備值，如果配置文件中沒有設定）
        private const string DefaultDevTokenFallback = "EA4A52C4-1489-441A-BA39-8E5C07D2E041";

        private static readonly string[] ExcludedPaths = {
            "/css/",
            "/js/",
            "/lib/",
            "/image/",
            "/img/",
            "/favicon.ico",
            "/Home/Error"
        };

        public TokenValidationMiddleware(RequestDelegate next,ILogger<TokenValidationMiddleware> logger,IWebHostEnvironment environment,IConfiguration configuration,Func_Log log)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
            _configuration = configuration;
            _log = log;
        }
        
        public async Task InvokeAsync(HttpContext context,MemberContext memberContext)
        {
            var path = context.Request.Path.Value ?? "";
            var traceId = context.TraceIdentifier;
            
            if(ExcludedPaths.Any(excluded => path.StartsWith(excluded,StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            var tokenFromQuery = context.Request.Query["Token"].FirstOrDefault();
            var tokenFromCookie = context.Request.Cookies[TokenCookieName];
            string? tokenToUse = null;
            string tokenSource = "無";

            if(!string.IsNullOrEmpty(tokenFromQuery))
            {
                tokenToUse = tokenFromQuery;
                tokenSource = "Query";
                context.Response.Cookies.Append(TokenCookieName,tokenFromQuery,new CookieOptions{
                    HttpOnly = true,
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    Expires = DateTimeOffset.Now.AddDays(1)
                });
            }
            else if(!string.IsNullOrEmpty(tokenFromCookie))
            {
                tokenToUse = tokenFromCookie;
                tokenSource = "Cookie";
            }

            if(string.IsNullOrEmpty(tokenToUse))
            {
                await RedirectToLoginAsync(context, "未提供 Token", tokenSource, tokenToUse);
                return;
            }
           if(!Guid.TryParse(tokenToUse,out Guid tokenGuid))
           {
              await RedirectToLoginAsync(context, "Token 格式錯誤", tokenSource, tokenToUse);
              return;
           }
           if(memberContext ==null)
           {
                _log.SystemErrorLog_Txt($"[Token驗證][{traceId}] MemberContext 為 null，請檢查依賴注入設定 -> 跳轉登入頁面");
                await RedirectToLoginAsync(context, "MemberContext 為 null", tokenSource, tokenToUse);
                return;
           }
           try
           {
                // _log.SystemLog_Txt($"[Token驗證][{traceId}] 開始查詢資料庫 Token前8碼={Mask(tokenGuid.ToString())}");
                var memberToken = await memberContext.MemberTokens.FirstOrDefaultAsync
                (mt => mt.Token == tokenGuid && mt.EntityStatus == 1);

                if(memberToken == null)
                {
                    await RedirectToLoginAsync(context, "資料庫找不到有效 Token", tokenSource, tokenGuid.ToString());
                    return;
                }

                // _log.SystemLog_Txt(
                //     $"[Token驗證][{traceId}] DB命中成功 MemberId={memberToken.MemberId}, EntityStatus={memberToken.EntityStatus}, ExpireDate={(memberToken.ExpireDate.HasValue ? memberToken.ExpireDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "NULL")}"
                // );

                if(memberToken.ExpireDate.HasValue && memberToken.ExpireDate.Value < DateTime.Now)
                {
                    await RedirectToLoginAsync(context, $"Token 已過期 ({memberToken.ExpireDate.Value:yyyy-MM-dd HH:mm:ss})", tokenSource, tokenGuid.ToString());
                    return;
                }

                context.Items["MemberId"] = memberToken.MemberId;
                context.Items["Token"] = tokenGuid;
                context.Items["MemberToken"] = memberToken;
                _log.SystemLog_Txt($"[Token驗證][{traceId}] 驗證成功 會員ID={memberToken.MemberId} -> 繼續處理");
                await _next(context);
           }
           catch(NullReferenceException ex)
           {
                string tokenGuidStr = tokenGuid != Guid.Empty ? tokenGuid.ToString() : "unknown";
                _log.SystemErrorLog_Txt($"[Token驗證][{traceId}] 空引用異常 - Token: {Mask(tokenGuidStr)}, 錯誤: {ex.Message}, 位置: {ex.StackTrace} -> 跳轉登入頁面");
                await RedirectToLoginAsync(context, "發生空引用異常", tokenSource, tokenGuidStr);
                return;
           }
           catch(Exception ex)
           {
                string tokenGuidStr = tokenGuid != Guid.Empty ? tokenGuid.ToString() : "unknown";
                _log.SystemErrorLog_Txt($"[Token驗證][{traceId}] 驗證過程中發生錯誤 tokenGuid={Mask(tokenGuidStr)}, 錯誤訊息={ex.Message}, StackTrace={ex.StackTrace} -> 跳轉登入頁面");
                await RedirectToLoginAsync(context, "驗證流程發生例外", tokenSource, tokenGuidStr);
                return;
           }
        }

        private async Task RedirectToLoginAsync(HttpContext context, string reason, string tokenSource, string? tokenValue)
        {
            var traceId = context.TraceIdentifier;
            var returnUrl = GetReturnUrl();
            var encodedReturnUrl = Uri.EscapeDataString(returnUrl);

            var  loginBaseUrl = GetLoginUrl().TrimEnd('/');
            var loginUrl = $"{loginBaseUrl}/?returnUrl={encodedReturnUrl}";

            _log.SystemLog_Txt(
                $"[Token驗證][{traceId}] 跳轉登入頁 - 原因={reason}，Token來源={tokenSource}，Token={Mask(tokenValue)}，Path={context.Request.Path}，完整跳轉網址={loginUrl}"
            );
            
            context.Response.Redirect(loginUrl);
            await Task.CompletedTask;           
        }
         private string GetLoginUrl()
        {
            var loginUrl = _configuration["MemberLoginUrl"];
            if(!string.IsNullOrEmpty(loginUrl))
            {
                return loginUrl.TrimEnd('/');
            }

            if(_environment.IsDevelopment())
            {
                return "https://v3member.testfeds.com/login/";
            }else
            {
                return "https://member.feds.com.tw/login/";
            }
        }

       private string GetReturnUrl()
        {
            var returnUrl = _configuration["MemberReturnUrl"];
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return returnUrl.TrimEnd('/');
            }

            // fallback：沒有設定檔時才使用
            if (_environment.IsDevelopment())
            {
                return "https://v3www.testfeds.com/Reservation_v2";
            }
            else
            {
                return "https://www.feds.com.tw/Reservation_v2";
            }
        }
         // 避免 log 寫入完整 token（敏感資訊）
        private static string Mask(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "(empty)";
            value = value.Trim();
            if (value.Length <= 8) return value;
            return value.Substring(0, 8) + "****";
        }
    }

    public static class TokenValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseTokenValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TokenValidationMiddleware>();
        } 
    }
}

