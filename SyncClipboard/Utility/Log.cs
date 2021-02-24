﻿using System;
using System.IO;

namespace SyncClipboard.Utility
{
    static class Log
    {
        private static String logFolder = "Log";

        public static void Write(string str)
        {
            var dayTime = DateTime.Now;
            string logStr = string.Format("[{0}] {1}", dayTime, str);

#if DEBUG
            WriteToConsole(logStr);
#else
            string logFile = dayTime.ToString("yyyyMMdd");
            WriteToFile(logStr, logFile);
#endif
        }

        private static void WriteToFile(string logStr, string logFile)
        {
            //判断文件夹是否存在
            if (!Directory.Exists(logFolder))
            {
                Directory.CreateDirectory(logFolder);
            }

            try
            {
                using (StreamWriter file = new StreamWriter($@"{logFolder}\{logFile}.txt", true, System.Text.Encoding.UTF8))
                {
                    file.WriteLine(logStr);
                    file.Close();
                }
            }
            catch (Exception)
            {
                //throw;
            }
        }

        private static void WriteToConsole(string logStr)
        {
            Console.WriteLine(logStr);
        }
    }
}
