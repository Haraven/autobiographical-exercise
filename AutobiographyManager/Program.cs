using System;
using System.Configuration;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Haraven.Autobiographies.Utils;

namespace Haraven.Autobiographies
{
	internal class Program
	{
		private static void Main()
		{
			if (!Validate())
			{
				Console.WriteLine("Cannot launch application. Please make sure it is configured properly (read README.md)");
				return;
			}

			// used to kill the mail-checking thread when exiting
			var cancellationTokenSource = new CancellationTokenSource();
			try
			{
				Logger.Log(Constants.Tags.SYSTEM,
					Environment.NewLine + "===========================================================" +
					Environment.NewLine +
					"YOU MAY EXIT THIS APPLICATION AT ANY TIME BY PRESSING ENTER" + Environment.NewLine +
					"===========================================================" + Environment.NewLine);

				// also log the output to the designated output file, using the .exe directory as root
				Logger.LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
					Constants.Logging.OUTPUT_LOG_FILENAME);

				// rename old logfile (if it exists)
				if (File.Exists(Logger.LogFilePath))
				{
					var oldLogfile = Logger.LogFilePath + ".old";
					if (File.Exists(oldLogfile))
						File.Delete(oldLogfile);
					File.Move(Logger.LogFilePath, oldLogfile);
				}

				Logger.NewLineFlushCount = Constants.Logging.NEW_LINE_FLUSH_COUNT;

				var _ = new UserManager(Paths.REGISTERED_USERS_PATH);

				var __ = new GmailManager(Constants.GmailApi.SCOPES, Constants.GmailApi.APPLICATION_NAME,
					Constants.GmailApi.CURRENT_USER, Paths.GMAIL_CREDENTIALS_PATH, Paths.GMAIL_TOKEN_PATH,
					cancellationTokenSource.Token);

				var autobiographyManager =
					new SelfKnowledgeManager(Paths.AUTOBIOGRAPHIES_PATH, Paths.FEEDBACK_PATH, Paths.PAIRING_DATA_FILE);

				// start a new thread that checks emails every few minutes, as dictated by Constants.EMAIL_CHECKING_INTERVAL
				// allowing for cancellation at any point via the cancellation token
				TaskRepeater.Interval(TimeSpan.FromMinutes(Constants.EMAIL_CHECKING_INTERVAL),
					autobiographyManager.ParseAllMails,
					cancellationTokenSource.Token);

				Console.ReadLine();
			}
			catch (Exception e)
			{
				Logger.LogException(Constants.Tags.SYSTEM, e);
			}

			cancellationTokenSource.Cancel();
		}

		private static bool Validate()
		{
			return !string.IsNullOrEmpty(
				ConfigurationManager.AppSettings.Get(Constants.GmailApi.APPLICATION_EMAIL_CONFIG_FIELD));
		}
	}
}