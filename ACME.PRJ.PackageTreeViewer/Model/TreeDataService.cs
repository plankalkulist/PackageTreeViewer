using ACME.PRJ.CodebaseCommons;
using ACME.PRJ.CSharpTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ACME.PRJ.PackageTreeViewer.Model
{
	/// <summary>
	/// Сервис для работы с данными узлов дерева
	/// </summary>
	public class TreeDataService
	{
		/// <summary>
		/// Дерево
		/// </summary>
		public ObservableCollection<Node> Tree { get; private set; }

		private static readonly BranchInfo _noBranchSelected
			= new BranchInfo
			{
				Key = null,
				Name = "--"
			};

		/// <summary>
		/// Сервис для работы с базой кода
		/// </summary>
		private ICodebaseService _codebaseService { get; set; }

		/// <summary>
		/// .ctor
		/// </summary>
		public TreeDataService(ICodebaseService codebaseService)
		{
			_codebaseService = codebaseService ?? throw new ArgumentNullException(nameof(codebaseService));
			Tree = new ObservableCollection<Node>();
		}

		/// <summary>
		/// Заполнение дерева репозиториями
		/// </summary>
		public void PopulateTreeByRepos(NodeFactory nodeFactory, HashSet<string> projectsToIgnoreKeys = null)
		{
			if (nodeFactory == null)
				throw new ArgumentNullException(nameof(nodeFactory));

			var projects = _codebaseService.GetProjects();

			if (projectsToIgnoreKeys?.Any() == true)
				projects = projects.Where(p => !projectsToIgnoreKeys.Contains(p.Key));

			Tree.Clear();
			foreach (var project in projects)
				Tree.Add(nodeFactory.CreateNode(
					// Project node
					project, projectNode =>
					{
						projectNode.Parent = null;
						projectNode.IsExpanded = true;
						projectNode.Nodes = new ObservableCollection<Node>(
							_codebaseService.GetRepos(project)
							.Select(repo => nodeFactory.CreateNode<RepoNode>(
							// Repo node
							repo, repoNode =>
							{
								repoNode.Parent = projectNode;
								repoNode.IsExpanded = true;
								repoNode.CompareMode = CompareMode.ByChildren;
							})));
					}));
		}

		/// <summary>
		/// Загрузка бранчей (перед каждой выборкой репа-бранч)
		/// </summary>
		public void UpdateBranchesLists(HashSet<(string, string)> reposToIgnoreKeys = null)
		{
			foreach (var projectNode in Tree)
			{
				if (projectNode.Nodes?.Any() != true)
					continue;

				foreach (var repoNode in projectNode.Nodes)
				{
					var repoInfoNode = repoNode as RepoNode
						?? throw new InvalidOperationException(nameof(Tree));

					if (reposToIgnoreKeys?.Contains((repoInfoNode.RepoInfoData.Project.Key
							, repoInfoNode.RepoInfoData.Key)) == true)
						continue;

					var repo = repoInfoNode.RepoInfoData;
					var branches = _codebaseService.GetBranches(repo);
					repoInfoNode.AllBranches = new[] { _noBranchSelected }.Concat(branches.OrderBy(branch => branch.Name));
					//repoInfoNode.BranchChangeTo = branches.FirstOrDefault(b => b.IsDefault); // ComboBox затирает свойства repoNode при первом отображении
				}
			}
		}

		/// <summary>
		/// Для скольких реп выбран новый бранч
		/// </summary>
		public int CountReposToChangeBranch(HashSet<(string, string)> reposToIgnoreKeys = null)
		{
			int reposToUpdateBranchCount = 0;
			foreach (var projectNode in Tree)
			{
				if (projectNode.Nodes?.Any() != true)
					continue;

				foreach (var repoNode in projectNode.Nodes)
				{
					var repoInfoNode = repoNode as RepoNode
						?? throw new ArgumentException(nameof(Tree));

					if (reposToIgnoreKeys?.Contains((repoInfoNode.RepoInfoData.Project.Key
							, repoInfoNode.RepoInfoData.Key)) == true)
						continue;

					// будем загружать солюшены текущей ветки, если выбор изменился
					if (!(repoInfoNode.BranchChangeTo == null || repoInfoNode.BranchChangeTo == _noBranchSelected
							|| (repoInfoNode.CurrentBranch != null
								&& repoInfoNode.CurrentBranch.Key == repoInfoNode.BranchChangeTo.Key)))
					{
						reposToUpdateBranchCount++;
					}

					// в режиме сравнения веток будем загружать также солюшены сравниваемой ветки, если они ещё не загружены и ветка к сравнению не равна текущей
					if (repoInfoNode.IsDiffMode
						&& repoInfoNode.BranchChangeTo != repoInfoNode.DiffBranchChangeTo
						&& !(repoInfoNode.DiffBranchChangeTo == null || repoInfoNode.DiffBranchChangeTo == _noBranchSelected
							|| (repoInfoNode.CurrentDiffBranch != null
								&& repoInfoNode.CurrentDiffBranch.Key == repoInfoNode.DiffBranchChangeTo.Key)))
					{
						reposToUpdateBranchCount++;
					}
				}
			}

			return reposToUpdateBranchCount;
		}

		/// <summary>
		/// Загрузка солюшенов в дерево
		/// </summary>
		public async Task LoadSolutionsToTree(
			NodeFactory nodeFactory
			, HashSet<(string, string)> reposToIgnoreKeys = null
			, ProgressObserver progressObserver = null)
		{
			if (nodeFactory == null)
				throw new ArgumentNullException(nameof(nodeFactory));

			await Task.Factory.StartNew(() =>
			{
				foreach (var projectNode in Tree)
				{
					foreach (var repoNode in projectNode.Nodes)
					{
						var repoInfoNode = repoNode as RepoNode
							?? throw new InvalidOperationException(nameof(Tree));

						if (reposToIgnoreKeys?.Contains((repoInfoNode.RepoInfoData.Project.Key
								, repoInfoNode.RepoInfoData.Key)) == true)
							continue;

						IEnumerable<Result<CsSolution>> solutions = null;
						int counterStep = 1; // шаг счётчика прогресса загрузки

						// загружаем солюшены текущей ветки, если выбор изменился
						bool isCurrentBranchChanged = !(repoInfoNode.BranchChangeTo == null
								|| repoInfoNode.BranchChangeTo == _noBranchSelected
								|| (repoInfoNode.CurrentBranch != null
									&& repoInfoNode.CurrentBranch.Key == repoInfoNode.BranchChangeTo.Key));
						if (isCurrentBranchChanged)
						{
							solutions = CsRepoHelper.GetAllSolutions(_codebaseService, repoInfoNode.BranchChangeTo, progressObserver);
						}

						// в режиме сравнения веток подгружаем также солюшены сравниваемой ветки, если они ещё не загружены и сравниваемая ветка не равна текущей
						IEnumerable<Result<CsSolution>> diffBranchSolutions = null;
						bool isDiffBranchChanged = repoInfoNode.IsDiffMode
							&& repoInfoNode.BranchChangeTo != repoInfoNode.DiffBranchChangeTo
							&& !(repoInfoNode.DiffBranchChangeTo == null
								|| repoInfoNode.DiffBranchChangeTo == _noBranchSelected
								|| (repoInfoNode.CurrentDiffBranch != null
									&& repoInfoNode.CurrentDiffBranch.Key == repoInfoNode.DiffBranchChangeTo.Key));
						if (isDiffBranchChanged)
						{
							diffBranchSolutions = CsRepoHelper.GetAllSolutions(_codebaseService, repoInfoNode.DiffBranchChangeTo, progressObserver);
							if (solutions != null)
							{
								solutions = solutions.Concat(diffBranchSolutions);
								counterStep = 2;
							}
							else
							{
								solutions = diffBranchSolutions;
							}
						}

						// если никакие солюшены не загружались, пропускаем репу
						if (!isCurrentBranchChanged && !isDiffBranchChanged)
							continue;

						repoNode.Nodes = new ObservableCollection<Node>(
							solutions
							.OrderBy(solutionLoadingResult => solutionLoadingResult.Data?.Name)
							.Select(solutionLoadingResult => solutionLoadingResult.IsSuccessful
							// Solution node
							? nodeFactory.CreateNode<DiffNode>(
								solutionLoadingResult.Data, solutionNode =>
								{
									solutionNode.Parent = repoNode;
									solutionNode.CompareMode = CompareMode.ByChildren;
									solutionNode.DiffState = DiffStates.NotSolvedYet;
									solutionNode.Nodes = new ObservableCollection<Node>(
										solutionLoadingResult.Data.ProjectsData
										.OrderBy(csProjectFile => csProjectFile.Name)
										.Select(csProject => nodeFactory.CreateNode<DiffNode>(
										// C# project node
										csProject, csProjectNode =>
										{
											csProjectNode.Parent = solutionNode;
											csProjectNode.CompareMode = CompareMode.ByData | CompareMode.ByChildren;
											csProjectNode.DiffState = DiffStates.NotSolvedYet;
											csProjectNode.Nodes = new ObservableCollection<Node>(
												new[]
												{
													// C# project Package references group node
													nodeFactory.CreateNode(
														"Package references:", packageReferencesNode =>
														{
															packageReferencesNode.Parent = csProjectNode;
															//packageReferencesNode.CompareMode = CompareMode.ByChildren;
															//packageReferencesNode.DiffState = DiffStates.NotSolvedYet;
															if (csProject.PackageReferences?.Any() == true)
																packageReferencesNode.Nodes = new ObservableCollection<Node>(
																	csProject.PackageReferences
																	.OrderBy(reference => reference.Name)
																	.Select(reference => nodeFactory.CreateNode<DiffNode>(
																	// C# project Package reference node
																	reference, referenceNode =>
																	{
																		referenceNode.Parent = packageReferencesNode;
																		referenceNode.CompareMode = CompareMode.ByData | CompareMode.DontMerge;
																		referenceNode.DiffState = DiffStates.NotSolvedYet;
																	})));
														}),
													// C# project Dot Net CliTool references group node
													nodeFactory.CreateNode(
														"Dot Net CliTool references:", dotNetCliToolReferencesNode =>
														{
															dotNetCliToolReferencesNode.Parent = csProjectNode;
															//dotNetCliToolReferencesNode.CompareMode = CompareMode.ByChildren;
															//dotNetCliToolReferencesNode.DiffState = DiffStates.NotSolvedYet;
															if (csProject.DotNetCliToolReferences?.Any() == true)
																dotNetCliToolReferencesNode.Nodes = new ObservableCollection<Node>(
																	csProject.DotNetCliToolReferences
																	.OrderBy(reference => reference.Name)
																	.Select(reference => nodeFactory.CreateNode<DiffNode>(
																	// C# project Dot Net CliTool reference node
																	reference, referenceNode =>
																	{
																		referenceNode.Parent = dotNetCliToolReferencesNode;
																		referenceNode.CompareMode = CompareMode.ByData | CompareMode.DontMerge;
																		referenceNode.DiffState = DiffStates.NotSolvedYet;
																	})));
														}),
													// C# project Local project references group node
													nodeFactory.CreateNode(
														"Local project references:", projectReferencesNode =>
														{
															projectReferencesNode.Parent = csProjectNode;
															//projectReferencesNode.CompareMode = CompareMode.ByChildren;
															//projectReferencesNode.DiffState = DiffStates.NotSolvedYet;
															if (csProject.ProjectReferences?.Any() == true)
																projectReferencesNode.Nodes = new ObservableCollection<Node>(
																	csProject.ProjectReferences
																	.OrderBy(reference => reference.RelativePath)
																	.Select(reference => nodeFactory.CreateNode<DiffNode>(
																	// C# project Local project reference node
																	reference, referenceNode =>
																	{
																		referenceNode.Parent = projectReferencesNode;
																		referenceNode.CompareMode = CompareMode.ByName | CompareMode.DontMerge;
																		referenceNode.DiffState = DiffStates.NotSolvedYet;
																	})));
														}),
												});
										})));
								})
							: nodeFactory.CreateNode(solutionLoadingResult.Tag, solutionLoadingResult.ErrorMessage)));

						// переход на ветку(-ки)
						if (isCurrentBranchChanged)
							repoInfoNode.CurrentBranch = repoInfoNode.BranchChangeTo;
						if (isDiffBranchChanged)
							repoInfoNode.CurrentDiffBranch = repoInfoNode.DiffBranchChangeTo;

						// увеличиваем счётчик прогресса загрузки
						if (progressObserver != null)
							progressObserver.Counter += counterStep;
					}
				}
			});
		}
	}
}
