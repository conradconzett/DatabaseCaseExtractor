using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseCaseExtractor.Models
{
    public class ExportInclude
    {
        public ExportInclude[] SubIncludes { get; set; }
        public string Include { get; set; }
    }
}
