using System;
using System.IO;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
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
				var gmailManager = new GmailManager(Constants.GmailApi.SCOPES, Constants.GmailApi.APPLICATION_NAME,
					Constants.GmailApi.CURRENT_USER, Paths.GMAIL_CREDENTIALS_PATH, Paths.GMAIL_TOKEN_PATH, cancellationTokenSource.Token);

				var autobiographyManager =
					new SelfKnowledgeManager(Paths.AUTOBIOGRAPHIES_PATH, Paths.FEEDBACK_PATH);

				TaskRepeater.Interval(TimeSpan.FromMinutes(5d), autobiographyManager.ParseAllMails, cancellationTokenSource.Token);

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