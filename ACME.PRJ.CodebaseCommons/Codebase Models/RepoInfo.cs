namespace ACME.PRJ.CodebaseCommons
{
	/// <summary>
	/// Инфо о репозитории
	/// </summary>
	public class RepoInfo : InfoBase
	{
		public RepoStates State { get; set; }

		public string StatusMessage { get; set; }

		public bool Forkable { get; set; }

		public bool IsPublic { get; set; }

		public ProjectInfo Project { get; set; }

		public override bool IsValid()
		{
			return !string.IsNullOrWhiteSpace(StatusMessage)
				&& (Project?.IsValid() == true)
				&& base.IsValid();
		}
	}

	public enum RepoStates
	{
		Unknown = 0,
		Available = 1
	}
}
