using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace MinioSyncCore
{
    /// <summary>
    /// 打印日志 
    /// </summary>
    public static class LogHelper
    {
        static ILog log = log4net.LogManager.GetLogger("loginfo");

        /// <summary>
        /// 打印提示
        /// </summary>
        /// <param name="txt"></param>
        public static void Info(string txt)
        {            
            log.Info(txt);
        }

        /// <summary>
        /// 打印提示
        /// </summary>
        /// <param name="txt"></param>
        public static void Info(string txt, Type type)
        {
            ILog log = log4net.LogManager.GetLogger(type);
            log.Info(txt);
        }

        /// <summary>
        /// 打印错误
        /// </summary>
        /// <param name="msg"></param>
        public static void Error(string msg)
        {
            log.Error(msg);
        }
        /// <summary>
        /// 打印错误
        /// </summary>
        /// <param name="msg"></param>
        public static void Error(string msg, Exception ex)
        {
            log.Error(msg, ex);
        }

        /// <summary>
        /// 打印警告
        /// </summary>
        /// <param name="msg"></param>
        public static void Warn(string msg)
        {
            log.Warn(msg);
        }


        /// <summary>
        /// 打印调试信息
        /// </summary>
        /// <param name="msg"></param>
        public static void Debug(string msg)
        {
            log.Debug(msg);
        }
    }
}
