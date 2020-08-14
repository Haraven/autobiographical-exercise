using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using Haraven.Autobiographies.Utils;
using Pastel;

namespace Haraven.Autobiographies
{
	/// <summary>
	/// The types of logs supported by the logger
	/// (influences the colors of the messages)
	/// </summary>
	public enum LogType
	{
		/// <summary>
		/// Colorless log
		/// </summary>
		None,
		/// <summary>
		/// See <see cref="Constants.Logging.INFO_COLOR"/>
		/// </summary>
		Info,
		/// <summary>
		/// See <see cref="Constants.Logging.WARNING_COLOR"/>
		/// </summary>
		Warning,
		/// <summary>
		/// See <see cref="Constants.Logging.ERROR_COLOR"/>
		/// </summary>
		Error
	}

	/// <summary>
	/// Simple logging class, allowing for formatted console/file logging
	/// </summary>
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

		/// <summary>
		/// Writes a single message with the specified tag and log type to the console,
		/// and if <see cref="LogFilePath"/> and <see cref="NewLineFlushCount"/> are
		/// set accordingly, then also writes the line to the output file
		/// </summary>
		/// <param name="tag">the tag (e.g. manager tag) to use when logging the message</param>
		/// <param name="message">the message to write</param>
		/// <param name="logType">the type of log to write</param>
		public static void Log(string tag, string message, LogType logType = LogType.Info)
		{
			Console.WriteLine(FormatMessage(tag, message, logType));

			if (string.IsNullOrEmpty(LogFilePath) || NewLineFlushCount < 0) return;

			// format the message without the pastel colors
			var formattedMessage = FormatMessageNoColors(tag, message) + Environment.NewLine;

			// no point in adding to loggedLines if we're supposed to flush every line anyway
			if (NewLineFlushCount == 1)
			{
				File.AppendAllText(LogFilePath, formattedMessage);
			}
			else
			{
				// add the formatted message to a list, and once enough messages accumulate,
				// flush all of them
				loggedLines.Add(formattedMessage);

				if (loggedLines.Count < NewLineFlushCount) return;

				File.AppendAllText(LogFilePath, string.Join("", loggedLines));
				loggedLines.Clear();
			}
		}

		/// <summary>
		/// Wrapper for logging exceptions
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="exception"></param>
		public static void LogException(string tag, Exception exception)
		{
			Log(tag, exception.Message + Environment.NewLine + Environment.NewLine + exception.StackTrace, LogType.Error);
		}

		/// <summary>
		/// Formats the given message using the format the logger uses, without writing it anywhere
		/// </summary>
		/// <param name="tag">the tag (e.g. manager tag) to use when logging the message</param>
		/// <param name="message"></param>
		/// <param name="logType"></param>
		/// <returns></returns>
		public static string FormatMessage(string tag, string message, LogType logType = LogType.None)
		{
			var formattedMessage = $"[{DateTime.Now.ToString(CultureInfo.InvariantCulture).Pastel(Constants.Logging.DETAILS_COLOR)}] [{tag.Pastel(Constants.Logging.DETAILS_COLOR)}]: {message}";

			return logType == LogType.None ? formattedMessage : formattedMessage.Pastel(GetColorFor(logType));
		}

		private static string FormatMessageNoColors(string tag, string message)
		{
			return $"[{DateTime.Now.ToString(CultureInfo.InvariantCulture)}] [{tag}]: {message}";
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