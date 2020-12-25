using System;

namespace ACME.PRJ.CodebaseCommons
{
	/// <summary>
	/// Наблюдатель за долгим прогрессом выполнения функции
	/// </summary>
	public class ProgressObserver
	{
		public static readonly string StartedStatus = "Started";
		public static readonly string FinishedStatus = "Finished";

		public object Status
		{
			get => _Status;
			set
			{
				_Status = value;
				OnStatusChanged(null);
			}
		}

		public object ProgressData
		{
			get => _progressData;
			set
			{
				_progressData = value;
				OnDataChanged(null);
			}
		}

		public int Counter { get; set; }

		public event EventHandler StatusChanged;

		public event EventHandler DataChanged;

		private object _Status;

		private object _progressData;

		private void OnStatusChanged(EventArgs e)
		{
			StatusChanged?.Invoke(this, e);
		}

		private void OnDataChanged(EventArgs e)
		{
			DataChanged?.Invoke(this, e);
		}
	}
}
