using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using ACME.PRJ.CodebaseCommons;

namespace ACME.PRJ.BitbucketAdapter
{
	public class BitbucketService : ICodebaseService
	{
		public string BaseUrl { get; private set; }
		public string LoggedInUser { get; private set; }
		public bool IsLoggedIn => LoggedInUser != null;

		private string EncodedAuthString;

		public BitbucketService(string baseUrl)
		{
			if (string.IsNullOrWhiteSpace(baseUrl))
				throw new ArgumentNullException(nameof(baseUrl));

			// пробуем пингануть юрл
			// . . .

			BaseUrl = baseUrl;
		}

		public void LogIn(string username, string password)
		{
			if (string.IsNullOrWhiteSpace(username))
				throw new ArgumentNullException(nameof(username));

			var encodedAuthString = BitbucketServiceHelper.GetEncodedAuthString(username, password);

			// пробуем любой запрос к Bitbucket API с авторизацией
			var projectDtos = BitbucketServiceHelper.GetValues<ProjectDto>(EncodedAuthString, $"{BaseUrl}/projects");
			if (string.IsNullOrEmpty(projectDtos.FirstOrDefault()?.key))
				throw new ArgumentException($"{nameof(username)}/{nameof(password)}");

			LoggedInUser = username;
			EncodedAuthString = encodedAuthString;
		}

		public void LogOut()
		{
			ThrowIfNotLoggedIn();

			LoggedInUser = null;
			EncodedAuthString = null;
		}

		public IEnumerable<ProjectInfo> GetProjects()
		{
			ThrowIfNotLoggedIn();

			var projectDtos = BitbucketServiceHelper.GetValues<ProjectDto>(EncodedAuthString, $"{BaseUrl}/projects");

			return projectDtos.Select(
				dto => new ProjectInfo
				{
					Id = dto.id,
					Key = dto.key,
					Name = dto.name,
                    Description = dto.description,
					Link = dto.links.self.FirstOrDefault().href
				});
		}

		public IEnumerable<RepoInfo> GetRepos(ProjectInfo projectInfo)
		{
			ThrowIfNotLoggedIn();
			ThrowIfNullOrInvalid(projectInfo);

			const string repoDtoAvailableState = "AVAILABLE";
			var repoDtos = BitbucketServiceHelper.GetValues<RepoDto>(EncodedAuthString
				, $"{BaseUrl}/projects/{projectInfo.Key}/repos");

			return repoDtos.Select(
				dto => new RepoInfo
				{
					Id = dto.id,
					Key = dto.slug,
					Name = dto.name,
					State = dto.state == repoDtoAvailableState
						? RepoStates.Available
						: RepoStates.Unknown,
					StatusMessage = dto.statusMessage,
					Forkable = dto.forkable,
					IsPublic = dto.@public,
					Project = projectInfo
				});
		}

		public IEnumerable<BranchInfo> GetBranches(RepoInfo repoInfo)
		{
			ThrowIfNotLoggedIn();
			ThrowIfNullOrInvalid(repoInfo);

			var branchDtos = BitbucketServiceHelper.GetValues<BranchDto>(EncodedAuthString
				, $"{BaseUrl}/projects/{repoInfo.Project.Key}/repos/{repoInfo.Key}/branches");

			return branchDtos.Select(
				dto => new BranchInfo
				{
					Id = repoInfo.Project.Id ^ repoInfo.Id ^ dto.id.GetHashCode(),
					Key = dto.id,
					Name = dto.displayId,
					LatestCommitId = dto.latestCommit,
					LatestChangesetId = dto.latestChangeset,
					IsDefault = dto.isDefault,
					Repo = repoInfo
				});
		}

		public IEnumerable<SourceItemInfo> GetSourceItemsList(BranchInfo branchInfo, string path)
		{
			ThrowIfNotLoggedIn();
			ThrowIfNullOrInvalid(branchInfo);

			const string sourceItemTypeFile = "FILE";
			const string sourceItemTypeDirectory = "DIRECTORY";

			var normPath = PathHelper.NormalizePath(path, '/');
			normPath = path != null ? $"/{path}" : string.Empty;

			var srcItemDtos = BitbucketServiceHelper.GetValues<SourceItemDto>(EncodedAuthString
				, $"{BaseUrl}/projects/{branchInfo.Repo.Project.Key}/repos/{branchInfo.Repo.Key}/browse{normPath}"
				, branchInfo.Key);

			// TODO: разобраться c contentId и node
			return srcItemDtos.Select(
				dto => new SourceItemInfo
				{
					Id = Constants.UndefinedId,
					Key = $"{path}/{dto.path.toString}",
					Name = dto.path.toString,
					FolderPath = path,
					Extension = dto.path.extension,
					//Parent = dto.path.parent,
					ContentId = dto.contentId,
					Node = dto.node,
					Type = dto.type == sourceItemTypeFile ? SourceItemTypes.File
						: dto.type == sourceItemTypeDirectory ? SourceItemTypes.Directory
						: SourceItemTypes.Unknown,
					Size = dto.size,
					Branch = branchInfo
				});
		}

		public string GetSourceFileContent(BranchInfo branchInfo, string path)
		{
			ThrowIfNotLoggedIn();
			ThrowIfNullOrInvalid(branchInfo);

			var normPath = PathHelper.NormalizePath(path, '/');
			if (string.IsNullOrWhiteSpace(normPath))
				throw new ArgumentNullException(nameof(path));

			try
			{
				var content = BitbucketServiceHelper.GetResponse(EncodedAuthString
					, $"{BaseUrl}/projects/{branchInfo.Repo.Project.Key}/repos/{branchInfo.Repo.Key}/raw/{normPath}"
					, $"at={branchInfo.Key}");

				return content;
			}
			catch (WebException e)
			{
				if ((e?.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
					throw new FileNotFoundException(e.Message);

				throw;
			}

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
