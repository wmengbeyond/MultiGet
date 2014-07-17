using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using Imgo.Share;

namespace Imgo.MultiGet.ExTools
{
    /// <summary>
    /// 日志
    /// </summary>
    public class Logger
    {
        private static Log _log = null;

        static Logger()
        {
            _log = new Log();
        }

        /// <summary>
        /// 设置日志参数
        /// </summary>
        /// <param name="logType">日志类型</param>
        /// <param name="logLvl">日志级别</param>
        /// <param name="LogFile">日志文件名</param>
        public static void SetLogParam(LogType logType, LogLvl logLvl, string logFile)
        {
            _log.LogType = logType;
            _log.LogLvl = logLvl;
            _log.LogFile = logFile;            
        }

        /// <summary>
        /// 清除所有日志缓冲区并写入到日志文件
        /// </summary>
        public static void Flus()
        {
            _log.Flush();
        }

        /// <summary>
        /// 输出到控制台
        /// </summary>
        /// <param name="content"></param>
        /// <param name="lvlDesc"></param>
        /// <param name="formatArgs"></param>
        private static void ConsoleOut(string content, string lvlDesc, params object[] formatArgs)
        {
            if (formatArgs != null && formatArgs.Length > 0)
            {
                try
                {
                    content = string.Format(content, formatArgs);
                }
                catch
                {
                }
            }
            Console.WriteLine(string.Format("{0:HH:mm:ss.fff} {1}--{2}", DateTime.Now, lvlDesc, content));
        }

        /// <summary>
        /// 写调试日志
        /// </summary>
        /// <param name="content"></param>
        public static void Debug(string content, params object[] formatArgs)
        {
            ConsoleOut(content, "DBG", formatArgs);

            _log.Debug(content, formatArgs);
        }

        /// <summary>
        /// 写错误日志
        /// </summary>
        /// <param name="content"></param>
        public static void Error(string content, params object[] formatArgs)
        {
            ConsoleOut(content, "ERR", formatArgs);

            _log.Error(content, formatArgs);
        }

        /// <summary>
        /// 写信息日志
        /// </summary>
        /// <param name="content"></param>
        public static void Info(string content, params object[] formatArgs)
        {
            ConsoleOut(content, "INF", formatArgs);

            _log.Info(content, formatArgs);
        }

        /// <summary>
        /// 写详细日志
        /// </summary>
        /// <param name="content"></param>
        public static void Verb(string content, params object[] formatArgs)
        {
            ConsoleOut(content, "VRB", formatArgs);

            _log.Verb(content, formatArgs);
        }

        /// <summary>
        /// 写警告日志
        /// </summary>
        /// <param name="content"></param>
        public static void Warn(string content, params object[] formatArgs)
        {
            ConsoleOut(content, "WRN", formatArgs);

            _log.Warn(content, formatArgs);
        }
    }
}
