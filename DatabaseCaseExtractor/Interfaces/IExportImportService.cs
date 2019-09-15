using DatabaseCaseExtractor.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseCaseExtractor.Interfaces
{
    public interface IExportImportService
    {
        ExportResult GetExportResult(ExportLayout exportLayout, bool loadAdditionalData = true);
        bool SetImportResult(ExportResult importData, bool clear);
    }
}
