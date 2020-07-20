using System;
using System.Drawing;
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
		public static void Log(string tag, string message, LogType logType = LogType.Info)
		{
			Console.WriteLine(FormatMessage(tag, message, logType));
		}

		public static void LogException(string tag, Exception exception)
		{
			Log(tag, exception.Message + "\n\n" + exception.StackTrace, LogType.Error);
		}

		public static string FormatMessage(string tag, string message, LogType logType = LogType.None)
		{
			var formattedMessage = $"[ {tag} ]: {message}";

			return logType == LogType.None ? formattedMessage : formattedMessage.Pastel(GetColorFor(logType));
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
				case LogType.None:
					// cascades
				default:
					throw new ArgumentOutOfRangeException(nameof(logType), logType, null);
			}
		}
	}
}