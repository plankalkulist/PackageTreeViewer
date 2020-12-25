using System.Collections.Generic;

namespace ACME.PRJ.BitbucketAdapter
{
	internal class ProjectDto
	{
		public string key { get; set; }
		public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public bool @public { get; set; }
		public string type { get; set; }
		public ProjectDtoLinksCollection links { get; set; }
	}

	internal class ProjectDtoLinksCollection
	{
		public IEnumerable<ProjectDtoLink> self { get; set; }
	}

	internal class ProjectDtoLink
	{
		public string href { get; set; }
	}

}
