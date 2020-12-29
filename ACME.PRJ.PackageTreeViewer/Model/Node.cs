using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace ACME.PRJ.PackageTreeViewer.Model
{
	/// <summary>
	/// Узел дерева (должен не зависеть от типа Data)
	/// </summary>
	public class Node : INotifyPropertyChanged
	{
		#region Delegate properties

		/// <summary>
		/// Получение имени (задаётся извне)
		/// </summary>
		public Func<Node, string> GetName { get; set; }

		/// <summary>
		/// Имя
		/// </summary>
		public string Name => GetName(this);

		/// <summary>
		/// Получение Uri иконки (задаётся извне)
		/// </summary>
		public Func<Node, string> GetImageUri { get; set; }

		/// <summary>
		/// Uri иконки
		/// </summary>
		public string ImageUri => GetImageUri(this);

		//public int KindId => GetKindId(this);

		//public Func<Node, int> GetKindId { get; set; }
		#endregion

		#region View properties

		/// <summary>
		/// Получение отображаемого имени (задаётся извне)
		/// </summary>
		public Func<Node, string> GetTitle { get; set; }

		/// <summary>
		/// Отображаемое имя
		/// </summary>
		public string Title => GetTitle(this);

		/// <summary>
		/// Видимость
		/// </summary>
		public Visibility Visibility
		{
			get => _Visibility;
			set
			{
				_Visibility = value;
				NotifyPropertyChanged();
			}
		}
		protected Visibility _Visibility;

		/// <summary>
		/// Цвет заднего фона
		/// </summary>
		public Brush BackgroundColor
		{
			get => _backgroundColor;
			set
			{
				_backgroundColor = value;
				NotifyPropertyChanged();
			}
		}
		protected Brush _backgroundColor;

		/// <summary>
		/// Раскрыт ли узел (показаны ли дочерние узлы)
		/// </summary>
		public bool IsExpanded
        {
            get => _IsExpanded;
            set
            {
                _IsExpanded = value;
                NotifyPropertyChanged();
            }
        }
        protected bool _IsExpanded;
        
		/// <summary>
		/// Выбран ли
		/// </summary>
        public bool IsSelected
        {
            get => _IsSelected;
            set
            {
                _IsSelected = value;
                NotifyPropertyChanged();
            }
        }
        protected bool _IsSelected;
		#endregion

		/// <summary>
		/// Родительский узел
		/// </summary>
		public Node Parent { get; set; }

		/// <summary>
		/// Дочерние узлы
		/// </summary>
		public ObservableCollection<Node> Nodes
		{
			get => _Nodes;
			set
			{
				_Nodes = value;
				NotifyPropertyChanged();
			}
		}
		protected ObservableCollection<Node> _Nodes;

		/// <summary>
		/// Игнорируется
		/// </summary>
		public bool IsIgnored { get; set; }

		/// <summary>
		/// Основные данные
		/// </summary>
		public object Data { get; set; }

		/// <summary>
		/// Ошибка в данных
		/// </summary>
		public object Error { get; set; }

		/// <summary>
		/// Данные заполнены успешно
		/// </summary>
		public bool IsSuccess => Error == null;

		/// <summary>
		/// Данные заполнены не успешно
		/// </summary>
		public bool NotSuccess => !IsSuccess;

		/// <summary>
		/// Дополнительные данные
		/// </summary>
		public object Tag { get; set; }

		/// <summary>
		/// Больше данных!!
		/// </summary>
		public List<object> Tags { get; set; }

		/// <summary>
		/// Для отладки
		/// </summary>
		public override string ToString()
		{
			return Tags != null
				? $"{Name} {string.Join(" ", Tags.Select(tag => $"[{tag}]"))}"
				: Name;
		}

		/// <summary>
		/// Оповещение о изменении значения свойства
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Вызов события <see cref="PropertyChanged"/>
		/// </summary>
		/// <param name="propertyName">Вызывающее свойство</param>
		protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
