namespace ACME.PRJ.BitbucketAdapter
{
	internal class RepoDto
	{
		public string slug { get; set; }
		public int id { get; set; }
		public string name { get; set; }
		public string scmId { get; set; }
		public string state { get; set; }
		public string statusMessage { get; set; }
		public bool forkable { get; set; }
		public bool @public { get; set; }
		public RepoDtoProject project { get; set; }
	}

	internal class RepoDtoProject
	{
		public int id { get; set; }
	}
}
