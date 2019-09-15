using DatabaseCaseExtractor.Models;
using DatabaseCaseExtractor.Unittests.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DatabaseCaseExtractor.Unittests
{
    public class ExportTests
    {

        public DbContext _context;
        public ExportImportService<Table1> _exportService;

        public ExportTests()
        {
        }

        private void InitData(string databaseName)
        {
            var optionBuilder = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;
            _context = new TestContext(optionBuilder);

            var table1 = _context.Set<Table1>();
            var table3Entry = new Table3()
            {
                Id = 1,
                Name = "Test2"
            };
            var table2Entry = new Table2()
            {
                Name = "Test2",
                Id = "Abc",
                ThirdTableRows = new List<Table3>() { table3Entry }
            };
            var table1Entry = new Table1()
            {
                Name = "Test",
                SecondTableRows = new List<Table2>() { table2Entry },
                Id = Guid.Parse("542c31f0-35e3-4a7d-a939-803f18f94669")
            };
            table1.Add(table1Entry);

            var table1Entry_2 = new Table1()
            {
                Name = "Test",
                Id = Guid.Parse("642c31f0-35e3-4a7d-a939-803f18f94669")
            };
            table1.Add(table1Entry_2);

            var additionalData = _context.Set<AdditionalData>();
            additionalData.Add(new AdditionalData()
            {
                Name = "TestAdditional",
                Id = Guid.Parse("cca5e277-77e7-4d70-9eb1-067156222da3"),
            });
            _context.SaveChanges();

            _exportService = new ExportImportService<Table1>(_context);

        }

        [Fact]
        public void GetNullTest()
        {
            InitData("GetNullTest");
            var exportResult = _exportService.GetExportResult(new ExportLayout()
            {
                EntityPrimaryValue = "542c31f0-35e3-4a7d-a939-803f18f94668" // Wrong ID
            });
            Assert.Null(exportResult.EntityData);
        }

        [Fact]
        public void GetAlldataTest()
        {
            InitData("GetAlldataTest");
            var exportResult = _exportService.GetExportResult(new ExportLayout());
            Assert.NotNull(exportResult.EntityData);
        }

        [Fact]
        public void GetLevel1Test()
        {
            InitData("GetLevel1Test");
            var exportLayout = new ExportLayout()
            {
                UseModelAttributes = false,
                EntityPrimaryKey = "Id",
                EntityPrimaryValue = "542c31f0-35e3-4a7d-a939-803f18f94669",
                AdditionalData = new ExportLayout[] { }
            };
            var exportResult = _exportService.GetExportResult(exportLayout);
            Assert.True(exportResult.EntityData != null);
            Assert.True(exportResult.AdditionalData.Length == 0);
            Assert.True(((Table1)exportResult.EntityData).Id == Guid.Parse("542c31f0-35e3-4a7d-a939-803f18f94669"));
            Assert.True(((Table1)exportResult.EntityData).SecondTableRows == null);
        }

        [Fact]
        public void GetLevel2Test()
        {
            InitData("GetLevel2Test");
            var exportLayout = new ExportLayout()
            {
                EntityPrimaryKey = "Id",
                EntityPrimaryValue = "542c31f0-35e3-4a7d-a939-803f18f94669",
                Includes = (new List<ExportInclude>() {
                    new ExportInclude() { Include = "SecondTableRows" }
                }).ToArray()
            };
            var exportResult = _exportService.GetExportResult(exportLayout);
            Assert.True(exportResult.EntityData != null);
            Assert.True(exportResult.AdditionalData.Length == 0);
            Assert.True(((Table1)exportResult.EntityData).Id == Guid.Parse("542c31f0-35e3-4a7d-a939-803f18f94669"));
            Assert.True(((Table1)exportResult.EntityData).SecondTableRows.Count == 1);
        }

        [Fact]
        public void GetLevel3Test()
        {
            InitData("GetLevel3Test");
            var exportLayout = new ExportLayout()
            {
                EntityPrimaryKey = "Id",
                EntityPrimaryValue = "542c31f0-35e3-4a7d-a939-803f18f94669",
                Includes = (new List<ExportInclude>() {
                    new ExportInclude() {
                        Include = "SecondTableRows",
                        SubIncludes = (new List<ExportInclude>() {
                            new ExportInclude() {
                                Include = "ThirdTableRows"
                            }
                        }).ToArray()
                    }
                }).ToArray()
            };
            var exportResult = _exportService.GetExportResult(exportLayout);
            Assert.True(exportResult.EntityData != null);
            Assert.True(exportResult.AdditionalData.Length == 0);
            Assert.True(((Table1)exportResult.EntityData).Id == Guid.Parse("542c31f0-35e3-4a7d-a939-803f18f94669"));
            Assert.True(((Table1)exportResult.EntityData).SecondTableRows.Count == 1);
            Assert.True(((Table1)exportResult.EntityData).SecondTableRows.First().ThirdTableRows.Count == 1);
        }
        [Fact]
        public void GetLevelAdditionalDataTest()
        {
            InitData("GetLevelAdditionalDataTest");
            var exportLayout = new ExportLayout()
            {
                EntityPrimaryKey = "Id",
                EntityPrimaryValue = "542c31f0-35e3-4a7d-a939-803f18f94669",
                Includes = (new List<ExportInclude>() {
                    new ExportInclude() {
                        Include = "SecondTableRows",
                        SubIncludes = (new List<ExportInclude>() {
                            new ExportInclude() {
                                Include = "ThirdTableRows"
                            }
                        }).ToArray()
                    }
                }).ToArray(),
                AdditionalData = new ExportLayout[] {
                    new ExportLayout()
                    {
                        EntityName = "AdditionalData",
                        EntityPrimaryValue = "cca5e277-77e7-4d70-9eb1-067156222da3"
                    }
                }
            };
            var exportResult = _exportService.GetExportResult(exportLayout);
            Assert.True(exportResult.EntityData != null);
            Assert.True(((Table1)exportResult.EntityData).Id == Guid.Parse("542c31f0-35e3-4a7d-a939-803f18f94669"));
            Assert.True(((Table1)exportResult.EntityData).SecondTableRows.Count == 1);
            Assert.True(((Table1)exportResult.EntityData).SecondTableRows.First().ThirdTableRows.Count == 1);
            Assert.True(exportResult.AdditionalData.Length > 0);
            Assert.True(((AdditionalData)((ExportResult)exportResult.AdditionalData.First()).EntityData).Name == "TestAdditional");
        }
    }
}
