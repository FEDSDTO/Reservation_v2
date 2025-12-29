using System;
using System.Configuration;

namespace Reservation.Models.Log
{
    /// <summary>
    /// Log 功能類別
    /// </summary>
    public class Func_Log
    {
        readonly static string logPath = ConfigurationManager.AppSettings["LogFilePath"];
        static ServerFile sf = new ServerFile();
        static bool setLod = Convert.ToBoolean(ConfigurationManager.AppSettings["SetLog"]);

        /// <summary>
        /// 寫入 Log 檔案
        /// </summary>
        /// <param name="type"></param>
        /// <param name="Message"></param>
        public static void InsertLog(LogType type, string Message)
        {            
            if (setLod)
            {
                string logName = $"Log_{DateTime.Now.ToString("yyyyMMdd")}.txt";
                string logContent = $"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")} [{type.ToString()}] {Message} ;";

                sf.UpdateFile(ConfigurationManager.AppSettings["LogFilePath"], logName, logContent);
            }
        }
    }

    /// <summary>
    /// Log 類型
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// 紀錄
        /// </summary>
        Info = 0,

        /// <summary>
        /// 警告
        /// </summary>
        Warn = 1,

        /// <summary>
        /// 錯誤
        /// </summary>
        Error = 2
    }
}