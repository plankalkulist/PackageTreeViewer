using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace ACME.PRJ.CodebaseCommons
{
	public class CodebaseServiceStub : ICodebaseService
	{
		public string BaseUrl { get; private set; }
		public string LoggedInUser { get; private set; }
		public bool IsLoggedIn => LoggedInUser != null;

		private const int _delay = 200;

		public CodebaseServiceStub()
			: this($"{Directory.GetCurrentDirectory()}\\..\\..\\..\\.TestData\\CodebaseCommons.Tests")
		{ }

		public CodebaseServiceStub(string baseUrl)
		{
			if (string.IsNullOrWhiteSpace(baseUrl))
				throw new ArgumentNullException(nameof(baseUrl));

			// пробуем пингануть юрл
            if (!Directory.Exists(baseUrl))
                throw new DirectoryNotFoundException();

            BaseUrl = baseUrl;
		}

		public void LogIn(string username, string password)
		{
			LoggedInUser = username;
		}

		public void LogOut()
		{
			ThrowIfNotLoggedIn();
			LoggedInUser = null;
		}

		public IEnumerable<ProjectInfo> GetProjects()
		{
			ThrowIfNotLoggedIn();

			var projectsDirectory = new DirectoryInfo(BaseUrl);

			Thread.Sleep(_delay);
            return projectsDirectory
                .GetDirectories()
                .Select(dir =>
                    new ProjectInfo
                    {
                        Id = Constants.UndefinedId,
                        Key = dir.Name,
                        Name = dir.Name,
                        Link = dir.FullName
                    });
        }

		public IEnumerable<RepoInfo> GetRepos(ProjectInfo projectInfo)
		{
			ThrowIfNotLoggedIn();
			ThrowIfNullOrInvalid(projectInfo);

			var projectDirectory = new DirectoryInfo($"{BaseUrl}\\{projectInfo.Key}");
			
			Thread.Sleep(_delay);
            return projectDirectory
                .GetDirectories()
                .Select(dir =>
                    new RepoInfo
                    {
                        Id = Constants.UndefinedId,
                        Key = dir.Name,
                        Name = dir.Name[0].ToString().ToUpper() + dir.Name.Substring(1),
                        State = RepoStates.Available,
                        StatusMessage = RepoStates.Available.ToString(),
                        Project = projectInfo
                    });
        }

		public IEnumerable<BranchInfo> GetBranches(RepoInfo repoInfo)
		{
			ThrowIfNotLoggedIn();
			ThrowIfNullOrInvalid(repoInfo);

			var repoDirectory = new DirectoryInfo($"{BaseUrl}\\{repoInfo.Project.Key}\\{repoInfo.Key}");

            var branchesDirectory = new DirectoryInfo(repoDirectory.FullName + "\\__branches");
            if (!branchesDirectory.Exists)
                return new []
                {
                    new BranchInfo
                    {
                        Id = Constants.UndefinedId,
                        Key = "..",
                        Name = "mockster", // "mock" + "master" = "mockster"
                        Repo = repoInfo
                    }
                };
				
			Thread.Sleep(_delay);
            return branchesDirectory
                .GetDirectories()
                .Select(dir =>
                    new BranchInfo
                    {
                        Id = Constants.UndefinedId,
                        Key = dir.Name,
						Name = dir.Name.Substring("refs_heads_".Length), // ключ ветки начинается с refs_heads_
                        Repo = repoInfo
                    });
        }

		public IEnumerable<SourceItemInfo> GetSourceItemsList(BranchInfo branchInfo, string path)
		{
			ThrowIfNotLoggedIn();
			ThrowIfNullOrInvalid(branchInfo);

			var sourceDirectory = new DirectoryInfo($"{BaseUrl}\\{branchInfo.Repo.Project.Key}\\{branchInfo.Repo.Key}\\__branches\\{branchInfo.Key}\\{path}");

            var dirs = sourceDirectory
                .GetDirectories()
                .Select(dir =>
                    new SourceItemInfo
                    {
                        Id = Constants.UndefinedId,
                        Key = dir.FullName,
                        Name = dir.Name,
                        FolderPath = sourceDirectory.FullName,
                        Type = SourceItemTypes.Directory,
                        Branch = branchInfo
                    });

            var files = sourceDirectory
                .GetFiles()
                .Select(file =>
                    new SourceItemInfo
                    {
                        Id = Constants.UndefinedId,
                        Key = file.FullName,
                        Name = file.Name,
                        Extension = string.IsNullOrEmpty(file.Extension)
                            ? file.Extension
                            : file.Extension[0] == '.'
                                ? file.Extension.Substring(1)
                                : file.Extension,
                        FolderPath = sourceDirectory.FullName,
                        Type = SourceItemTypes.File,
                        Branch = branchInfo
                    });
					
			Thread.Sleep(_delay);
            return dirs.Concat(files);
        }

		public string GetSourceFileContent(BranchInfo branchInfo, string path)
		{
			ThrowIfNotLoggedIn();
			ThrowIfNullOrInvalid(branchInfo);

			string content = null;

            using (StreamReader sr = new StreamReader(path))
            {
                content = sr.ReadToEnd();
            }
			
			Thread.Sleep(_delay * 2);
            return content;
		}

		private void ThrowIfNotLoggedIn()
		{
			if (!IsLoggedIn)
				throw new InvalidOperationException("Для этого действия необходима авторизация.");
		}

		private void ThrowIfNullOrInvalid(InfoBase infoArgument)
		{
			if (infoArgument == null)
				throw new ArgumentNullException(infoArgument.GetType().Name);

			if (!infoArgument.IsValid())
				throw new ArgumentException($"Ошибка в данных {infoArgument.GetType().Name}.");
		}
	}
}
