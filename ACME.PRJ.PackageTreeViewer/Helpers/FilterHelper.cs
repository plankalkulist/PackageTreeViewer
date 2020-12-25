using System;
using System.Collections.Generic;
using System.Linq;

namespace ACME.PRJ.PackageTreeViewer
{
	public static class FilterHelper
	{
		public static bool IsMatch(string s, string filter
			, bool ignoreCase
			, bool wholeStringMatching)
		{
			if (string.IsNullOrEmpty(s) && string.IsNullOrEmpty(filter))
				return true;
			if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(filter))
				return false;

			if (wholeStringMatching)
			{
				if (ignoreCase)
					return string.Equals(s, filter, StringComparison.InvariantCultureIgnoreCase);
				else
					return string.Equals(s, filter);
			}
			else
			{
				if (ignoreCase)
					return s.ToLower().Contains(filter.ToLower());
				else
					return s.Contains(filter);
			}
		}

		public static bool IsMatchSpecial(string s, string filter
			, bool ignoreCase
			, bool ignoreWhitespace)
		{
			if (string.IsNullOrEmpty(s) && string.IsNullOrEmpty(filter))
				return true;
			if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(filter))
				return false;

			string normS, normFilter;
			if (ignoreWhitespace)
			{
				normS = s.Trim();
				normFilter = filter.Trim();
				if (string.IsNullOrEmpty(normS) && string.IsNullOrEmpty(normFilter))
					return true;
				if (string.IsNullOrEmpty(normS) || string.IsNullOrEmpty(normFilter))
					return false;
			}
			else
			{
				normS = s;
				normFilter = filter;
			}

			if (normFilter == "*")
				return true;

			if (normFilter.Length > 1)
			{
				if (normFilter[normFilter.Length - 1] == '!')
					return IsMatch(normS, normFilter.Substring(0, normFilter.Length - 1), ignoreCase, true);

				if (normFilter[0] == '!')
					return !IsMatch(normS, normFilter.Substring(1, normFilter.Length - 1), ignoreCase, true);
			}

			return IsMatch(normS, normFilter, ignoreCase, false);
		}

		public static IEnumerable<T> WhereAnyIsMatchSpecial<T>(this IEnumerable<T> sequence
			, Func<T, string> selector
			, IEnumerable<string> filters
			, bool ignoreCase
			, bool ignoreWhitespace)
		{
			IEnumerable<T> result;

			foreach (var filter in filters)
			{
				result = sequence
					.Where(item => IsMatchSpecial(selector(item), filter, ignoreCase, ignoreWhitespace));

				if (result.Any())
					return result;
			}

			return new List<T>();
		}
	}
}
