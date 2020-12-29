using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using ACME.PRJ.CodebaseCommons;
using ACME.PRJ.CSharpTools;

namespace ACME.PRJ.PackageTreeViewer.Model
{
	public static class TreeDataExtensions
	{
		/// <summary>
		/// Рекурсивный поиск узлов среди дочерних указанного
		/// </summary>
		/// <param name="condition">Предикат поиска</param>
		/// <returns></returns>
		public static IEnumerable<Node> FindNodes(this IEnumerable<Node> nodes
			, Func<Node, bool> condition, CancellationToken? token = null, int? maxDeepLevel = null)
		{
			return FindNodes(nodes, 0, condition, token, maxDeepLevel);
		}

		/// <summary>
		/// Рекурсивный поиск узлов
		/// </summary>
		private static IEnumerable<Node> FindNodes(IEnumerable<Node> nodes, int currentLevel
			, Func<Node, bool> condition, CancellationToken? token = null, int? maxDeepLevel = null)
		{
			if (nodes == null)
				throw new ArgumentNullException(nameof(nodes));
			if (condition == null)
				throw new ArgumentNullException(nameof(condition));
			if (currentLevel < 0)
				throw new ArgumentException(nameof(currentLevel));
			if (maxDeepLevel.HasValue && maxDeepLevel < 0)
				throw new ArgumentException(nameof(maxDeepLevel));

			foreach (var node in nodes)
			{
				if (token?.IsCancellationRequested == true)
					yield break;

				if (node.IsIgnored)
					continue;

				if (condition(node))
					yield return node;

				if (node.Nodes?.Any() == true
					&& (!maxDeepLevel.HasValue
						|| currentLevel < maxDeepLevel))
					foreach (var childNode in FindNodes(node.Nodes, currentLevel + 1, condition, token, maxDeepLevel))
						yield return childNode;
			}
		}

		/// <summary>
		/// Рекурсивный foreach
		/// </summary>
		/// <param name="action">Действие</param>
		public static void ForEachNode(this IEnumerable<Node> nodes, Action<Node> action
			, CancellationToken? token = null, int? maxDeepLevel = null, bool includeIgnored = false)
		{
			ForEachNode(nodes, action, null, 0, token, maxDeepLevel, includeIgnored);
		}

		/// <summary>
		/// Рекурсивный foreach
		/// </summary>
		/// <param name="action">Действие</param>
		public static void ForEachNode(this IEnumerable<Node> nodes, Func<int, Node, bool> actionAndEscapeCondition
			, CancellationToken? token = null, int? maxDeepLevel = null, bool includeIgnored = false)
		{
			ForEachNode(nodes, null, actionAndEscapeCondition, 0, token, maxDeepLevel, includeIgnored);
		}

		/// <summary>
		/// Рекурсивный foreach
		/// </summary>
		private static void ForEachNode(IEnumerable<Node> nodes, Action<Node> action, Func<int, Node, bool> actionAndEscapeCondition
			, int currentLevel, CancellationToken? token = null, int? maxDeepLevel = null, bool includeIgnored = false)
		{
			if (nodes == null)
				throw new ArgumentNullException(nameof(nodes));
			if (action == null && actionAndEscapeCondition == null)
				throw new ArgumentNullException(nameof(action));
			if (currentLevel < 0)
				throw new ArgumentException(nameof(currentLevel));
			if (maxDeepLevel.HasValue && maxDeepLevel < 0)
				throw new ArgumentException(nameof(maxDeepLevel));

			foreach (var node in nodes)
			{
				if (token?.IsCancellationRequested == true)
					return;

				if ((node.IsIgnored && !includeIgnored) || actionAndEscapeCondition?.Invoke(currentLevel, node) == true)
					continue;

				action?.Invoke(node);

				if (node.Nodes?.Any() == true
					&& (!maxDeepLevel.HasValue
						|| currentLevel < maxDeepLevel))
					ForEachNode(node.Nodes, action, actionAndEscapeCondition, currentLevel + 1, token, maxDeepLevel, includeIgnored);
			}
		}

		/// <summary>
		/// Переключить режим сравнения веток для всех узлов
		/// </summary>
		public static int ToggleNodesDiffMode(this ObservableCollection<Node> tree, bool isDiffMode, CancellationToken? token = null)
		{
			int diffRepoNodesCount = 0; // ??
			RepoNode currentRepoNode = null;

			tree.ForEachNode((level, node) =>
			{
				if (level < 1)
					return false;

				if (node is DiffNode diffNode)
				{
					diffNode.IsDiffMode = isDiffMode;
					if (node is RepoNode repoNode)
					{
						currentRepoNode = repoNode;
						diffRepoNodesCount++;
					}
					else if (level == 2) // это узел солюшена
					{
						var isOrigin = (node.Data as CsSolution).Data.SolutionFileInfo.Branch == currentRepoNode.CurrentBranch;
						node.IsIgnored = isDiffMode
							? isOrigin && !diffNode.DiffState.HasFlag(DiffStates.Disappeared)
							: !isOrigin;
					}
				}

				return false;
			}
			, token, null, true);

			return diffRepoNodesCount;
		}

