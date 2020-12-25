using System.Collections.Generic;
using System.Windows;
using ACME.PRJ.CodebaseCommons;

namespace ACME.PRJ.PackageTreeViewer.Model
{
	/// <summary>
	/// Узел дерева c инфой о репе
	/// </summary>
	public class RepoNode : DiffNode
	{
		/// <summary>
		/// Основные данные о репе
		/// </summary>
		public RepoInfo RepoInfoData => Data as RepoInfo;

		/// <summary>
		/// Правильное отображение имени (учитывая особенности работы Window)
		/// </summary>
		public string ValidViewedName => base.Name.Replace("_", "__");

		/// <summary>
		/// Бранчи
		/// </summary>
		public IEnumerable<BranchInfo> AllBranches { get; set; }

		/// <summary>
		/// Текущий загруженный бранч
		/// </summary>
		public BranchInfo CurrentBranch { get;
			set; }

		/// <summary>
		/// Бранч, на который будет переход
		/// </summary>
		public BranchInfo BranchChangeTo
		{
			get => _branchChangeTo;
			set
			{
				_branchChangeTo = value;
				NotifyPropertyChanged();
			}
		}
		protected BranchInfo _branchChangeTo;

		/// <summary>
		/// Режим сравнения веток
		/// </summary>
		public override bool IsDiffMode
		{
			get => _isDiffMode;
			set
			{
				_isDiffMode = value;
				NotifyPropertyChanged();
				DiffVisibility = _isDiffMode
					? Visibility.Visible
					: Visibility.Collapsed;
			}
		}

		/// <summary>
		/// Видимость веток для сравнения
		/// </summary>
		public Visibility DiffVisibility
		{
			get => _diffVisibility;
			set
			{
				_diffVisibility = value;
				NotifyPropertyChanged();
			}
		}
		protected Visibility _diffVisibility;

		/// <summary>
		/// Текущий загруженный бранч для сравнения
		/// </summary>
		public BranchInfo CurrentDiffBranch { get; set; }

		/// <summary>
		/// Бранч для сравнения, на который будет переход
		/// </summary>
		public BranchInfo DiffBranchChangeTo
		{
			get => _diffBranchChangeTo;
			set
			{
				_diffBranchChangeTo = value;
				NotifyPropertyChanged();
			}
		}
		protected BranchInfo _diffBranchChangeTo;
	}
}
