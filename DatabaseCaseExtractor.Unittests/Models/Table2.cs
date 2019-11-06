using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DatabaseCaseExtractor.Unittests.Models
{
	public class Table2
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public string Id { get; set; }
		public string Name { get; set; }
		public Table1 Table1Id { get; set; }
		public ICollection<Table3> ThirdTableRows { get; set; }

	}
}
