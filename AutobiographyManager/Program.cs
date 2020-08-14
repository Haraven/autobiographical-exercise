using System;
using System.Drawing;
using System.IO;
using System.Threading;
using Haraven.Autobiographies.Utils;
using Pastel;

namespace Haraven.Autobiographies
{
	internal class Program
	{
		private static void Main()
		{
			var cancellationTokenSource = new CancellationTokenSource();
			try
			{
				Logger.LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.Logging.OUTPUT_LOG_FILENAME);
				Logger.NewLineFlushCount = Constants.Logging.NEW_LINE_FLUSH_COUNT;

				var userManager = new UserManager(Paths.REGISTERED_USERS_PATH);

				var gmailManager = new GmailManager(Constants.GmailApi.SCOPES, Constants.GmailApi.APPLICATION_NAME,
					Constants.GmailApi.CURRENT_USER, Paths.GMAIL_CREDENTIALS_PATH, Paths.GMAIL_TOKEN_PATH,
					cancellationTokenSource.Token);

				var autobiographyManager =
					new SelfKnowledgeManager(Paths.AUTOBIOGRAPHIES_PATH, Paths.FEEDBACK_PATH);

				TaskRepeater.Interval(TimeSpan.FromMinutes(1d), autobiographyManager.ParseAllMails,
					cancellationTokenSource.Token);

				Logger.Log(Constants.Tags.SYSTEM, "You may exit this application at any time by pressing Enter".PastelBg(Color.DarkRed));
				Console.ReadLine();
			}
			catch (Exception e)
			{
				Logger.LogException(Constants.Tags.SYSTEM, e);
			}

			cancellationTokenSource.Cancel();
		}
	}
}