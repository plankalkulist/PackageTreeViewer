namespace ACME.PRJ.CodebaseCommons
{
    /// <summary>
    /// Инфо о проекте (= совокупности репозиториев, собираемых(build) и разворачиваемых(deploy) вместе)
    /// </summary>
    public class ProjectInfo : InfoBase
	{
        public string Description { get; set; }

        public bool IsPublic { get; set; }

		public string Type { get; set; }

		public string Link { get; set; }

		public override bool IsValid()
		{
			return !string.IsNullOrWhiteSpace(Link)
				&& base.IsValid();
		}
	}
}
