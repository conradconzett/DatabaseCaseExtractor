using DatabaseCaseExtractor.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
                Dictionary<string, string> parameters = GetParameters(message);
                foreach(string commandString in message.Split(';'))
                {
                    // Get Query
                    int commandIndex = commandString.IndexOf("INSERT");
                    if (commandIndex == -1)
                    {
                        commandIndex = commandString.IndexOf("UPDATE");
                    }
                    if (commandIndex == -1)
                    {
                        commandIndex = commandString.IndexOf("DELETE");
                    }
                    if (commandIndex == -1)
                    {
                        continue;
                    }
                    string command = commandString.Substring(commandIndex);

                    if (command.Contains("SELECT @@ROWCOUNT;"))
                    {
                        command = command.Substring(0, command.IndexOf("SELECT @@ROWCOUNT;") - 2);
                    }

                    // Get Parameters for this commandString
                    Dictionary<string, string> tempParameters = new Dictionary<string, string>();
                    foreach(KeyValuePair<string,string> keyValue in parameters)
                    {
                        if (command.Contains(keyValue.Key))
                        {
                            tempParameters.Add(keyValue.Key, keyValue.Value);
                        }
                    }


                    LogEntry entry = new LogEntry() { Command = command.Replace("\\r\\n", ""), Parameters = tempParameters };
                    

                    if (!LoggedEntries.Contains(entry.ToString()))
                    {
                        LogEntries.Add(entry);
                        LoggedEntries.Add(entry.ToString());
                    }
                }
            }
        }

        private Dictionary<string, string> GetParameters(string message)
        {
            int startIndex = message.IndexOf("[Parameters=") + 13;
            int length = message.IndexOf("], CommandType") - startIndex;

            Dictionary<string, string> result = new Dictionary<string, string>();
            // Get Parameters-String
            string onlyParameters = message
                .Substring(startIndex, length);
            string[] potentialParameters = onlyParameters.Split(new string[] { "@p" }, StringSplitOptions.RemoveEmptyEntries);
            foreach(string potentialParameter in potentialParameters)
            {
                // Check if it is a parameter -> Means @p[index]=' @p is removed
                string index = potentialParameter.Substring(0, potentialParameter.IndexOf("='"));
                if (Regex.IsMatch(index, @"\d"))
                {
                    result.Add("@p" + index, potentialParameter.Substring(index.Length + 2, potentialParameter.LastIndexOf("'") - index.Length - 2));
                }
            }
            return result;
            /*

            List<string> tempResultList = message
                .Substring(startIndex, length)
                .Split(new string[] { ", @p" }, StringSplitOptions.RemoveEmptyEntries)
                .ToList<string>()
                .Select(s => 
                    TranslateResult(s)
                ).ToList();
            Dictionary<string, string> result = new Dictionary<string, string>();
            int i = 0;
            foreach(string value in tempResultList)
            {
                result.Add("@p" + i, value);
                i++;
            }
            return result;
            */
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
