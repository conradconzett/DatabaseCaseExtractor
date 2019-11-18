using DatabaseCaseExtractor.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseCaseExtractor.Interfaces
{
	public interface IExportImportService
	{
		void ClearContext();
		void GetExportZip(ExportLayout exportLayout, string outputfile);

		List<SimpleExportResult> GetExportResult(ExportLayout exportLayout, bool loadAdditionalData = true);
		void SetImportResult(ExportResult importData, bool clear = true, bool doUpdate = false);
		void SetImportResultWithoutSave(ExportResult importData, bool doUpdate = false);
		List<LogEntry> ExportSQLScripts(ExportResult initialData, ExportResult newData);
	}
}
