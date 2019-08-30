DatabaseCaseExtractor
=====================

DatabaseCaseExtractor provides logic to export and imports busines cases from/into a database. The projects is written in C# and uses .net core entity framework.

To use `DatabaseCaseExtractor` in your project it must meet the following requirements:

1. .NET Core 2.2 or later
2. Entity Framework Core 2.2.6

To build `DatabaseCaseExtractor` and run tests you will need Visual Studio 2019.


Installation
--------------------
1. `DatabaseCaseExtractor` can be installed via the nuget UI (as [DatabaseCaseExtractor](https://www.nuget.org/packages/DatabaseCaseExtractor)) or via the nuget package manager console:

        PM> Install-Package DatabaseCaseExtractor
2. Alternatively, you can grab sources from here and build.

Include DatabaseCaseExtractor into your project:
--------------------
After the installation you can provide the export and import functionality over your controllers. As following example:

```csharp
using Microsoft.AspNetCore.Mvc;
using DatabaseCaseExtractor;
using DatabaseCaseExtractor.Models;
using API.Models;

namespace API.Controllers
{
    [Route("Testcontroller")]
    public class ExportController : Controller {
	
		protected readonly ExampleContext _context;

        public BaseController(ExampleContext context)
        {
            _context = context;
        }


        [HttpPost]
        [Route("export")]
        public ActionResult<object> Export(ExportLayout layout)
        {
            ExportImportService<TestEntity> tempService = new ExportImportService<TestEntity>(_context);
            return Ok(tempService.GetExportResult(layout));
        }


	}
}
```

Import
--------------------
***IMPORTANT: Do only provide the import-service on test-environments. The default setting deletes all data from the database before new data will be importated.

```csharp
        [HttpPost]
        [Route("import")]
        public ActionResult<bool> Import(ExportResult importData)
        {
            ExportImportService<TestEntity> tempService = new ExportImportService<TestEntity>(_context);
            return Ok(tempService.SetImportResult(importData));
        }
```

Data exmport example
--------------------

```json

{
	"EntityName": "TestTable1",
	"EntityPrimaryKeyType": "Guid",
	"EntityPrimaryKey": "Id",
	"EntityPrimaryValue": "acb5c806-f1e7-4f1f-eb03-08d6c638e1e0",
	"Includes": [
		{
			"Include": "TestTable2",
			"SubIncludes": [
				{
					"Include": "TestTable3"
				}	
			]
		}
	],
	"AdditionalData": [
		{
			"EntityName": "TestTable4",
			"EntityPrimaryKeyType": "Guid",
			"EntityPrimaryKey": "Id"			
		}
	]
}

```
