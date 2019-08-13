using System;
using log4net;
using log4net.Config;

[assembly: XmlConfigurator(Watch = true)]
namespace Akzin.Crm.DataMigrator.Helpers
{
    public static class Log
    {
        private const string Repository = "Tracing";
        private static readonly ILog Logger = LogManager.GetLogger(Repository);

        public static void Debug(string s) => Logger.Debug(s);

        public static void Error(object exception) => Logger.Error(exception);

        public static void Info(string msg) => Logger.Info(msg);

        public static void Warn(Exception exception) => Logger.Warn(exception);

        public static void Warn(string message) => Logger.Warn(message);

        public static void Error(object message, Exception exception) => Logger.Error(message, exception);
    }
}
