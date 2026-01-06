using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Reservation.Models.EFMemeberModels;

namespace Reservation.Middleware
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenValidationMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private const string TokenCookieName = "MemberToken";
        // 開發環境預設 Token（後備值，如果配置文件中沒有設定）
        private const string DefaultDevTokenFallback = "EA4A52C4-1489-441A-BA39-8E5C07D2E041";

        private static readonly string[] ExcludedPaths = {
            "/css/",
            "/js/",
            "/lib/",
            "/Image/",
            "/favicon.ico",
            "/Home/Error"
        };

        public TokenValidationMiddleware(
            RequestDelegate next,
            ILogger<TokenValidationMiddleware> logger,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
            _configuration = configuration;
        }
        
        /// <summary>
        /// 取得開發環境預設 Token（從配置檔案讀取，如果沒有則使用後備值）
        /// </summary>
        private string GetDefaultDevToken()
        {
            return _configuration["DefaultDevToken"] ?? DefaultDevTokenFallback;
        }

        /// <summary>
        /// 取得正式環境預設 Token（從配置檔案讀取）
        /// </summary>
        private string? GetDefaultProdToken()
        {
            return _configuration["DefaultProdToken"];
        }

        public async Task InvokeAsync(HttpContext context, MemberContext memberContext)
        {
            // 检查是否在排除列表中
            var path = context.Request.Path.Value ?? "";
            if (ExcludedPaths.Any(excluded => path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            // 特殊处理：Restaurant/Index 允许从 URL 参数获取 Token（首次访问）
            var isRestaurantIndex = path.Equals("/Restaurant/Index", StringComparison.OrdinalIgnoreCase) ||
                                   path.Equals("/Restaurant", StringComparison.OrdinalIgnoreCase) ||
                                   path.Equals("/", StringComparison.OrdinalIgnoreCase);

            // 尝试从 Cookie 读取 Token
            var tokenFromCookie = context.Request.Cookies[TokenCookieName];
            
            // 如果是 Restaurant/Index 且 Cookie 中没有 Token，尝试从 URL 参数获取
            if (isRestaurantIndex && string.IsNullOrEmpty(tokenFromCookie))
            {
                var tokenFromQuery = context.Request.Query["Token"].FirstOrDefault();
                if (!string.IsNullOrEmpty(tokenFromQuery))
                {
                    // 將 URL 參數中的 Token 設置到 Cookie 中
                    context.Response.Cookies.Append(TokenCookieName, tokenFromQuery, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = context.Request.IsHttps,
                        SameSite = SameSiteMode.Lax
                    });
                    tokenFromCookie = tokenFromQuery;
                }
            }

            // 開發環境：如果沒有 Token，使用預設 Token
            if (string.IsNullOrEmpty(tokenFromCookie) && _environment.IsDevelopment())
            {
                var defaultToken = GetDefaultDevToken();
                _logger.LogInformation($"開發環境：使用預設 Token: {defaultToken}");
                tokenFromCookie = defaultToken;
                // 將預設 Token 設置到 Cookie 中
                context.Response.Cookies.Append(TokenCookieName, defaultToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Lax
                });
            }

            // 正式環境：如果沒有 Token 且配置了預設 Token，使用預設 Token
            if (string.IsNullOrEmpty(tokenFromCookie) && !_environment.IsDevelopment())
            {
                var defaultProdToken = GetDefaultProdToken();
                if (!string.IsNullOrEmpty(defaultProdToken))
                {
                    _logger.LogInformation($"正式環境：使用預設 Token: {defaultProdToken}");
                    tokenFromCookie = defaultProdToken;
                    // 將預設 Token 設置到 Cookie 中
                    context.Response.Cookies.Append(TokenCookieName, defaultProdToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = context.Request.IsHttps,
                        SameSite = SameSiteMode.Lax
                    });
                }
            }

            // 如果沒有 Token（既沒有 Cookie 也沒有 URL 參數，且沒有預設 Token）
            if (string.IsNullOrEmpty(tokenFromCookie))
            {
                await RedirectToLoginAsync(context);
                return;
            }

            // 验证 Cookie 中的 Token
            if (!Guid.TryParse(tokenFromCookie, out Guid tokenGuid))
            {
                _logger.LogWarning($"Cookie 中的 Token 格式無效: {tokenFromCookie}");
                await RedirectToLoginAsync(context);
                return;
            }

            try
            {
                // 验证 Token：必须同时满足 Token 存在 且 EntityStatus == 1
                var memberToken = await memberContext.MemberTokens
                    .FirstOrDefaultAsync(mt => mt.Token == tokenGuid && mt.EntityStatus == 1);

                // 開發環境：如果 Token 驗證失敗，跳過驗證使用模擬資料
                if (_environment.IsDevelopment())
                {
                    if (memberToken == null)
                    {
                        _logger.LogWarning($"開發環境：Token 不存在於資料庫或 EntityStatus 不等於 1，跳過驗證. Token: {tokenGuid}");
                        // 設置模擬的會員資訊
                        context.Items["MemberId"] = 0;
                        context.Items["Token"] = tokenGuid;
                        await _next(context);
                        return;
                    }

                    // 開發環境：即使 Token 過期也允許通過
                    if (memberToken.ExpireDate.HasValue && memberToken.ExpireDate.Value < DateTime.Now)
                    {
                        _logger.LogWarning($"開發環境：Token 已過期，但允許通過. Token: {tokenGuid}, 過期時間: {memberToken.ExpireDate}");
                        context.Items["MemberId"] = memberToken.MemberId;
                        context.Items["Token"] = tokenGuid;
                        context.Items["MemberToken"] = memberToken;
                        await _next(context);
                        return;
                    }
                }
                else
                {
                    // 正式環境：嚴格驗證
                    // 验证条件：Token 不存在或 EntityStatus != 1
                    if (memberToken == null)
                    {
                        _logger.LogWarning($"Token 驗證失敗: Token 不存在或 EntityStatus 不等於 1. Token: {tokenGuid}");
                        await RedirectToLoginAsync(context);
                        return;
                    }

                    // 验证过期时间
                    if (memberToken.ExpireDate.HasValue && memberToken.ExpireDate.Value < DateTime.Now)
                    {
                        _logger.LogWarning($"Token 已過期: {tokenGuid}, 過期時間: {memberToken.ExpireDate}");
                        await RedirectToLoginAsync(context);
                        return;
                    }
                }

                // 验证通过，将信息存储到 HttpContext.Items
                context.Items["MemberId"] = memberToken.MemberId;
                context.Items["Token"] = tokenGuid;
                context.Items["MemberToken"] = memberToken;

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"驗證 Token 時發生錯誤: {tokenGuid}");
                
                // 開發環境：發生錯誤時不重定向，允許繼續
                if (_environment.IsDevelopment())
                {
                    _logger.LogWarning($"開發環境：Token 驗證發生錯誤，但允許繼續執行");
                    context.Items["MemberId"] = 0;
                    context.Items["Token"] = tokenGuid;
                    await _next(context);
                }
                else
                {
                    await RedirectToLoginAsync(context);
                }
            }
        }

         private async Task RedirectToLoginAsync(HttpContext context)
        {
            // 获取当前完整 URL
            var request = context.Request;
            var currentUrl = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
            
            // URL 编码
            var encodedReturnUrl = Uri.EscapeDataString(currentUrl);
            
            // 重定向到登录页面
            var loginUrl = $"https://member.feds.com.tw/login/?returnUrl={encodedReturnUrl}";
            
            context.Response.Redirect(loginUrl);
            await Task.CompletedTask;
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

