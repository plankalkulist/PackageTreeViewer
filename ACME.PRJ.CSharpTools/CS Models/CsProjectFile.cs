using System.Collections.Generic;
using ACME.PRJ.CodebaseCommons;

namespace ACME.PRJ.CSharpTools
{
	/// <summary>
	/// Полезное содержимое файла проекта решения C#
	/// </summary>
	public class CsProjectFile
	{
		public string Version { get; set; }

		public string TargetFramework { get; set; }

		public bool GeneratePackageOnBuild { get; set; }

		public string Authors { get; set; }

		public string Description { get; set; }

		public string Company { get; set; }

		public string Product { get; set; }

		public string AssemblyName { get; set; }

		public string RootNamespace { get; set; }

		public IEnumerable<PackageReference> PackageReferences { get; set; }

		public IEnumerable<ProjectReference> ProjectReferences { get; set; }

		public IEnumerable<DotNetCliToolReference> DotNetCliToolReferences { get; set; }

		public bool IsNewFormat { get; set; }

		public CsProjectFileInfo ProjectFileInfo { get; set; }

		public string Name => ProjectFileInfo.Name;

		public bool IsValid()
		{
			return ProjectFileInfo?.IsValid() == true;
		}

		public override string ToString()
		{
			return Name;
		}

		public static CsProjectFile Parse(ICodebaseService codebaseService, CsProjectFileInfo projFileInfo)
		{
			var projFileContent = codebaseService.GetSourceFileContent(projFileInfo);

			var result = new CsProjectFile
			{
				ProjectFileInfo = projFileInfo,
				//IsNewFormat = CsParsingHelper.IsProjectOfNewFormat(projFileContent)
			};
			CsParsingHelper.ParseFieldsFromCsProjectXml(result, projFileContent);

			if (!result.IsNewFormat)
			{
				if (codebaseService.TryGetSourceFileContentIfExists(projFileInfo.Branch
					, $"{projFileInfo.FolderPath}/packages.config", out var packagesConfigContent))
				{
					result.PackageReferences = CsParsingHelper.ParsePackageReferencesFromConfig(packagesConfigContent);
				}
			}

			return result;
		}

		public static bool TryParse(ICodebaseService codebaseService, CsProjectFileInfo projFileInfo, out CsProjectFile csProject)
		{
			try
			{
				csProject = CsProjectFile.Parse(codebaseService, projFileInfo);
				return true;
			}
			catch
			{
				csProject = null;
				return false;
			}
		}

		public override bool Equals(object obj)
		{
			var file = obj as CsProjectFile;
			return file != null &&
				   Version == file.Version &&
				   TargetFramework == file.TargetFramework &&
				   GeneratePackageOnBuild == file.GeneratePackageOnBuild &&
				   Authors == file.Authors &&
				   Description == file.Description &&
				   Company == file.Company &&
				   Product == file.Product &&
				   AssemblyName == file.AssemblyName &&
				   RootNamespace == file.RootNamespace &&
				   EqualityComparer<IEnumerable<PackageReference>>.Default.Equals(PackageReferences, file.PackageReferences) &&
				   EqualityComparer<IEnumerable<ProjectReference>>.Default.Equals(ProjectReferences, file.ProjectReferences) &&
				   EqualityComparer<IEnumerable<DotNetCliToolReference>>.Default.Equals(DotNetCliToolReferences, file.DotNetCliToolReferences) &&
				   IsNewFormat == file.IsNewFormat &&
				   Name == file.Name;
		}

		public class PackageReference
		{
			public string Name { get; set; }

			public string Version { get; set; }

			public override bool Equals(object obj)
			{
				var reference = obj as PackageReference;
				return reference != null &&
					   Name == reference.Name &&
					   Version == reference.Version;
			}

			/// <summary>
			/// Для отладки
			/// </summary>
			public override string ToString()
			{
				return $"{Name}  v. {Version}";
			}
		}

		public class DotNetCliToolReference : PackageReference
		{ }

		public class ProjectReference
		{
			public string RelativePath { get; set; }

			public override bool Equals(object obj)
			{
				var reference = obj as ProjectReference;
				return reference != null &&
					   RelativePath == reference.RelativePath;
			}

			public override string ToString()
			{
				return RelativePath;
			}
		}
	}
}
