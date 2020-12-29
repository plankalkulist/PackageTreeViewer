using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ACME.PRJ.CodebaseCommons;
using ACME.PRJ.CSharpTools;
using ACME.PRJ.PackageTreeViewer.Model;

namespace ACME.PRJ.PackageTreeViewer.ViewModel
{
	public class MainWindowViewModel : INotifyPropertyChanged
    {
		/// <summary>
		/// Игнорируемые проекты
		/// </summary>
        public static HashSet<string> ProjectsToIgnoreKeys { get; private set; }
            = new HashSet<string>() // пример заполнения
            {
                "GPS" // нет солюшенов C#
			};

		/// <summary>
		/// Игнорируемые репы
		/// </summary>
        public static HashSet<(string, string)> ReposToIgnoreKeys { get; private set; }
            = new HashSet<(string, string)>() // пример заполнения
            {
				("BAZ", "BAZ_doc"), // там нет солюшенов и не предвидется
				("BAZ", "BAZ_service"), // ничего особо интересного
				("UFO", "ufo_qa") // ничего особо интересного
			};

		/// <summary>
		/// Иконки для узлов дерева по типу Data
		/// </summary>
        public static Dictionary<Type, string> NodeDataTypeImageUris
            => new Dictionary<Type, string>
            {
                [typeof(object)] = "/Resources/GoToLastRow_16x.png",
                [typeof(ProjectInfo)] = "/Resources/Folder_16x.png",
                [typeof(RepoInfo)] = "/Resources/PackageFolder_16x.png",
                [typeof(CsSolution)] = "/Resources/FSApplication_16x.png",
                [typeof(CsProjectFile)] = "/Resources/PYWPFApplication_16x.png",
                [typeof(CsProjectFile.PackageReference)] = "/Resources/FrameSelect_16x.png",
                [typeof(CsProjectFile.DotNetCliToolReference)] = "/Resources/FrameSelect_16x.png",
                [typeof(CsProjectFile.ProjectReference)] = "/Resources/FrameSelect_16x.png",
                [typeof(Exception)] = "/Resources/StatusCriticalError_16x.png"
			};

        /// <summary>
        /// Дерево отображаемых проектов (= совокупностей репозиториев, собираемых(build) и разворачиваемых(deploy) вместе) и их содержимого вплоть до ссылок в файлах проектов C# (.csproj)
        /// </summary>
        public ObservableCollection<Node> Tree { get; private set; }

		/// <summary>
		/// Готово ли дерево к работе
		/// </summary>
		public bool IsTreeReady
		{
			get
			{
				return _isTreeReady;
			}
			set
			{
				_isTreeReady = value;
				NotifyPropertyChanged();
			}
		}
		private bool _isTreeReady;

		/// <summary>
		/// Заполнено ли дерево репами
		/// </summary>
		public bool IsTreeLoadedWithRepos
		{
			get => _isTreeLoadedWithRepos;
			set
			{
				_isTreeLoadedWithRepos = value;
				NotifyPropertyChanged();
			}
		}
		private bool _isTreeLoadedWithRepos;

		/// <summary>
		/// Заполнено ли дерево солюшенами
		/// </summary>
		public bool IsTreeLoadedWithSolutions
		{
			get
			{
				if (_isTreeLoadedWithSolutions && !_isTreeLoadedWithRepos)
					throw new InvalidOperationException(nameof(IsTreeLoadedWithSolutions));
				return _isTreeLoadedWithSolutions;
			}
			set
			{
				if (value && !_isTreeLoadedWithRepos)
					throw new InvalidOperationException(nameof(IsTreeLoadedWithSolutions));
				_isTreeLoadedWithSolutions = value;
				NotifyPropertyChanged();
				ProgressBarVisibility = value
					? Visibility.Collapsed
					: Visibility.Visible;
			}
		}
		private bool _isTreeLoadedWithSolutions;

		/// <summary>
		/// Поле фильтра веток
		/// </summary>
		public string BranchFilterField
		{
			get => _branchFilterField;
			set
			{
				_branchFilterField = value;
				RestartFilterBranches(_branchFilterField
					, (repoInfoNode, filteredBranch) => repoInfoNode.BranchChangeTo = filteredBranch);
			}
		}
		private string _branchFilterField;
		private Task _filteringBranchesTask;
		private CancellationTokenSource _filteringBranchesCancellationTokenSource;

