using DatabaseCaseExtractor.Models;
using DatabaseCaseExtractor.Unittests.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DatabaseCaseExtractor.Unittests
{
    public class ImportTests
    {

        public DbContext _context;
        public ExportImportService<Table1> _exportService;

        public ImportTests()
        {
            var optionBuilder = new DbContextOptionsBuilder()
                .UseInMemoryDatabase(databaseName: "temp")
                .Options;
            _context = new TestContext(optionBuilder);

            var table1 = _context.Set<Table1>();
            var table3Entry = new Table3() {
                Id = 1,
                Name = "Test2"
            };
            var table2Entry = new Table2() {
                Name = "Test2",
                Id = "Abc",
                ThirdTableRows = new List<Table3>() { table3Entry }
            };
            var table1Entry = new Table1() {
                Name = "Test",
                SecondTableRows = new List<Table2>() { table2Entry },
                Id = Guid.Parse("542c31f0-35e3-4a7d-a939-803f18f94669")
            };
            table1.Add(table1Entry);

            var additionalData = _context.Set<AdditionalData>();
            additionalData.Add(new AdditionalData() {
                Name = "TestAdditional",
                Id = Guid.Parse("cca5e277-77e7-4d70-9eb1-067156222da3"),
            });
            _context.SaveChanges();

            _exportService = new ExportImportService<Table1>(_context);
        }

        [Fact]
        public void GetBasicsTest()
        {
            var exportResult = _exportService.SetImportResult(new ExportResult()
            {
                EntityName = "Table1",
                EntityData = new Table1()
                {
                    Name = "AAA 2",
                    Id = Guid.Parse("8ca5e277-77e7-4d70-9eb1-067156222da3"),
                    SecondTableRows = new Table2[] {
                        new Table2() {
                            Name = "AAA 3",
                            Id = "Cda",
                            ThirdTableRows = new Table3[] {
                                new Table3() {
                                    Name = "AAA 4",
                                    Id = 2
                                }
                            }
                        }
                    }
                }
            });
            // Check if result is true
            Assert.True(exportResult);
            DbSet<Table1> set = _context.Set<Table1>();

            Table1 result = set.FirstOrDefault();
            // Check if first Result is "AAA 2"
            Assert.True(result.Name == "AAA 2");
            // Check if Table was cleared
            Assert.True(set.Count<Table1>() == 1);
        }

        [Fact]
        public void GetAdditionalDataTest()
        {
            var exportResult = _exportService.SetImportResult(new ExportResult()
            {
                EntityName = "Table1",
                EntityData = new Table1()
                {
                    Name = "AAA 2",
                    Id = Guid.Parse("8ca5e277-77e7-4d70-9eb1-067156222da3"),
                    SecondTableRows = new Table2[] {
                        new Table2() {
                            Name = "AAA 3",
                            Id = "Cda",
                            ThirdTableRows = new Table3[] {
                                new Table3() {
                                    Name = "AAA 4",
                                    Id = 2
                                }
                            }
                        }
                    }
                },
                AdditionalData = new ExportResult[] {
                    new ExportResult() {
                        EntityName = "AdditionalData",
                        EntityData = new AdditionalData()
                        {
                            Name = "TestAdditional1",
                            Id = Guid.Parse("cca5e277-77e7-4d70-9eb1-067156222da4")
                        }
                    }
                }
            });
            // Check if result is true
            Assert.True(exportResult);
            DbSet<AdditionalData> set = _context.Set<AdditionalData>();

            AdditionalData result = set.FirstOrDefault();
            // Check if first Result is "TestAdditional1"
            Assert.True(result.Name == "TestAdditional1");
            // Check if Table was cleared
            Assert.True(set.Count<AdditionalData>() == 1);
        }
    }
}
