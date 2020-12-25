using System;
using System.Collections.Generic;

namespace ACME.PRJ.CodebaseCommons
{
	/// <summary>
	/// Класс со вспомогательными методами для путей
	/// </summary>
	public static class PathHelper
	{
		/// <summary>
		/// Соединяет пути с учётом относительной части ("..\..\")
		/// </summary>
		public static string CombinePaths(string path1, string path2)
		{
			if (string.IsNullOrWhiteSpace(path1))
				return path2;

			if (string.IsNullOrWhiteSpace(path2))
				return path1;

			char catalogSeparator;

			if (path1.Contains("/") || (!path1.Contains("/") && !path1.Contains("\\")))
			{
				if (path1.Contains("\\"))
					throw new ArgumentException(nameof(path1));

				path2 = path2.Replace('\\', '/');
				catalogSeparator = '/';
			}
			else
			{
				path2 = path2.Replace('/', '\\');
				catalogSeparator = '\\';
			}

			return CombinePaths(path1, path2, catalogSeparator);
		}

		/// <summary>
		/// Соединяет пути с учётом относительной части ("..\..\", ".\")
		/// </summary>
		public static string CombinePaths(string path1, string path2, char catalogSeparator)
		{
			var normPath1 = NormalizePath(path1, catalogSeparator);
			var normPath2 = NormalizePath(path2, catalogSeparator);

			if (path1 == null)
				return path2;
			if (path2 == null)
				return path1;

			var combinedPath = $"{normPath1}{catalogSeparator}{normPath2}";
			var combinedPathParts = combinedPath.Split(catalogSeparator);

			const string thisCatalog = ".";
			const string toParentCatalog = "..";
			int skipping = 0;
			var resultParts = new List<string>();

			for (int i = combinedPathParts.Length - 1; i >= 0; i--)
			{
				if (combinedPathParts[i] == thisCatalog)
					continue;

				if (combinedPathParts[i] == toParentCatalog)
				{
					skipping++;
					continue;
				}

				if (skipping > 0)
				{
					skipping--;
					continue;
				}

				resultParts.Add(combinedPathParts[i]);
			}

			resultParts.Reverse();
			return string.Join(catalogSeparator.ToString(), resultParts);
		}

		/// <summary>
		/// Нормализуем путь по краям
		/// </summary>
		public static string NormalizePath(string path, char catalogSeparator)
		{
			if (string.IsNullOrWhiteSpace(path))
				return null;

			var normPath = path.Trim();
			if (normPath.Length < 2)
				return normPath;

			// удаляем слеш в начале и в конце пути
			if (normPath[0] == catalogSeparator)
				normPath = normPath.Substring(1);

			if (normPath[normPath.Length - 1] == catalogSeparator)
				normPath = normPath.Substring(0, normPath.Length - 1);

			return normPath;
		}

		/// <summary>
		/// Возвращает путь каталога файла
		/// </summary>
		public static string GetFolderPath(string fullPath)
		{
			if (string.IsNullOrWhiteSpace(fullPath))
				return null;

			const int notFoundIndex = -1;

			var separatorLastIndex = fullPath.LastIndexOf('/');
			if (separatorLastIndex == notFoundIndex)
				separatorLastIndex = fullPath.LastIndexOf('\\');
			if (separatorLastIndex == notFoundIndex)
				return null;

			return fullPath.Substring(0, separatorLastIndex);
		}
	}
}