		/// <summary>
		/// Режим сравнения веток
		/// </summary>
		public bool IsDiffMode
		{
			get => _isDiffMode;
			set
			{
				_isDiffMode = value;
				DiffVisibility = _isDiffMode
					? Visibility.Visible
					: Visibility.Collapsed;

				Tree.ToggleNodesDiffMode(_isDiffMode);

				if (IsTreeLoadedWithSolutions)
				{
					if (!string.IsNullOrEmpty(_ReferenceFilter))
						RestartFilterReferences();
					else
						Tree.ResetNodesVisibility();
				}

				NotifyPropertyChanged();
			}
		}
		private bool _isDiffMode;

		/// <summary>
		/// Видимость элементов в режиме сравнения веток
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
		/// Поле фильтра веток для сравнения
		/// </summary>
		public string DiffBranchFilterField
		{
			get => _diffBranchFilterField;
			set
			{
				_diffBranchFilterField = value;
				RestartFilterBranches(_diffBranchFilterField
					, (repoInfoNode, filteredBranch) => repoInfoNode.DiffBranchChangeTo = filteredBranch);
			}
		}
		private string _diffBranchFilterField;

		/// <summary>
		/// Поле фильтра ссылок
		/// </summary>
		public string ReferenceFilterField
		{
			get => _referenceFilterField;
			set
			{
				_referenceFilterField = value;
				_selectedReference = null;
				_isWholeWordFilterMatch = false;
				_ReferenceFilter = _referenceFilterField;
			}
		}
		private string _referenceFilterField;

		/// <summary>
		/// Список отфильтрованных ссылок
		/// </summary>
		public ObservableCollection<string> FilteredReferences { get; private set; }
			= new ObservableCollection<string>();

		/// <summary>
		/// Выбранная ссылка в списке <see cref="FilteredReferences"/>
		/// </summary>
		public string SelectedReference
		{
			get => _selectedReference;
			set
			{
				if (value == null)
					return;
				_selectedReference = value;
				_isWholeWordFilterMatch = true;
				_ReferenceFilter = _selectedReference;
			}
		}
		private string _selectedReference;

		/// <summary>
		/// Строка для фильтрации ссылок (вызывается изменением <see cref="ReferenceFilterField"/> или <see cref="SelectedReference"/>)
		/// </summary>
        private string _ReferenceFilter
        {
            get => _referenceFilter;
			set
			{
				_referenceFilter = value;
				RestartFilterReferences();
			}
        }
		private Task _filteringReferencesTask;
		private CancellationTokenSource _filteringReferencesCancellationTokenSource;
		private string _referenceFilter;

		/// <summary>
		/// Строка с инфой о текущем состоянии заполения дерева
		/// </summary>
		public string TreeLoadingProgressText
		{
			get => _treeLoadingProgressText;
			set
			{
				_treeLoadingProgressText = value;
				NotifyPropertyChanged();
			}
		}
		private string _treeLoadingProgressText;

		/// <summary>
		/// Готовность заполнения дерева в процентах
		/// </summary>
		public double TreeLoadingProgressPercentage
		{
			get => _treeLoadingProgressPercentage;
			set
			{
				_treeLoadingProgressPercentage = value;
				NotifyPropertyChanged();
			}
		}
		private double _treeLoadingProgressPercentage;

		/// <summary>
		/// Видимость ProgressBar
		/// </summary>
		public Visibility ProgressBarVisibility
		{
			get => _progressBarVisibility;
			set
			{
				_progressBarVisibility = value;
				NotifyPropertyChanged();
			}
		}
		private Visibility _progressBarVisibility;

		#region Commands
		/// <summary>
		/// Обновление узлов солюшенов
		/// </summary>
		public ICommand UpdateSolutions
		{
			get
			{
				return _updateSolutionsData
						?? (_updateSolutionsData = new RelayCommand(
							p => true, p => UpdateSolutionsData())); // await здесь не нужен!
			}
		}
		private ICommand _updateSolutionsData;
		#endregion

		/// <summary>
		/// Сервис для работы с базой кода
		/// </summary>
		private ICodebaseService _codebaseService { get; set; }

		/// <summary>
		/// Сервис для работы с данными дерева
		/// </summary>
		private TreeDataService _treeDataService { get; set; }

		/// <summary>
		/// Для отслеживания статуса загрузки данных в дерево
		/// </summary>
        private ProgressObserver _treeLoadingObserver;

