﻿using System;
using System.Configuration;
using System.IO;


namespace ServiceTest01
{
    public static class Logger
    {
        public enum LogLevel { DEBUG = 0, INFO = 1, WARNING = 2, CRITICAL = 3, ERROR = 4 };

        static LogLevel currLogLevel;
        static readonly TextWriter tw;
        private static readonly object _syncObject = new object();

        static Logger()
        {
            System.Collections.Specialized.NameValueCollection appSettings = ConfigurationManager.AppSettings;
            tw = TextWriter.Synchronized(File.AppendText(appSettings["logFileName"]));
            currLogLevel = (LogLevel)System.Enum.Parse(typeof(LogLevel), appSettings["logLevel"]);
        }

        public static void Log(LogLevel ll, string logMessage)
        {
            if (ll >= currLogLevel)
            {
                try
                {
                    Write(logMessage, tw, ll);
                }
                catch (IOException)
                {
                    tw.Close();
                }
            }
        }

        private static void Write(string logMessage, TextWriter w, LogLevel ll)
        {
            // only one thread can own this lock, so other threads 
            // entering this method will wait here until lock is
            // available.
            lock (_syncObject)
            {
                w.WriteLine("{0} {1} {2}: {3}", Enum.GetName(typeof(LogLevel), ll), DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString(), logMessage);
                // Update the underlying file.
                w.Flush();
            }
        }
    }
}