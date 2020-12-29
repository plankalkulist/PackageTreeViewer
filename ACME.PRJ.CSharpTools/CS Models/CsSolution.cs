using System;
using System.Collections.Generic;
using System.Linq;

namespace ACME.PRJ.CSharpTools
{
    /// <summary>
    /// Контейнер с инфой о солюшене
    /// </summary>
    public class CsSolution
	{
		public CsSolutionFile Data { get; }

		public IEnumerable<CsProjectFile> ProjectsData { get; }

		public string Name => Data.SolutionFileInfo.Name;

		public CsSolution(CsSolutionFile solutionFileData, IEnumerable<CsProjectFile> projectsData)
		{
			if (solutionFileData == null)
				throw new ArgumentNullException(nameof(solutionFileData));
			if (!solutionFileData.IsValid())
				throw new ArgumentException(nameof(solutionFileData));

			if (projectsData == null)
				throw new ArgumentNullException(nameof(projectsData));
			if (projectsData.Any(p => !p.IsValid()))
				throw new ArgumentException(nameof(projectsData));

			Data = solutionFileData;
			ProjectsData = projectsData;
		}

		public override string ToString()
		{
			return Name;
		}

		public override bool Equals(object obj)
		{
			var solution = obj as CsSolution;
			return solution != null &&
				   Data.Equals(solution.Data);
		}
	}
}
