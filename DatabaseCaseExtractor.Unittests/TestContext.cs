using DatabaseCaseExtractor.Unittests.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseCaseExtractor.Unittests
{
	public class TestContext : DbContext
	{
		public TestContext(DbContextOptions options) : base(options)
		{

		}

		#region DbSets

		public DbSet<Table1> Table1s { get; set; }
		public DbSet<Table2> Table2s { get; set; }
		public DbSet<Table3> Table3s { get; set; }
		public DbSet<AdditionalData> AdditionalDatas { get; set; }

		#endregion


	}
}
