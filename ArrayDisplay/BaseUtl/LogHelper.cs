using System;
using log4net;

namespace ArrayDisplay.BaseUtl
{
    public class LogHelper
    {
        private static readonly ILog Infolog = LogManager.GetLogger("infolog");
        private static readonly ILog Errorlog = LogManager.GetLogger("errorlog");

        public static void LogInfo(string info)
        {
            if (Infolog.IsInfoEnabled)
            {
                Infolog.Info(info);
            }
        }

        public static void LogError(Exception se)
        {
            if (Errorlog.IsErrorEnabled)
            {
                Errorlog.Error(se.Message, se);
            }
        }
        public static void LogDebug(Exception se)
        {
            if (Errorlog.IsDebugEnabled)
            {
                Errorlog.Debug(se.Message, se);
            }
        }
        public static void LogFatal(Exception se)
        {
            if (Errorlog.IsFatalEnabled)
            {
                Errorlog.Fatal(se.Message, se);
                
            }
        }
        public static void Log(Exception se)
        {
            if (Errorlog.IsWarnEnabled)
            {
                Errorlog.Warn(se.Message, se);
            }
        }
    }
}