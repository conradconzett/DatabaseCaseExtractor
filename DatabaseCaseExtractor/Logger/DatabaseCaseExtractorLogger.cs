using DatabaseCaseExtractor.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DatabaseCaseExtractor.Logger
{
    public class DatabaseCaseExtractorLogger : ILogger
    {
        public DatabaseCaseExtractorLogger()
        {
        }

        public static List<LogEntry> LogEntries { get; set; } = new List<LogEntry>();
        public static List<string> LoggedEntries { get; set; } = new List<string>();
        public static void ClearLogs()
        {
            LogEntries = new List<LogEntry>();
            LoggedEntries = new List<string>();
        }
        
        internal IExternalScopeProvider ScopeProvider { get; set; }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);

            if (message.Contains("INSERT INTO") || message.Contains("UPDATE") || message.Contains("DELETE")) {
                // Get Parameters 
                string[] parameters = GetParameters(message);
                // Get Query
                int commandIndex = message.IndexOf("INSERT");
                if (commandIndex == -1)
                {
                    commandIndex = message.IndexOf("UPDATE");
                }
                if (commandIndex == -1)
                {
                    commandIndex = message.IndexOf("DELETE");
                }
                string command = message.Substring(commandIndex);

                if (command.Contains("SELECT @@ROWCOUNT;"))
                {
                    command = command.Substring(0, command.IndexOf("SELECT @@ROWCOUNT;") - 2);
                }

                LogEntry entry = new LogEntry() { Command = command.Replace("\\r\\n", ""), Parameters = parameters };
                if (!LoggedEntries.Contains(entry.ToString()))
                {
                    LogEntries.Add(entry);
                    LoggedEntries.Add(entry.ToString());
                }
            }
        }

        private string[] GetParameters(string message)
        {
            int startIndex = message.IndexOf("[Parameters=") + 12;
            int length = message.IndexOf("], CommandType") - startIndex;
            string[] tempResultList = message
                .Substring(startIndex, length)
                .Split(new string[] { ", @p" }, StringSplitOptions.RemoveEmptyEntries)
                .ToList<string>()
                .Select(s => 
                    TranslateResult(s)
                )
                .ToArray();

            return tempResultList;
        }

        private string TranslateResult(string line)
        {
            line = line.Replace("p=", "").Substring(line.IndexOf("'") + 1);
            if (line.Contains("' (Size ="))
            {
                line = line.Substring(0, line.IndexOf("' (Size =") + 1);
            }
            return line.Substring(0, line.Length - 1);
        }

        public IDisposable BeginScope<TState>(TState state) => ScopeProvider?.Push(state) ?? NullScope.Instance;

        public void Dispose()
        {
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }
    }
}
