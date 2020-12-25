using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace ACME.PRJ.PackageTreeViewer.ViewModel
{
	public class TagsCollectionToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is IEnumerable tagsArray)
			{
				return string.Join(" ", tagsArray.Cast<object>().Select(obj => $"[{obj.ToString()}]"));
			}

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
