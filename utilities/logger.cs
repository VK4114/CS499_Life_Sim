using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

namespace Utilities
{
   public class Logger
    {
        public enum Level
        {
            FATAL = 0,
            ERROR = 1,
            WARN = 2,
            INFO = 3,
            DEBUG = 4,
            VERBOSE = 5


        }

        private static readonly ConcurrentDictionary<string, Logger> m_instances = new();
        private static readonly object instanceLock = new();
        private readonly string  m_componentName;
        private readonly StreamWriter? m_logFile;
        private readonly object m_fileLock = new();
        private Level m_levelFilter;
        private readonly StringBuilder m_currentMessage = new();
        public static int GlobalLogLevel { get; set; } = (int)Level.DEBUG;

        public static Logger GetInstance(string componentName = "Default")
        {
            return m_instances.GetOrAdd(componentName, name => new Logger(name));
        }

        private Logger(string componentName)
        {
            m_componentName = componentName;
            m_levelFilter = (Level)GlobalLogLevel;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string solutionRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
            string logDir = Path.Combine(solutionRoot, "logs");
            Directory.CreateDirectory(logDir);
            
            string logFileName = Path.Combine(logDir, $"{componentName}.log");
            
            

            if(componentName != "Default")
            {
                try
                {
                        m_logFile = new StreamWriter(new FileStream(logFileName, FileMode.Append, FileAccess.Write, FileShare.Read))
                    {
                        AutoFlush = true
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to open log file {logFileName}: {ex.Message}");
                }
            }

        }

        private string LevelName(Level level) => level switch
        {
            Level.FATAL => "FATAL",
            Level.ERROR => "ERROR",
            Level.WARN => "WARN",
            Level.INFO => "INFO",
            Level.DEBUG => "DEBUG",
            Level.VERBOSE => "VERBOSE",
            _ => "XXX"
        };

        private string Timestamp()
        {
            var now = DateTime.Now;
            return now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        public Logger Fatal(string message) => Log(Level.FATAL, message);
        public Logger Error(string message) => Log(Level.ERROR, message);
        public Logger Warn(string message) => Log(Level.WARN, message);
        public Logger Info(string message) => Log(Level.INFO, message);
        public Logger Debug(string message) => Log(Level.DEBUG, message);
        public Logger Verbose(string message) => Log(Level.VERBOSE, message);
      
        public Logger Log(Level level, string message)
        {
            if (level > m_levelFilter)
                return this;

            string stamp = $"[{LevelName(level)} - {Timestamp()}]";
            string logLine = $"{stamp} [{m_componentName}] {message}";

            lock (m_fileLock)
            {
                m_logFile?.WriteLine(logLine);
            }

            if(level <= Level.ERROR)
            {
                Console.Error.WriteLine(logLine);
            }
            else
            {
                Console.WriteLine(logLine);
            }

            return this;
        }

        public Logger LogFormat(Level level, string format, params object[] args)
        {
            return Log(level, string.Format(format, args));
        }

        public Logger Log(Level level, FormattableString message)
        {
            return Log(level, message.ToString());
        }
       

        public void SetLevel(Level level)
        {
            m_levelFilter = level;
        }

        public void SetLevel(int level)
        {
            m_levelFilter = (Level)level;
        }

        public int GetLevel() => (int)m_levelFilter;

        ~Logger()
        {
            m_logFile?.Close();
        }


      


        


    }

    public static class Log
    {
        public static Logger For<T>() => Logger.GetInstance(typeof(T).Name);
        public static Logger For(string componentName) => Logger.GetInstance(componentName);
    }
    

}
