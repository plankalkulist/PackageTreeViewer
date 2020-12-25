using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ACME.PRJ.CodebaseCommons
{
	public static class CodebaseServiceExtensions
	{
		public static BranchInfo GetDefaultBranch(this ICodebaseService codebaseService, RepoInfo repoInfo)
		{
			return codebaseService.GetBranches(repoInfo)
				.First(branch => branch.IsDefault);
		}

		public static IEnumerable<SourceItemInfo> GetSourceItemsList(this ICodebaseService codebaseService, BranchInfo branchInfo)
		{
			return codebaseService.GetSourceItemsList(branchInfo, string.Empty);
		}

		public static string GetSourceFileContent(this ICodebaseService codebaseService, SourceItemInfo fileInfo)
		{
			ThrowIfNullOrInvalid(fileInfo);

			return codebaseService.GetSourceFileContent(fileInfo.Branch, fileInfo.Key);
		}

		public static bool TryGetSourceFileContentIfExists(this ICodebaseService codebaseService, BranchInfo branchInfo, string path, out string content)
		{
			try
			{
				content = codebaseService.GetSourceFileContent(branchInfo, path);
				return true;
			}
			catch (FileNotFoundException)
			{
				content = null;
				return false;
			}
			catch (Exception e)//////////////////////////////////////////////////////////
			{
				throw;
			}
		}

		public static bool TryGetSourceFileContentIfExists(this ICodebaseService codebaseService, SourceItemInfo fileInfo, out string content)
		{
			ThrowIfNullOrInvalid(fileInfo);

			return codebaseService.TryGetSourceFileContentIfExists(fileInfo.Branch, fileInfo.Key, out content);
		}

		/// <summary>
		/// Рекурсивный поиск элементов в указанной папке репозитория по условию
		/// </summary>
		/// <param name="codebaseService">База кода</param>
		/// <param name="branchInfo">Инфо о ветке</param>
		/// <param name="path">Путь, с которого начинается поиск</param>
		/// <param name="resultCondition">Условие поиска</param>
		/// <param name="escapeFolderCondition">Условие выхода из папки (остановка рекурсии)</param>
		/// <param name="includeResultsFromEscapingFolder">Учитывать или нет результаты поиска из папки, в которой прекращается рекурсия</param>
		/// <param name="progressObserver">Наблюдатель за прогрессом выполнения</param>
		/// <returns>Список результатов, не null</returns>
		public static ICollection<SourceItemInfo> FindAllItems(this ICodebaseService codebaseService
			, BranchInfo branchInfo, string path, Func<SourceItemInfo, bool> resultCondition
			, Func<IEnumerable<SourceItemInfo>, bool> escapeFolderCondition, bool includeResultsFromEscapingFolder
			, ProgressObserver progressObserver = null)
		{
			ThrowIfNullOrInvalid(branchInfo);

			var resultList = new List<SourceItemInfo>(64);

			if (progressObserver != null)
			{
				progressObserver.ProgressData = $"[{branchInfo.Repo.Project.Name}/{branchInfo.Repo.Name}: {branchInfo.Name}]/{path}";
				progressObserver.Status = ProgressObserver.StartedStatus;
			}

			FindAllItemsBody(codebaseService, branchInfo, path, resultCondition, escapeFolderCondition
				, includeResultsFromEscapingFolder, ref resultList, progressObserver);

			if (progressObserver != null)
			{
				progressObserver.ProgressData = $"[{branchInfo.Repo.Project.Name}/{branchInfo.Repo.Name}: {branchInfo.Name}]/{path}";
				progressObserver.Status = ProgressObserver.FinishedStatus;
			}

			return resultList;
		}

		private static void FindAllItemsBody(ICodebaseService codebaseService
			, BranchInfo branchInfo, string path, Func<SourceItemInfo, bool> resultCondition
			, Func<IEnumerable<SourceItemInfo>, bool> escapeFolderCondition, bool includeResultsFromEscapingFolder
			, ref List<SourceItemInfo> results, ProgressObserver progressObserver)
		{
            var itemsList = codebaseService.GetSourceItemsList(branchInfo, path);
			if (!itemsList.Any())
				return;

			if (progressObserver != null)
				progressObserver.ProgressData = $"[{branchInfo.Repo.Project.Name}/{branchInfo.Repo.Name}: {branchInfo.Name}]/{path}";

			bool escaping = escapeFolderCondition(itemsList);
			if (escaping && !includeResultsFromEscapingFolder)
				return;

			var resultItemsList = itemsList.Where(i => resultCondition(i));
			foreach (var item in resultItemsList)
				results.Add(item);

			if (!escaping)
			{
				var folderList = itemsList.Where(i => i.Name?.Substring(0, 1) != "." && i.Type == SourceItemTypes.Directory);
				if (!folderList.Any())
					return;

				var pathPrefix = !string.IsNullOrEmpty(path) ? $"{path}/" : string.Empty;
				foreach (var folder in folderList)
					FindAllItemsBody(codebaseService, branchInfo, $"{pathPrefix}{folder.Name}", resultCondition
						, escapeFolderCondition, includeResultsFromEscapingFolder, ref results, progressObserver);
			}
		}

		private static void ThrowIfNullOrInvalid(InfoBase infoArgument)
		{
			if (infoArgument == null)
				throw new ArgumentNullException(infoArgument.GetType().Name);

			if (!infoArgument.IsValid())
				throw new ArgumentException($"Ошибка в данных {infoArgument.GetType().Name}.");
		}
	}
}
