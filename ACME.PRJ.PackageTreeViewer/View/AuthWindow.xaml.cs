using System;
using System.Windows;
using ACME.PRJ.CodebaseCommons;
using ACME.PRJ.PackageTreeViewer.ViewModel;

namespace ACME.PRJ.PackageTreeViewer.View
{
	/// <summary>
	/// Interaction logic for AuthWindow.xaml
	/// </summary>
	public partial class AuthWindow : Window
	{
		public AuthWindow()
		{
			InitializeComponent();

			// Инициализация AuthWindowViewModel
			var viewModel = new AuthWindowViewModel()
			{
				CodebaseUrl = AuthConfigHelper.LastCodebaseUrl,
				Username = AuthConfigHelper.LastUsername
			};

			// берём сохранённый пароль из конфига
			if (!string.IsNullOrEmpty(AuthConfigHelper.SavedPassword))
			{
				viewModel.IsPasswordSaving = true;
				txtPassword.Password = AuthConfigHelper.SavedPassword;
			}

			DataContext = viewModel;
		}

		/// <summary>
		/// Реализовал здесь, чтобы не тянуть зависимость от окон во ViewModel 
		/// </summary>
		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var authViewModel = DataContext as AuthWindowViewModel;
				var codebaseService = CommonContainer.Resolve<ICodebaseService>(authViewModel.CodebaseUrl);
				codebaseService.LogIn(authViewModel.Username, txtPassword.Password);

				AuthConfigHelper.LastCodebaseUrl = authViewModel.CodebaseUrl;
				AuthConfigHelper.LastUsername = authViewModel.Username;

				if (authViewModel.IsPasswordSaving)
					AuthConfigHelper.SavedPassword = txtPassword.Password;
				else
					AuthConfigHelper.DeletePassword();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, this.Title);
				return;
			}

			new MainWindow().Show();
			this.Close();
		}
	}
}
