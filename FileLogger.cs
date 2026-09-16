using System;
using System.IO;

namespace BurhaniGuards.Api
{
    public static class FileLogger
    {
        public static void Log(string message)
        {
            try {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "miqaat_log.txt");
                File.AppendAllText(logPath, $"{DateTime.Now}: {message}\n");
            } catch {}
        }
    }
}
