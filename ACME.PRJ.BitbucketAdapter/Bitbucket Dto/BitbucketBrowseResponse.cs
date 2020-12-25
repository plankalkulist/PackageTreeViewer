using System.Collections.Generic;

namespace ACME.PRJ.BitbucketAdapter
{
	internal class BitbucketBrowseResponse
	{
		public BitbucketBrowseResponsePath path { get; set; }
		public string revision { get; set; }
		public BitbucketResponse<SourceItemDto> children { get; set; }
	}

	internal class BitbucketBrowseResponsePath
	{
		public IEnumerable<object> components { get; set; }
		public string name { get; set; }
		public string toString { get; set; }
	}
}
