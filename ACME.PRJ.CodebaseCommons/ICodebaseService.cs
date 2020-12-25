using System.Collections.Generic;

namespace ACME.PRJ.CodebaseCommons
{
	/// <summary>
	/// Сервис для работы с базой кода
	/// </summary>
	public interface ICodebaseService
	{
		string BaseUrl { get; }
		bool IsLoggedIn { get; }
		string LoggedInUser { get; }

		void LogIn(string username, string password);
		void LogOut();
		IEnumerable<ProjectInfo> GetProjects();
		IEnumerable<RepoInfo> GetRepos(ProjectInfo projectInfo);
		IEnumerable<BranchInfo> GetBranches(RepoInfo repoInfo);
		IEnumerable<SourceItemInfo> GetSourceItemsList(BranchInfo branchInfo, string path);
		string GetSourceFileContent(BranchInfo branchInfo, string path);
	}
}
