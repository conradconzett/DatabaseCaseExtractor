using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DatabaseCaseExtractor.Models
{
    public class ExportLayout
    {
        public bool UseModelAttributes { get; set; } = true;
        public string EntityName { get; set; }
        public string EntityPrimaryKeyType { get; set; }
        public string EntityPrimaryKey { get; set; }
        public string EntityPrimaryValue { get; set; }

        public ExportInclude[] Includes { get; set; }
        public ExportLayout[] AdditionalData { get; set; }

    }
}
