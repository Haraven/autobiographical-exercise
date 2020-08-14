using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using Haraven.Autobiographies.Utils;
using Pastel;

namespace Haraven.Autobiographies
{
	public enum LogType
	{
		None,
		Info,
		Warning,
		Error
	}

	public static class Logger
	{
		/// <summary>
		/// If set, will flush new lines to this file in addition to writing on console
		/// </summary>
		public static string LogFilePath { get; set; }

		/// <summary>
		/// How many lines have to be logged before the logger will flush them to the <see cref="LogFilePath"/> (default is 1 line)
		///
		/// <para>IMPORTANT: No flushing will take place if less than or equal to 0!</para>
		/// </summary>
		public static int NewLineFlushCount { get; set; } = 1;

		// ReSharper disable once InconsistentNaming
		private static readonly List<string> loggedLines = new List<string>();

		public static void Log(string tag, string message, LogType logType = LogType.Info)
		{
			Console.WriteLine(FormatMessage(tag, message, logType));
			if (!string.IsNullOrEmpty(LogFilePath))
			{
				if (NewLineFlushCount < 0) return;

				var formattedMessage = FormatMessageNoColors(tag, message) + Environment.NewLine;

				// no point in adding to loggedLines if we're supposed to flush every line anyway
				if (NewLineFlushCount == 1)
				{
					File.AppendAllText(LogFilePath, formattedMessage);
				}
				else
				{
					loggedLines.Add(formattedMessage);

					if (loggedLines.Count >= NewLineFlushCount)
					{
						File.AppendAllText(LogFilePath, string.Join("", loggedLines));
						loggedLines.Clear();
					}
				}
			}
		}

		public static void LogException(string tag, Exception exception)
		{
			Log(tag, exception.Message + Environment.NewLine + Environment.NewLine + exception.StackTrace, LogType.Error);
		}

		public static string FormatMessage(string tag, string message, LogType logType = LogType.None)
		{
			var formattedMessage = $"[ {DateTime.Now.ToString(CultureInfo.InvariantCulture).Pastel(Constants.Logging.DETAILS_COLOR)} ] [ {tag.Pastel(Constants.Logging.DETAILS_COLOR)} ]: {message}";

			return logType == LogType.None ? formattedMessage : formattedMessage.Pastel(GetColorFor(logType));
		}

		private static string FormatMessageNoColors(string tag, string message)
		{
			return $"[ {DateTime.Now.ToString(CultureInfo.InvariantCulture)} ] [ {tag} ]: {message}";
		}

		private static Color GetColorFor(LogType logType)
		{
			switch (logType)
			{
				case LogType.Info:
					return Constants.Logging.INFO_COLOR;
				case LogType.Warning:
					return Constants.Logging.WARNING_COLOR;
				case LogType.Error:
					return Constants.Logging.ERROR_COLOR;
				// ReSharper disable once RedundantCaseLabel
				case LogType.None:
					// cascades
				default:
					throw new ArgumentOutOfRangeException(nameof(logType), logType, null);
			}
		}
	}
}