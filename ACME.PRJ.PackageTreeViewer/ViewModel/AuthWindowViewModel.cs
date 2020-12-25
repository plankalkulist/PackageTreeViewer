using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ACME.PRJ.PackageTreeViewer.ViewModel
{
	public class AuthWindowViewModel : INotifyPropertyChanged
	{
		/// <summary>
		/// Поле Codebase URL
		/// </summary>
		public string CodebaseUrl
		{
			get => _codebaseUrl;
			set
			{
				_codebaseUrl = value;
				ValidateFields();
			}
		}
		private string _codebaseUrl;

		/// <summary>
		/// Поле имя пользователя
		/// </summary>
		public string Username
		{
			get => _username;
			set
			{
				_username = value;
				ValidateFields();
			}
		}
		private string _username;

		/// <summary>
		/// Сохранить пароль?
		/// </summary>
		public bool IsPasswordSaving { get; set; }

		/// <summary>
		/// Валидны ли поля
		/// </summary>
		public bool IsFieldsValid
		{
			get => _isFieldsValid;
			set
			{
				_isFieldsValid = value;
				NotifyPropertyChanged();
			}
		}
		private bool _isFieldsValid;

		#region Commands
		/// <summary>
		/// Отмена входа
		/// </summary>
		public ICommand Cancel
		{
			get
			{
				return _cancel
						?? (_cancel = new RelayCommand(
							p => true, p => Environment.Exit(0)));
			}
		}
		private ICommand _cancel;
		#endregion

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

		private void ValidateFields()
		{
			IsFieldsValid = !string.IsNullOrWhiteSpace(_codebaseUrl) && !string.IsNullOrWhiteSpace(_username);
		}
	}
}