		/// <summary>
		/// Определение ветки, к которой относится узел
		/// </summary>
		public static BranchInfo GetBranchByNode(Node node)
		{
			if (node == null)
				throw new ArgumentNullException(nameof(node));

			do
			{
				switch (node.Data)
				{
					case SourceItemInfo sourceItemInfo:
						return sourceItemInfo.Branch;
					case CsProjectFile projectFile:
						return projectFile.ProjectFileInfo.Branch;
					case CsSolution solution:
						return solution.Data.SolutionFileInfo.Branch;
				}

				node = node.Parent;
			} while (node != null);

			throw new ArgumentException(nameof(node));
		}

		/// <summary>
		/// Рекурсивное сравнение одноимённых узлов, слияние одинаковых узлов в один (beta)
		/// </summary>
		public static void MergeDiffNodes(this ObservableCollection<Node> tree
			, ProgressObserver progressObserver = null, CancellationToken? token = null)
		{
			foreach (var projNode in tree)
			{
				foreach (var repoNode in projNode.Nodes)
				{
					if (repoNode is RepoNode repoInfoNode
							&& repoInfoNode.IsDiffMode
							&& repoInfoNode.CurrentBranch != null
							&& repoInfoNode.CurrentBranch != repoInfoNode.CurrentDiffBranch)
					{
						if (progressObserver != null)
						{
							progressObserver.ProgressData = $"[{repoInfoNode.RepoInfoData.Project.Name}/{repoInfoNode.Name}]";
							progressObserver.Status = ProgressObserver.StartedStatus;
						}

						// ожидается что repoInfoNode.Nodes уже отсортированы по имени
						MergeDiffNodes(repoInfoNode.Nodes, node => GetBranchByNode(node).Equals(repoInfoNode.CurrentBranch));

						if (progressObserver != null)
						{
							progressObserver.ProgressData = $"[{repoInfoNode.RepoInfoData.Project.Name}/{repoInfoNode.Name}]";
							progressObserver.Status = ProgressObserver.FinishedStatus;
							progressObserver.Counter++;
						}
					}
				}
			}
		}

