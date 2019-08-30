using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseCaseExtractor.Models
{
    public class ExportResult
    {
        public string EntityName { get; set; }
        public object EntityData { get; set; }
        public object[] AdditionalData { get; set; }
    }
}
