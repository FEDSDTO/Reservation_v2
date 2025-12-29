namespace Reservation.Models
{
    public class ApiResult
    {
        /// <summary>
        /// HTTP 狀態碼（200 表示成功，其他表示錯誤）
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 回應訊息（通常是狀態描述或錯誤訊息）
        /// </summary>
        public string Msg { get; set; } = string.Empty;

        /// <summary>
        /// 回應資料（JSON 字串格式）
        /// </summary>
        public string Data { get; set; } = string.Empty;
    }
}
