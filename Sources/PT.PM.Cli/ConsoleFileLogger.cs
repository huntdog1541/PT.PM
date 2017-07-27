﻿using System;
using NLog;
using PT.PM.Common;

namespace PT.PM.Cli
{
    public class ConsoleFileLogger : FileLogger
    {
        protected Logger NLogConsoleLogger { get; } = LogManager.GetLogger("console");

        public override void LogError(Exception ex)
        {
            base.LogError(ex);
            if (IsLogErrors)
            {
                NLogConsoleLogger.Error($"Error: {PrepareForConsole(ex.Message.Trunc())}");
            }
        }

        public override void LogInfo(string message)
        {
            string truncatedMessage = message.Trunc();
            base.LogInfo(truncatedMessage);
            NLogConsoleLogger.Info(PrepareForConsole(truncatedMessage));
        }

        public override void LogDebug(string message)
        {
            string truncatedMessage = message.Trunc();
            base.LogDebug(truncatedMessage);
            if (IsLogDebugs)
            {
                NLogConsoleLogger.Debug(PrepareForConsole(truncatedMessage));
            }
        }

        protected string PrepareForConsole(string str)
        {
            return str.Replace("\a", "");
        }
    }
}