		/// <summary>
		/// Cовпадение имён ссылок с _referenceFilter целиком/наличие _referenceFilter в имени ссылки
		/// </summary>
		private bool _isWholeWordFilterMatch = false;

		/// <summary>
		/// Фабрика узлов
		/// </summary>
		private NodeFactory _nodeFactory { get; set; }

		/// <summary>
		/// Для обновления интерфейса окна из параллельных потоков
		/// </summary>
        private readonly Dispatcher _currentDispatcher = Dispatcher.CurrentDispatcher;

		/// <summary>
		/// lock ( )
		/// </summary>
		private static readonly object _refFilteringLock = new object();

		/// <summary>
		/// Оповещение окна о изменении значения свойства
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Вызов события <see cref="PropertyChanged"/>
		/// </summary>
		/// <param name="propertyName">Вызывающее свойство</param>
		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

		/// <summary>
		/// .ctor
		/// </summary>
        public MainWindowViewModel()
        {
			_codebaseService = CommonContainer.Resolve<ICodebaseService>();

            _treeDataService = new TreeDataService(_codebaseService);
            Tree = _treeDataService.Tree;

			Init();
		}

		/// <summary>
		/// Получить Uri иконки для узла
		/// </summary>
		private string GetNodeImageUri(Node node)
        {
			if (node.NotSuccess)
				return NodeDataTypeImageUris[typeof(Exception)];

            if (node?.Data == null)
                return string.Empty;

            if (NodeDataTypeImageUris.ContainsKey(node.Data.GetType()))
                return NodeDataTypeImageUris[node.Data.GetType()];

            return NodeDataTypeImageUris[typeof(object)];
        }

		/// <summary>
		/// Инициализация дерева
		/// </summary>
        private void Init()
        {
			IsTreeReady = false;
			IsTreeLoadedWithRepos = false;
			IsTreeLoadedWithSolutions = false;

			// базовая инициализация для всех узлов
            _nodeFactory = new NodeFactory(
                node =>
                {
					node.GetName = n => (n.Data as InfoBase)?.Name
						?? (n.Data as CsProjectFile.PackageReference)?.Name
						?? n.Data?.ToString()
						?? throw new ArgumentException(nameof(Tree));

					node.GetTitle = n => n.Data is CsProjectFile.PackageReference packRef
						? $"{packRef.Name}  ver. {packRef.Version}"
						: n is DiffNode diffN && diffN.DiffState.HasFlag(DiffStates.DataModified)
							? $"{n.Name} *"
							: n.Name;

                    node.GetImageUri = n => GetNodeImageUri(n);
                });

            // строим дерево реп
            _treeDataService.PopulateTreeByRepos(_nodeFactory, ProjectsToIgnoreKeys);
			_treeDataService.UpdateBranchesLists(ReposToIgnoreKeys);


			// помечаем проигнорированные проекты и репы
			Tree.ForEachNode(node =>
			{
				switch (node.Data)
				{
					case ProjectInfo project:
						if (ProjectsToIgnoreKeys.Contains(project.Key))
							node.Tags = new List<object> { "игнор" };
						break;
					case RepoInfo repo:
						if (ReposToIgnoreKeys.Contains((repo.Project.Key, repo.Key)))
							node.Tags = new List<object> { "игнор" };
						break;
				}
			}, null, 1);

			IsDiffMode = false;
			Tree.ToggleNodesDiffMode(IsDiffMode);
			IsTreeLoadedWithRepos = true;
			IsTreeReady = true;
        }

