using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text;

namespace SongFeedReaders.Logging
{
    public class FeedReaderLogger
        : FeedReaderLoggerBase
    {
        public FeedReaderLogger()
        {

        }

        public FeedReaderLogger(LoggingController controller)
            : this()
        {
            LogController = controller;
        }

        public override void Log(string message, LogLevel logLevel, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            if (LogLevel > logLevel)
            {
                return;
            }
            string sourcePart, timePart = "";
            if (!ShortSource)
                sourcePart = $"[{Path.GetFileName(file)}_{member}({line})";
            else
                sourcePart = $"[{LoggerName}";
            if (EnableTimestamp)
                timePart = $" @ {DateTime.Now.ToString("HH:mm")}";
            Console.WriteLine($"{sourcePart}{timePart} - {logLevel}] {message}");
        }

        public override void Log(string message, Exception e, LogLevel logLevel, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            if (LogLevel > logLevel)
            {
                return;
            }
            string sourcePart, timePart = "";
            if (!ShortSource)
                sourcePart = $"[{Path.GetFileName(file)}_{member}({line})";
            else
                sourcePart = $"[{LoggerName}";
            if (EnableTimestamp)
                timePart = $" @ {DateTime.Now.ToString("HH:mm")}";
            Console.WriteLine($"{sourcePart}{timePart} - {logLevel}] {message}: {e.Message}");
            Console.WriteLine(e);
        }
    }
}
