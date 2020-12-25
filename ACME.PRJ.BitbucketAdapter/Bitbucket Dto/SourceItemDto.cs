using System.Collections.Generic;

namespace ACME.PRJ.BitbucketAdapter
{
	internal class SourceItemDto
	{
		public SourceItemDtoPath path { get; set; }
		public string contentId { get; set; }
		public string node { get; set; }
		public string type { get; set; }
		public int size { get; set; }
	}

	internal class SourceItemDtoPath
	{
		public IEnumerable<string> components { get; set; }
		public string parent { get; set; }
		public string name { get; set; }
		public string extension { get; set; }
		public string toString { get; set; }
	}
}