		/// <summary>
		/// Обновление узлов солюшенов
		/// </summary>
		private async Task UpdateSolutionsData()
		{
			int reposToUpdateBranchCount = _treeDataService.CountReposToChangeBranch(ReposToIgnoreKeys);
			if (reposToUpdateBranchCount == 0)
				return;

			// наблюдатель
			_treeLoadingObserver = new ProgressObserver();
			_treeLoadingObserver.DataChanged += (o, e)
				=> _currentDispatcher.Invoke(() =>
				{
					var observerInstance = o as ProgressObserver;
					TreeLoadingProgressText = $"Поиск файлов '*.sln' в {observerInstance.ProgressData}";
				});
			_treeLoadingObserver.StatusChanged += (o, e)
				=> _currentDispatcher.Invoke(() =>
				{
					var observerInstance = o as ProgressObserver;
					if ((string)observerInstance.Status == ProgressObserver.StartedStatus)
						TreeLoadingProgressText = $"Поиск файлов '*.sln' в {observerInstance.ProgressData} начат.";
					else if ((string)observerInstance.Status == ProgressObserver.FinishedStatus)
						TreeLoadingProgressText = $"Поиск файлов '*.sln' в {observerInstance.ProgressData} завершён успешно. Загрузка и парсинг файлов '*.sln' и '*.csproj'...";
					TreeLoadingProgressPercentage = (double)observerInstance.Counter / reposToUpdateBranchCount * 100d;
				});

			try
			{
				IsTreeReady = false;
				IsTreeLoadedWithSolutions = false;

				await _treeDataService.LoadSolutionsToTree(
					_nodeFactory
					, ReposToIgnoreKeys
					, _treeLoadingObserver);

				Tree.ResetNodesVisibility();
				TreeLoadingProgressText = string.Empty;

				var diffReposCount = Tree.ToggleNodesDiffMode(IsDiffMode);
				if (IsDiffMode)
				{
					var diffNodesObserver = new ProgressObserver();
					diffNodesObserver.StatusChanged += (o, e)
						=> _currentDispatcher.Invoke(() =>
						{
							var observerInstance = o as ProgressObserver;
							if ((string)observerInstance.Status == ProgressObserver.StartedStatus)
								TreeLoadingProgressText = $"Сравнение элементов в {observerInstance.ProgressData} начато.";
							else if ((string)observerInstance.Status == ProgressObserver.FinishedStatus)
								TreeLoadingProgressText = $"Сравнение элементов в {observerInstance.ProgressData} завершено успешно.";
							TreeLoadingProgressPercentage = (double)observerInstance.Counter / diffReposCount * 100d;
						});

					//////////////////// для отладки режима сравнения (beta)
					Tree.ForEachNode(node =>
					{
						if (!(node.Data is ProjectInfo || node.Data is RepoInfo))
						{
							if (node.Tags != null)
								node.Tags.Add(TreeDataExtensions.GetBranchByNode(node).Name);
							else
								node.Tags = new List<object> { TreeDataExtensions.GetBranchByNode(node).Name };
						}
					}, null, null, true);
					////////////////////

					Tree.MergeDiffNodes(diffNodesObserver);
				}

				if (!string.IsNullOrEmpty(_ReferenceFilter))
					RestartFilterReferences();
				else
					Tree.ResetNodesVisibility();

				IsTreeLoadedWithSolutions = true;
				IsTreeReady = true;
			}
			catch (Exception e)
			{
				TreeLoadingProgressText = $"Неожиданная ошибка: {e.Message}";
			}
		}

		/// <summary>
		/// Начинаем заново фильтрацию ссылок
		/// </summary>
		private void RestartFilterReferences()
		{
			// если предыдущая фильтрация завершилась / отменена / упала с ошибкой / ещё не было
			if (_filteringReferencesTask == null
				|| _filteringReferencesTask.Status != TaskStatus.Running)
			{
				_filteringReferencesCancellationTokenSource = new CancellationTokenSource();
				_filteringReferencesTask = FilterReferences();
			}
			// если предыдущая фильтрация в процессе и не отменялась
			else if (!_filteringReferencesCancellationTokenSource.IsCancellationRequested)
			{
				_filteringReferencesCancellationTokenSource.Cancel();
				_filteringReferencesTask.ContinueWith(
					t =>
					{
						_filteringReferencesCancellationTokenSource = new CancellationTokenSource();
						_filteringReferencesTask = FilterReferences();
					});
			}
		}

