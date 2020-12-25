using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ACME.PRJ.CodebaseCommons;

namespace ACME.PRJ.CSharpTools
{
	/// <summary>
	/// Полезное содержимое файла решения C#
	/// </summary>
	public class CsSolutionFile
	{
		public Guid Guid { get; set; }

		public IEnumerable<CsProjectFileInfo> CsProjectFileInfos { get; set; }

		public SourceItemInfo SolutionFileInfo { get; set; }

		public bool IsValid()
		{
			return SolutionFileInfo?.IsValid() == true;
		}

		public static CsSolutionFile Parse(string slnFileContent, SourceItemInfo slnFileInfo)
		{
			if (string.IsNullOrEmpty(slnFileContent))
				throw new ArgumentNullException(nameof(slnFileContent));

			if (slnFileInfo == null)
				throw new ArgumentNullException(nameof(slnFileInfo));

			if (!slnFileInfo.IsValid())
				throw new ArgumentException(nameof(slnFileInfo));

			var solutionGuid = CsParsingHelper.ParseCsSolutionGuid(slnFileContent);

			return new CsSolutionFile
			{
				Guid = solutionGuid,
				CsProjectFileInfos = CsParsingHelper.ParseCsProjectFileInfos(slnFileContent)
					.Where(csProjInfo => !string.IsNullOrEmpty(Path.GetExtension(csProjInfo.RelativeToSolutionPath)))
					.Select(csProjInfo =>
					{
						csProjInfo.FullPath = PathHelper.CombinePaths(slnFileInfo.FolderPath, csProjInfo.RelativeToSolutionPath);
						csProjInfo.FolderPath = PathHelper.GetFolderPath(csProjInfo.FullPath);

						var extension = Path.GetExtension(csProjInfo.RelativeToSolutionPath);
						csProjInfo.Extension = extension.Substring(1);
						csProjInfo.Name = $"{csProjInfo.Name}.{csProjInfo.Extension}";

						csProjInfo.Id = Constants.UndefinedId;
						csProjInfo.SolutionGuid = solutionGuid;
						csProjInfo.Size = Constants.UnknownSize;
						csProjInfo.Type = SourceItemTypes.File;
						csProjInfo.Branch = slnFileInfo.Branch;

						return csProjInfo;
					}),
				SolutionFileInfo = slnFileInfo
			};
		}

		public static bool TryParse(string slnFileContent, SourceItemInfo slnFileInfo, out CsSolutionFile csSolutionFileInfo)
		{
			try
			{
				csSolutionFileInfo = CsSolutionFile.Parse(slnFileContent, slnFileInfo);
				return true;
			}
			catch
			{
				csSolutionFileInfo = null;
				return false;
			}
		}

		public override bool Equals(object obj)
		{
			var file = obj as CsSolutionFile;
			return file != null &&
				   Guid.Equals(file.Guid) &&
				   SolutionFileInfo.Equals(file.SolutionFileInfo);
		}
	}
}
