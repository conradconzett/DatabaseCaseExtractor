using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseCaseExtractor.Models
{
    public class LogEntry
    {
        public string Command { get; set; }
        public string[] Parameters { get; set; }
        public string ToString()
        {
            return Command + "|" + Parameters.ToString();
        }
    }
}
