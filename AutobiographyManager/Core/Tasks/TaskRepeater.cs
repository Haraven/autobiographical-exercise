using System;
using System.Threading;
using System.Threading.Tasks;
using Haraven.Autobiographies.Utils;
using Pastel;

namespace Haraven.Autobiographies
{
	public static class TaskRepeater
	{
		public static Task Interval(TimeSpan pollInterval, Action action, CancellationToken token)
		{
			// We don't use Observable.Interval:
			// If we block, the values start bunching up behind each other.
			return Task.Factory.StartNew(
				() =>
				{
					Logger.Log(Constants.Tags.THREADING, "Starting task execution...", LogType.None);
					while (true)
					{
						Logger.Log(Constants.Tags.THREADING, "Executing repeated task action...", LogType.None);

						action();

						Logger.Log(Constants.Tags.THREADING, "Executing repeated task action successfully.", LogType.None);

						if (token.WaitCancellationRequested(pollInterval))
						{
							Logger.Log(Constants.Tags.THREADING, "Exiting task...", LogType.None);
							break;
						}
					}
				}, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		}
	}

	public static class CancellationTokenExtensions
	{
		public static bool WaitCancellationRequested(this CancellationToken token, TimeSpan timeout)
		{
			return token.WaitHandle.WaitOne(timeout);
		}
	}
}