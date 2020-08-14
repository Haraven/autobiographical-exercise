using System;
using System.Threading;
using System.Threading.Tasks;
using Haraven.Autobiographies.Utils;

namespace Haraven.Autobiographies
{
	public static class TaskRepeater
	{
		/// <summary>
		/// Repeats the specified action indefinitely, at the specified polling interval,
		/// allowing the task to be cancelled at any time
		/// </summary>
		/// <param name="pollInterval">the amount of time to wait until the action should be invoked again</param>
		/// <param name="action">the action to invoke</param>
		/// <param name="token">the cancellation token that can interrupt the task</param>
		/// <returns></returns>
		public static Task Interval(TimeSpan pollInterval, Action action, CancellationToken token)
		{
			// we don't use Observable.Interval:
			// if we block, the values start bunching up behind each other
			return Task.Factory.StartNew(
				() =>
				{
					Logger.Log(Constants.Tags.THREADING, "Starting task execution...");
					while (true)
					{
						Logger.Log(Constants.Tags.THREADING, "Executing repeated task action...");

						action();

						Logger.Log(Constants.Tags.THREADING, "Executing repeated task action successfully.");

						if (token.WasWaitCancellationRequested(pollInterval))
						{
							Logger.Log(Constants.Tags.THREADING, "Exiting task...");
							break;
						}
					}
				}, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		}
	}

	public static class CancellationTokenExtensions
	{
		/// <summary>
		/// Performs one wait cycle to check if a cancellation has been requested
		/// for the given token
		/// </summary>
		/// <param name="token">the token to check for cancellation</param>
		/// <param name="timeout">the amount of time to wait before cancelling the wait</param>
		/// <returns>true if the token has been cancelled, false otherwise</returns>
		public static bool WasWaitCancellationRequested(this CancellationToken token, TimeSpan timeout)
		{
			return token.WaitHandle.WaitOne(timeout);
		}
	}
}