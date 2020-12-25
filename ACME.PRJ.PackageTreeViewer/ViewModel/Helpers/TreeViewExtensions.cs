using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using ACME.PRJ.PackageTreeViewer.Model;

namespace ACME.PRJ.PackageTreeViewer.ViewModel
{
	/// <summary>
	/// Расширения для работы с отображением узлов дерева
	/// </summary>
	public static class TreeViewExtensions
	{
		/// <summary>
		/// Установить видимость и раскрытость узлов по умолчанию (проект->репа->солюшн)
		/// </summary>
		public static void ResetNodesVisibility(this ObservableCollection<Node> tree, CancellationToken? token = null)
		{
			if (tree == null)
				throw new ArgumentNullException(nameof(tree));

			foreach (var projectNode in tree)
			{
				if (token?.IsCancellationRequested == true)
					return;
				// Project node
				projectNode.Visibility = Visibility.Visible;
				projectNode.IsExpanded = true;

				if (projectNode.Nodes?.Any() != true)
					continue;
				foreach (var repoNode in projectNode.Nodes)
				{
					if (token?.IsCancellationRequested == true)
						return;
					// Repo node
					if (repoNode.IsIgnored)
					{
						repoNode.Visibility = Visibility.Collapsed;
						continue;
					}
					repoNode.Visibility = Visibility.Visible;
					repoNode.IsExpanded = true;

					if (repoNode.Nodes?.Any() != true)
						continue;
					foreach (var solutionNode in repoNode.Nodes)
					{
						if (token?.IsCancellationRequested == true)
							return;
						// Solution node
						if (solutionNode.IsIgnored)
						{
							solutionNode.Visibility = Visibility.Collapsed;
							continue;
						}
						solutionNode.Visibility = Visibility.Visible;
						solutionNode.IsExpanded = false;

						if (solutionNode.Nodes?.Any() != true)
							continue;
						foreach (var csProjectNode in solutionNode.Nodes)
						{
							if (token?.IsCancellationRequested == true)
								return;
							// C# project node
							if (csProjectNode.IsIgnored)
							{
								csProjectNode.Visibility = Visibility.Collapsed;
								continue;
							}
							csProjectNode.Visibility = Visibility.Visible;
							csProjectNode.IsExpanded = false;
							

							if (csProjectNode.Nodes?.Any() != true)
								continue;
							foreach (var refsGroupNode in csProjectNode.Nodes)
							{
								if (token?.IsCancellationRequested == true)
									return;
								// C# project reference group node
								if (refsGroupNode.IsIgnored || refsGroupNode.Nodes?.Any() != true)
								{
									refsGroupNode.Visibility = Visibility.Collapsed;
									continue;
								}
								refsGroupNode.Visibility = Visibility.Visible;
								refsGroupNode.IsExpanded = true;
								
								foreach (var referenceNode in refsGroupNode.Nodes)
								{
									if (token?.IsCancellationRequested == true)
										return;
									// C# project reference node
									if (referenceNode.IsIgnored)
									{
										referenceNode.Visibility = Visibility.Collapsed;
										continue;
									}
									referenceNode.Visibility = Visibility.Visible;
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Скрываем все узлы
		/// </summary>
		public static void HideAllNodes(this ObservableCollection<Node> tree, CancellationToken? token = null)
		{
			tree.ForEachNode(node =>
			{
				node.Visibility = Visibility.Collapsed;
			}
			, token);
		}

		/// <summary>
		/// Показать указанные узлы
		/// </summary>
		/// <param name="nodes">Нужные узлы</param>
		/// <param name="expandParents">Раскрыть родительские узлы</param>
		public static void ShowNodes(this IEnumerable<Node> nodes, bool expandParents = true, CancellationToken? token = null)
		{
			if (nodes == null)
				throw new ArgumentNullException(nameof(nodes));

			foreach (var node in nodes)
			{
				node.Visibility = Visibility.Visible;

				var parent = node.Parent;
				while (parent != null)
				{
					if (token?.IsCancellationRequested == true)
						return;

					parent.Visibility = Visibility.Visible;
					if (expandParents)
						parent.IsExpanded = true;
					parent = parent.Parent;
				}
			}
		}
	}
}