		/// <summary>
		/// Фильтрация ссылок
		/// </summary>
		private async Task FilterReferences()
        {
			await Task.Factory.StartNew(() =>
			{
				lock (_refFilteringLock)
				{
					_currentDispatcher.Invoke(() => FilteredReferences.Clear());

					if (string.IsNullOrEmpty(_referenceFilter))
					{
						Tree.ResetNodesVisibility(_filteringReferencesCancellationTokenSource.Token);
						return;
					}

					// поиск подходящих узлов
					var filteredRefsNodes = Tree.FindNodes(
						node =>
						{
							string name;
							switch (node.Data)
							{
								case CsProjectFile.DotNetCliToolReference cliRefData:
									name = cliRefData.Name;
									break;
								case CsProjectFile.PackageReference packRefData:
									name = packRefData.Name;
									break;
								case CsProjectFile.ProjectReference packRefData:
									return false;
								default:
									return false;
							}

							return FilterHelper.IsMatchSpecial(name, _referenceFilter, true, true);

						}, _filteringReferencesCancellationTokenSource.Token);
					if (_filteringReferencesCancellationTokenSource.Token.IsCancellationRequested)
						return;

					if (!filteredRefsNodes.Any())
						return;

					// скрываем все узлы дерева
					Tree.HideAllNodes(_filteringReferencesCancellationTokenSource.Token);
					if (_filteringReferencesCancellationTokenSource.Token.IsCancellationRequested)
						return;

					// отображаем узлы с ошибкой
					Tree.FindNodes(node => node.NotSuccess, _filteringReferencesCancellationTokenSource.Token)
						.ShowNodes(true, _filteringReferencesCancellationTokenSource.Token);

					// отображаем отфильтрованные узлы
					filteredRefsNodes.ShowNodes(true, _filteringReferencesCancellationTokenSource.Token);
					if (_filteringReferencesCancellationTokenSource.Token.IsCancellationRequested)
						return;

					// обновляем список отфильтрованных ссылок
					var filteredRefsNames = filteredRefsNodes
							.Select(node => (node.Data as CsProjectFile.PackageReference).Name)
							.Distinct()
							.OrderBy(name => name);

					if (_filteringReferencesCancellationTokenSource.Token.IsCancellationRequested)
						return;

					_currentDispatcher.Invoke(
						() =>
						{
							foreach (var refName in filteredRefsNames)
							{
								if (_filteringReferencesCancellationTokenSource.Token.IsCancellationRequested)
									return;
								FilteredReferences.Add(refName);
							}
						});
				}
			});

			// отметка о том, что фильтрация завершена
			_filteringReferencesTask = null;
		}

		/// <summary>
		/// Начинаем заново фильтрацию веток
		/// </summary>
		/// <param name="setFilteredBranch">Действие для сопоставления узла репы и отфильтрованной ветки</param>
		private void RestartFilterBranches(string filterLine, Action<RepoNode, BranchInfo> setFilteredBranch)
		{
			if (setFilteredBranch == null)
				throw new ArgumentNullException(nameof(setFilteredBranch));

			// если предыдущая фильтрация завершилась / отменена / упала с ошибкой / ещё не было
			if (_filteringBranchesTask == null
				|| _filteringBranchesTask.Status != TaskStatus.Running)
			{
				_filteringBranchesCancellationTokenSource = new CancellationTokenSource();
				_filteringBranchesTask = FilterBranches(filterLine, setFilteredBranch);
			}
			// если предыдущая фильтрация в процессе и не отменялась
			else if (!_filteringBranchesCancellationTokenSource.IsCancellationRequested)
			{
				_filteringReferencesCancellationTokenSource.Cancel();
				_filteringBranchesTask.ContinueWith(
					t =>
					{
						_filteringBranchesCancellationTokenSource = new CancellationTokenSource();
						_filteringBranchesTask = FilterBranches(filterLine, setFilteredBranch);
					});
			}
		}

		/// <summary>
		/// Фильтрация бранчей
		/// </summary>
		private async Task FilterBranches(string filterLine, Action<RepoNode, BranchInfo> setFilteredBranch)
		{
			await Task.Factory.StartNew(() =>
			{
				lock (_refFilteringLock)
				{
					if (filterLine == null)
						return;

					var filters = filterLine.Split(';');

					var normFilters = filters.Select(f => f.Trim().ToLower())
						.Where(nf => !string.IsNullOrEmpty(nf));

					if (!normFilters.Any())
						return;

					// установка отфильтрованных бранчей
					_currentDispatcher.Invoke(() =>
						{
							Tree.ForEachNode(node =>
							{
								if (node is RepoNode repoInfoNode
										&& ReposToIgnoreKeys?.Contains((repoInfoNode.RepoInfoData.Project.Key
											, repoInfoNode.RepoInfoData.Key)) != true)
								{
									setFilteredBranch(repoInfoNode
										, repoInfoNode.AllBranches
											.WhereAnyIsMatchSpecial(branch => branch.Name, normFilters, true, true)
											.FirstOrDefault());
								}
							}
							, _filteringBranchesCancellationTokenSource.Token, 1);
						});
				}
			});

			// отметка о том, что фильтрация завершена
			_filteringBranchesTask = null;
		}
	}
}
