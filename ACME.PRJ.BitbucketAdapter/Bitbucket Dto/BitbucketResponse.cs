using System.Collections.Generic;

namespace ACME.PRJ.BitbucketAdapter
{
	internal class BitbucketResponse<T>
	{
		public int size { get; set; }
		public int limit { get; set; }
		public bool isLastPage { get; set; }
		public IEnumerable<T> values { get; set; }
		public int start { get; set; }
	}
}
