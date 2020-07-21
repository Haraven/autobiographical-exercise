using System;
using System.Threading;
using Haraven.Autobiographies.Utils;

namespace Haraven.Autobiographies
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			var cancellationTokenSource = new CancellationTokenSource();
			try
			{
				var userManager = new UserManager(Paths.REGISTERED_USERS_PATH);

				var gmailManager = new GmailManager(Constants.GmailApi.SCOPES, Constants.GmailApi.APPLICATION_NAME,
					Constants.GmailApi.CURRENT_USER, Paths.GMAIL_CREDENTIALS_PATH, Paths.GMAIL_TOKEN_PATH,
					cancellationTokenSource.Token);

				var autobiographyManager =
					new SelfKnowledgeManager(Paths.AUTOBIOGRAPHIES_PATH, Paths.FEEDBACK_PATH);

				TaskRepeater.Interval(TimeSpan.FromMinutes(5d), autobiographyManager.ParseAllMails,
					cancellationTokenSource.Token);

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