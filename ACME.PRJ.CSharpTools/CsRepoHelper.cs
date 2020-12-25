using ACME.PRJ.CodebaseCommons;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ACME.PRJ.CSharpTools
{
	/// <summary>
	/// Вспомогательные методы для работы с репозиториями C#
	/// </summary>
	public static class CsRepoHelper
	{
		/// <summary>
		/// Получает последовательность всех солюшенов из репозитория (на указанной ветке)
		/// </summary>
		public static IEnumerable<Result<CsSolution>> GetAllSolutions(
			ICodebaseService codebaseService
			, BranchInfo branch
			, ProgressObserver progressObserver = null)
		{
			if (codebaseService == null)
				throw new ArgumentNullException(nameof(codebaseService));

			bool isSolutionFileInfo(SourceItemInfo item)
				=> item.Type == SourceItemTypes.File && item.Extension?.ToLower() == "sln";

			return codebaseService.FindAllItems(
					branch
					, null
					, isSolutionFileInfo
					, items => items.Any(isSolutionFileInfo)
					, true
					, progressObserver)
				.Select(solutionFileInfo =>
				{
					try
					{
						var solutionFileContent = codebaseService.GetSourceFileContent(solutionFileInfo);
						var solutionFile = CsSolutionFile.Parse(solutionFileContent, solutionFileInfo);
						var projectFiles = solutionFile.CsProjectFileInfos
							.Select(projectFileInfo => CsProjectFile.Parse(codebaseService, projectFileInfo));

						return new Result<CsSolution>
						{
							IsSuccessful = true,
							Data = new CsSolution(solutionFile, projectFiles)
						};
					}
					catch (Exception e)
					{
						return new Result<CsSolution>
						{
							IsSuccessful = false,
							Tag = solutionFileInfo,
							ErrorMessage = e.Message
						};
					}
				});
		}
	}

	/// <summary>
	/// Обёртка для результата выполнения сложной функции
	/// </summary>
	public class Result<T>
	{
		public bool IsSuccessful { get; set; }

		public T Data { get; set; }

		public object Tag { get; set; }

		public string ErrorMessage { get; set; }
	}
}
