using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace Reservation.Service
{
    public class Func_Log
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string _logBasePath;

        public Func_Log(IWebHostEnvironment environment)
        {
            _environment = environment;
            _logBasePath = Path.Combine(_environment.ContentRootPath, "Log");
        }

        /// <summary>
        /// 將操作紀錄寫入 SystemLog.txt
        /// </summary>
        /// <param name="message">Log 訊息</param>
        public void SystemLog_Txt(string message)
        {
            string logPath = Path.Combine(
                _logBasePath,
                "SystemLog",
                $"{DateTime.Now:yyyy-MM-dd} - Reservation-SystemLog.txt"
            );
            WriteLog(logPath, message);
        }

        /// <summary>
        /// 將錯誤紀錄寫入 SystemErrorLog.txt
        /// </summary>
        /// <param name="message">錯誤訊息</param>
        public void SystemErrorLog_Txt(string message)
        {
            string logPath = Path.Combine(
                _logBasePath,
                "SystemErrorLog",
                $"{DateTime.Now:yyyy-MM-dd} - Reservation-SystemErrorLog.txt"
            );
            WriteLog(logPath, message);
        }

        /// <summary>
        /// 將效能紀錄寫入 SystemPerformance.txt
        /// </summary>
        /// <param name="message">效能訊息</param>
        public void SystemPerformance_Txt(string message)
        {
            string logPath = Path.Combine(
                _logBasePath,
                "SystemPerformance",
                $"{DateTime.Now:yyyy-MM-dd} - Reservation-SystemPerformance.txt"
            );
            WriteLog(logPath, message);
        }

        /// <summary>
        /// 將 API 回應紀錄寫入 ApiResponseLog.txt
        /// </summary>
        /// <param name="message">API 回應訊息</param>
        public void ApiResponseLog_Txt(string message)
        {
            string logPath = Path.Combine(
                _logBasePath,
                "ApiResponseLog",
                $"{DateTime.Now:yyyy-MM-dd} - Reservation-ApiResponseLog.txt"
            );
            WriteLog(logPath, message);
        }
        
        /// <summary>
        /// 統一的日誌寫入方法
        /// </summary>
        /// <param name="filePath">檔案路徑</param>
        /// <param name="message">訊息內容</param>
        private void WriteLog(string filePath, string message)
        {
           try
           {
               string? directory = Path.GetDirectoryName(filePath);
               if(!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
               {
                Directory.CreateDirectory(directory);
               }
               using (StreamWriter sw = new StreamWriter(filePath, true, System.Text.Encoding.UTF8))
                {
                    sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}\r\n");
                }
           }
           catch
           {
               // 日誌寫入失敗時不拋出異常，避免影響主流程
           }
        }
    }
}
