using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ACME.PRJ.CSharpTools
{
	/// <summary>
	/// Класс со вспомогательными методами парсинга сущностей C#
	/// </summary>
	internal static class CsParsingHelper
	{
		/// <summary>
		/// Идентификатор типа записи о проекте - 'виртуальная папка'
		/// </summary>
		private static string SolutionVirtualFolderGuid => "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

		public static Guid ParseCsSolutionGuid(string slnFileContent)
		{
			const string guidFieldPattern = @"SolutionGuid"
				+ @"\s*=\s*{(?<slnGuid>.*)}";

			var match = Regex.Match(slnFileContent, guidFieldPattern);

			return match.Success ? Guid.Parse(match.Groups["slnGuid"].Value) : Guid.Empty;
		}

		public static IEnumerable<CsProjectFileInfo> ParseCsProjectFileInfos(string slnFileContent)
		{
			const string projectFieldPattern = @"Project\("
				+ "\"(?<projTypeGuid>.*)\"" + @"\)"
				+ "\\s*=\\s*\"(?<projName>.*)\"\\s*,\\s*\"(?<projPath>.*)\"\\s*,\\s*\"(?<projGuid>.*)\"\\s*";

			var matches = Regex.Matches(slnFileContent, projectFieldPattern);

			return matches.Cast<Match>()
					.Where(m => !string.Equals(m.Groups["projTypeGuid"].Value
						, SolutionVirtualFolderGuid
						, StringComparison.InvariantCultureIgnoreCase))
					.Select(match => new CsProjectFileInfo()
					{
						Guid = Guid.Parse(match.Groups["projGuid"].Value),
						RelativeToSolutionPath = match.Groups["projPath"].Value.Replace('\\', '/'),
						Name = match.Groups["projName"].Value.Replace('\\', '/')
					});
		}

		/// <summary>
		/// Возвращает XElement из строки
		/// </summary>
		public static XElement ToXElement(string xmlString, Action<Exception> onError = null)
		{
			try
			{
				return XElement.Parse(xmlString);
			}
			catch (Exception e)
			{
				onError?.Invoke(e);
				throw;
			}
		}

		public static void ParseFieldsFromCsProjectXml(CsProjectFile targetProject, string xmlString, Action<Exception> onError = null)
		{
			if (targetProject == null)
				throw new ArgumentNullException(nameof(targetProject));

			var projXml = CsParsingHelper.ToXElement(xmlString, onError);

			// новый/старый формат
			var isNewFormat = projXml.Attribute("ToolsVersion") == null;
			targetProject.IsNewFormat = isNewFormat;
			if (isNewFormat)
			{

				var propertyGroup = projXml.Elements("PropertyGroup")
					.SelectMany(e => e.Elements());
				targetProject.Version = propertyGroup.FirstOrDefault(e => e.Name.LocalName == "Version")?.Value;
				targetProject.TargetFramework = propertyGroup.FirstOrDefault(e => e.Name.LocalName == "TargetFramework")?.Value;
				targetProject.GeneratePackageOnBuild =  propertyGroup.FirstOrDefault(e => e.Name.LocalName == "GeneratePackageOnBuild")?.Value.Trim().ToLower() == "true";
				targetProject.Authors = propertyGroup.FirstOrDefault(e => e.Name.LocalName == "Authors")?.Value;
				targetProject.Description = propertyGroup.FirstOrDefault(e => e.Name.LocalName == "Description")?.Value;
				targetProject.Company = propertyGroup.FirstOrDefault(e => e.Name.LocalName == "Company")?.Value;
				targetProject.Product = propertyGroup.FirstOrDefault(e => e.Name.LocalName == "Product")?.Value;
				targetProject.AssemblyName = propertyGroup.FirstOrDefault(e => e.Name.LocalName == "AssemblyName")?.Value;
				targetProject.RootNamespace = propertyGroup.FirstOrDefault(e => e.Name.LocalName == "RootNamespace")?.Value;

				targetProject.PackageReferences = projXml.Elements("ItemGroup")
					.SelectMany(e => e.Elements()
						.Where(rf => rf.Name.LocalName == "PackageReference"))
					.Select(pr =>
						new CsProjectFile.PackageReference
						{
							Name = pr.Attribute("Include").Value,
							Version = pr.Attribute("Version")?.Value // может отсутствовать
						});

				targetProject.DotNetCliToolReferences = projXml.Elements("ItemGroup")
					.SelectMany(e => e.Elements()
						.Where(rf => rf.Name.LocalName == "DotNetCliToolReference"))
					.Select(e =>
						new CsProjectFile.DotNetCliToolReference
						{
							Name = e.Attribute("Include").Value,
							Version = e.Attribute("Version").Value
						});

				targetProject.ProjectReferences = projXml.Elements("ItemGroup")
					.SelectMany(e => e.Elements()
						.Where(rf => rf.Name.LocalName == "ProjectReference"))
					.Select(e =>
						new CsProjectFile.ProjectReference
						{
							RelativePath = e.Attribute("Include").Value
						});

			}
			else
			{
				// если формат файла проекта старый

				var propertyGroup = projXml.Elements("PropertyGroup")
					.SelectMany(e => e.Elements());
				targetProject.Version = propertyGroup.FirstOrDefault(e => e.Name.LocalName == "Version")?.Value;
				targetProject.TargetFramework = propertyGroup.FirstOrDefault(e => e.Name.LocalName == "TargetFrameworkVersion")?.Value;
				targetProject.GeneratePackageOnBuild = propertyGroup.FirstOrDefault(e => e.Name.LocalName == "GeneratePackageOnBuild")?.Value.Trim().ToLower() == "true";
				targetProject.Authors = propertyGroup.FirstOrDefault(e => e.Name.LocalName == "Authors")?.Value;
				targetProject.Description = propertyGroup.FirstOrDefault(e => e.Name.LocalName == "Description")?.Value;
				targetProject.Company = propertyGroup.FirstOrDefault(e => e.Name.LocalName == "Company")?.Value;
				targetProject.Product = propertyGroup.FirstOrDefault(e => e.Name.LocalName == "Product")?.Value;
				targetProject.AssemblyName = propertyGroup.FirstOrDefault(e => e.Name.LocalName == "AssemblyName")?.Value;
				targetProject.RootNamespace = propertyGroup.FirstOrDefault(e => e.Name.LocalName == "RootNamespace")?.Value;

                targetProject.ProjectReferences = projXml.Elements("ItemGroup")
                    .SelectMany(e => e.Elements()
                        .Where(rf => rf.Name.LocalName == "ProjectReference"))
                    .Select(e =>
                        new CsProjectFile.ProjectReference
                        {
                            RelativePath = e.Attribute("Include").Value
                        });
            }
		}

		public static IEnumerable<CsProjectFile.PackageReference> ParsePackageReferencesFromConfig(string packagesConfigContent)
		{
			if (string.IsNullOrEmpty(packagesConfigContent))
				throw new ArgumentNullException(nameof(packagesConfigContent));

			var configXml = ToXElement(packagesConfigContent);

			return configXml.Elements()
				.Select(e =>
					new CsProjectFile.PackageReference
					{
						Name = e.Attribute("id").Value,
						Version = e.Attribute("version").Value
					});
		}
	}
}
