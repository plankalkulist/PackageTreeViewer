namespace ACME.PRJ.BitbucketAdapter
{
	internal class BranchDto
	{
		public string id { get; set; }
		public string displayId { get; set; }
		public string type { get; set; }
		public string latestCommit { get; set; }
		public string latestChangeset { get; set; }
		public bool isDefault { get; set; }
	}
}
