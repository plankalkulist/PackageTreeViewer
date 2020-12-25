using System;
using System.Text;

namespace ACME.PRJ.PackageTreeViewer
{
	internal static class AuthConfigHelper
	{
		public static string LastCodebaseUrl
		{
			get => Settings.Default.LastCodebaseUrl;
			set
			{
				Settings.Default.LastCodebaseUrl = value;
				Settings.Default.Save();
			}
		}

		public static string LastUsername
		{
			get => Settings.Default.LastUsername;
			set
			{
				Settings.Default.LastUsername = value;
				Settings.Default.Save();
			}
		}

		public static string SavedPassword
		{
			get => string.IsNullOrEmpty(Settings.Default.SavedPasswordEncrypted)
				? null
				: Encoding.UTF8.GetString(Convert.FromBase64String(Settings.Default.SavedPasswordEncrypted));
			set
			{
				Settings.Default.SavedPasswordEncrypted = string.IsNullOrEmpty(value)
					? null
					: Settings.Default.SavedPasswordEncrypted = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
				Settings.Default.Save();
			}
		}

		public static void DeletePassword()
		{
			Settings.Default.SavedPasswordEncrypted = null;
			Settings.Default.Save();
		}
	}
}
