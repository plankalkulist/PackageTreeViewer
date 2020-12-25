using System;
using ACME.PRJ.CodebaseCommons;

namespace ACME.PRJ.CSharpTools
{
	/// <summary>
	/// Инфо о файле проекта решения C# (заполняется по данным из файла решения)
	/// </summary>
	public class CsProjectFileInfo : SourceItemInfo
	{
		public Guid Guid { get; set; }

		public Guid SolutionGuid { get; set; }

		public string RelativeToSolutionPath { get; set; }

		public override bool IsValid()
		{
			return !string.IsNullOrWhiteSpace(RelativeToSolutionPath)
				&& !Guid.Equals(default)
				&& base.IsValid();
		}
	}
}
