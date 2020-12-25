using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ACME.PRJ.PackageTreeViewer.Model;

namespace ACME.PRJ.PackageTreeViewer.ViewModel
{
	public class DiffStateToColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var diffState = value as DiffStates?;

			switch (diffState)
			{
				case DiffStates.TheSame:
					return new SolidColorBrush(Colors.Black);
				case DiffStates.Appeared:
					return new SolidColorBrush(Colors.Green);
				case DiffStates.ParentAppeared:
					return new SolidColorBrush(Colors.DarkGreen);
				case DiffStates.Disappeared:
					return new SolidColorBrush(Colors.Red);
				case DiffStates.ParentDisappeared:
					return new SolidColorBrush(Colors.DarkRed);
				case DiffStates.DataModified:
					return new SolidColorBrush(Colors.DarkSlateBlue);
				case DiffStates.ChildrenModified:
					return new SolidColorBrush(Colors.Orange);
				case DiffStates.DataModified | DiffStates.ChildrenModified:
					return new SolidColorBrush(Colors.DarkOrange);
				case DiffStates.NotSolvedYet:
				default:
					return new SolidColorBrush(Colors.Gray);
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
