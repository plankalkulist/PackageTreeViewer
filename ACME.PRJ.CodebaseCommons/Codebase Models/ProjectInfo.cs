namespace ACME.PRJ.CodebaseCommons
{
	/// <summary>
	/// Инфо о проекте (напр. УФО, СКР и тд)
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
