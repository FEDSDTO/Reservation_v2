using System;
using System.IO;

namespace Reservation.Models.Log
{
    /// <summary>
    /// 檔案功能類別
    /// </summary>
    public class ServerFile
    {
        /// <summary>
        /// 更新檔案
        /// </summary>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="content"></param>
        /// <param name="isContinue"></param>
        /// <returns></returns>
        public UpdateFileInfo UpdateFile(string path, string name, string content, bool isContinue = true)
        {
            UpdateFileInfo result = new UpdateFileInfo(path, name);
            string fullPath = path + name;

            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                using (StreamWriter sw = new StreamWriter(fullPath, isContinue))
                {
                    sw.WriteLine(content);
                }
                result.Messige = $"Success.";
            }
            catch (Exception ex)
            {
                result.Messige = ex.Message;
            }

            return result;
        }
    }

    /// <summary>
    /// 檔案更新執行資訊
    /// </summary>
    public class UpdateFileInfo
    {
        /// <summary>
        /// 檔案路徑
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 檔案名稱
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 建立成功
        /// </summary>
        public bool isSuccess { get; set; }

        /// <summary>
        /// 系統訊息
        /// </summary>
        public string Messige { get; set; }

        /// <summary>
        /// 類別建構函式
        /// </summary>
        public UpdateFileInfo() { }

        /// <summary>
        /// 類別建構函式
        /// </summary>
        /// <param name="path"></param>
        /// <param name="name"></param>
        public UpdateFileInfo(string path, string name)
        {
            Path = path;
            Name = name;
        }
    }
}