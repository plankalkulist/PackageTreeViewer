using System;
using System.Windows.Media;

namespace ACME.PRJ.PackageTreeViewer.Model
{
	/// <summary>
	/// Узел, участвующий в сравнении
	/// </summary>
	public class DiffNode : Node
	{
		/// <summary>
		/// Режим сравнения
		/// </summary>
		public virtual bool IsDiffMode
		{
			get => _isDiffMode;
			set
			{
				_isDiffMode = value;
				NotifyPropertyChanged();

				if (_isDiffMode)
					NotifyPropertyChanged(nameof(DiffState));
			}
		}
		protected bool _isDiffMode;

		/// <summary>
		/// Это исходная версия
		/// </summary>
		public virtual bool IsOrigin { get; set; }

		/// <summary>
		/// Изменённые данные
		/// </summary>
		public virtual object DataDiff { get; set; }

		public virtual CompareMode CompareMode { get; set; }

		/// <summary>
		/// Статус различия
		/// </summary>
		public virtual DiffStates DiffState
		{
			get =>  _isDiffMode ? _diffState : DiffStates.TheSame;
			set
			{
				_diffState = value;

				if (_isDiffMode)
					NotifyPropertyChanged();
			}
		}
		protected DiffStates _diffState;
	}

	/// <summary>
	/// Как сравнивать
	/// </summary>
	[Flags]
	public enum CompareMode
	{
		/// <summary>
		/// По имени
		/// </summary>
		ByName = 0,

		/// <summary>
		/// По данным узла
		/// </summary>
		ByData = 0b_01,

		/// <summary>
		/// По списку дочерних узлов
		/// </summary>
		ByChildren = 0b_10,

		//DoMerge = 0b_00_00,

		/// <summary>
		/// Не сливать с другими узлами
		/// </summary>
		DontMerge = 0b_01_00
	}

	/// <summary>
	/// Статусы различия
	/// </summary>
	[Flags]
	public enum DiffStates
	{
		/// <summary>
		/// Тот же
		/// </summary>
		TheSame = 0,

		/// <summary>
		/// Появился (отсутсвует в версии origin, присутсвует в версии toCompare)
		/// </summary>
		Appeared = 0b_01,

		/// <summary>
		/// Пропал
		/// </summary>
		Disappeared = 0b_10,

		/// <summary>
		/// Изменились данные узла
		/// </summary>
		DataModified = 0b_01_00,

		/// <summary>
		/// Есть изменения в списке дочерних узлов (или в самих дочерних узлах)
		/// </summary>
		ChildrenModified = 0b_10_00,

		//ChildrenValuesTheSame = 0b_00_00_00,
		//ProbablyChildrenValuesModified = 0b_01_00_00

		/// <summary>
		/// Сравнение не проводилось
		/// </summary>
		NotSolvedYet = 0b_11_00_00,

		/// <summary>
		/// Какой-то из предков изменился, сравнение на этом уровне не проводилось
		/// </summary>
		ByParent = 0b_11_00_00_00,

		/// <summary>
		/// Какой-то из предков появился
		/// </summary>
		ParentAppeared = ByParent | Appeared,

		/// <summary>
		/// Какой-то из предков пропал
		/// </summary>
		ParentDisappeared = ByParent | Disappeared,
	}

	/// <summary>
	/// Результат сравнения списка узлов
	/// </summary>
	[Flags]
	public enum DiffResult
	{
		/// <summary>
		/// Нет изменений
		/// </summary>
		AllTheSame = 0,

		/// <summary>
		/// Есть новые
		/// </summary>
		ThereAreAppeared = 0b_01,

		/// <summary>
		/// Есть пропавшие
		/// </summary>
		ThereAreDisappeared = 0b_10,

		/// <summary>
		/// Есть изменённые (по данным/по списку дочерних узлов/имет изменённые дочерние узлы)
		/// </summary>
		ThereAreModified = 0b_01_00,
	}
}