		/// <summary>
		/// Рекурсивное сравнение одноимённых узлов, слияние одинаковых узлов в один (beta)
		/// </summary>
		/// <param name="orderedNodes">Список сравниваемых узлов (ожидается что узлы в нём уже отсортированы по имени)</param>
		/// <param name="isOrigin">Функция проверки узла на версию (origin/toCompare)</param>
		/// <param name="token">Токен отмены</param>
		private static DiffResult MergeDiffNodes(IEnumerable<Node> orderedNodes, Func<Node, bool> isOrigin, CancellationToken? token = null)
		{
			var result = DiffResult.AllTheSame;

			if (orderedNodes?.Any() != true)
				return result;

			var nodesList = orderedNodes.ToList(); // TODO обойтись без ToList
			for (int i = 1; i < nodesList.Count; i++)
			{
				var prevNode = nodesList[i - 1];
				var prevDiffNode = prevNode as DiffNode;
				//if (prevDiffNode?.IsDiffMode == false)
				//	continue; // по умолчанию сравниваем все узлы, для которых явно не выключен режим сравнения

				var currNode = nodesList[i];
				var isPrevNodeOrigin = isOrigin(prevNode);

				if (prevNode.Name == "ACME.Logging.sln") ///////////////////////////////////////
					prevNode = prevNode;

				// сравнение по имени
				if (prevNode.Name != currNode.Name)
				{
					var prevNodeDiffState = isPrevNodeOrigin
							? DiffStates.Disappeared
							: DiffStates.Appeared;

					MarkNodeAndChildren(prevNode, prevNodeDiffState, isPrevNodeOrigin, token);

					result |= (DiffResult)prevNodeDiffState;
					continue;
				}

				var currDiffNode = currNode as DiffNode;
				// проверка на валидность
				if ((prevDiffNode == null) != (currDiffNode == null)
					|| (prevDiffNode != null
						&& prevDiffNode.CompareMode != currDiffNode.CompareMode))
					throw new ArgumentException($"{prevNode.Parent}");

				var originNode = isPrevNodeOrigin ? prevNode : currNode; // узел исходной версии
				var toCompareNode = isPrevNodeOrigin ? currNode : prevNode; // узел версии к сравнению
				originNode.IsIgnored = false;
				toCompareNode.IsIgnored = false;

				bool isDiffNodes = prevDiffNode != null; // оба тек. сравниваемых узла is DiffNode
				var originDiffNode = originNode as DiffNode;
				var toCompareDiffNode = toCompareNode as DiffNode;
				if (isDiffNodes)
				{
					originDiffNode.IsOrigin = true;
					prevDiffNode.DiffState = DiffStates.TheSame;
					currDiffNode.DiffState = DiffStates.TheSame;
				}

				// сравнение узлов по Data
				bool? isDataEqual = null;
				if (prevDiffNode?.CompareMode.HasFlag(CompareMode.ByData) == true)
				{
					isDataEqual = prevNode.Data.Equals(currNode.Data);

					if (isDataEqual == false)
					{
						originDiffNode.DiffState = DiffStates.DataModified;
						toCompareDiffNode.DiffState = DiffStates.DataModified;
						result |= DiffResult.ThereAreModified;
					}
				}

				// сравнение по дочерним узлам (по результату слияния дочерних узлов prevNode и currNode)
				bool? areChildrenEqual = null;
				IEnumerable<Node> mergedChildren = null;

				if (prevDiffNode?.CompareMode.HasFlag(CompareMode.ByChildren) != false)
				{
					// подготовка к сравнению и слиянию дочерних узлов
					if (prevNode.Nodes != null && currNode.Nodes != null)
					{
						mergedChildren = prevNode.Nodes.Concat(currNode.Nodes);
						// если у дочерних узлов слияние выключено - сортируем по имени и версии
						if (mergedChildren.Any(node => (node as DiffNode)?.CompareMode.HasFlag(CompareMode.DontMerge) == true))
						{
							mergedChildren = mergedChildren
								.OrderBy(node => node.Name)
								.ThenByDescending(isOrigin);
						}
						else
						{
							mergedChildren = mergedChildren
								.OrderBy(node => node.Name);
						}
					}
					else
					{
						// ожидается, что все дочерние узлы prevNode (currNode) относятся к одной версии
						mergedChildren = prevNode.Nodes ?? currNode.Nodes;
					}

					areChildrenEqual = mergedChildren == null ? true
						: MergeDiffNodes(mergedChildren, isOrigin, token) == DiffResult.AllTheSame;

					if (areChildrenEqual == false)
					{
						if (isDiffNodes)
						{
							originDiffNode.DiffState |= DiffStates.ChildrenModified;
							toCompareDiffNode.DiffState |= DiffStates.ChildrenModified;
						}
						result |= DiffResult.ThereAreModified;
					}
				}

				// слияние - результат записывается в узел версии к сравнению,
				// узел исходной версии помечается как игнорируемый (для оперативного вкл./выключ. режима сравнения)
				if (prevDiffNode?.CompareMode.HasFlag(CompareMode.DontMerge) != true)
				{
					if (isDataEqual == false)
					{
						toCompareDiffNode.DataDiff = toCompareNode.Data;
						toCompareNode.Data = originNode.Data;
					}

					if (areChildrenEqual == false)
					{
						toCompareNode.Nodes = new ObservableCollection<Node>(mergedChildren);
					}

					// вынести в View?
					originNode.IsIgnored = true;
					toCompareNode.IsIgnored = false;
				}
				else
				{
					if (isDataEqual == false || areChildrenEqual == false)
					{
						MarkNodeAndChildren(originDiffNode, DiffStates.Disappeared, true, token);
						MarkNodeAndChildren(toCompareDiffNode, DiffStates.Appeared, false, token);

						// вынести в View?
						originNode.IsIgnored = false;
						toCompareNode.IsIgnored = false;
					}
					else
					{
						// вынести в View?
						originNode.IsIgnored = true;
						toCompareNode.IsIgnored = false;
					}
				}

				i++; // переход к следующей паре узлов
			}

			// последний узел может быть не обработан, но он точно без пары
			var lastOrderedNode = nodesList.LastOrDefault();
			if (lastOrderedNode is DiffNode lastOrderedDiffNode)
			{
				if (lastOrderedDiffNode.DiffState == DiffStates.NotSolvedYet)
				{
					var isLastOrderedNodeOrigin = isOrigin(lastOrderedDiffNode);
					var lastOrderedNodeDiffState = isLastOrderedNodeOrigin
						? DiffStates.Disappeared
						: DiffStates.Appeared;

					MarkNodeAndChildren(lastOrderedDiffNode, lastOrderedNodeDiffState, isLastOrderedNodeOrigin);
				}
			}

			return result;
		}

		/// <summary>
		/// Помечаем указанный узел Appeared/Disappeared и все дочерние, что их предок Appeared/Disappeared (beta)
		/// </summary>
		private static void MarkNodeAndChildren(Node node, DiffStates newDiffState, bool isNodeOrigin, CancellationToken? token = null)
		{
			if (node is DiffNode diffNode)
			{
				diffNode.DiffState = newDiffState;
				diffNode.IsOrigin = isNodeOrigin;
			}

			if (node.Nodes != null)
				node.Nodes.ForEachNode(child =>
				{
					if (child is DiffNode diffChild)
					{
						diffChild.DiffState = DiffStates.ByParent | newDiffState;
						diffChild.IsOrigin = isNodeOrigin;
					}
				}, token);
		}
	}
}
