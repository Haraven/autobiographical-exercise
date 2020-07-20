using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Haraven.Autobiographies.Utils;

namespace Haraven.Autobiographies
{
	public class SelfKnowledgeManager
	{
		public string CurrentAutobiographiesPath { get; set; }
		public string CurrentFeedbackPath { get; set; }

		private GmailManager currentGmailManager;

		private Dictionary<string, Email> autobiographyEmails = new Dictionary<string, Email>();
		private Dictionary<string, Email> feedbackEmails = new Dictionary<string, Email>();

		public SelfKnowledgeManager(GmailManager gmailManager, string autobiographiesPath, string feedbackPath)
		{
			if (gmailManager == null || string.IsNullOrEmpty(autobiographiesPath) || string.IsNullOrEmpty(feedbackPath))
				throw new Exception
				(
					Logger.FormatMessage
					(
						Constants.Tags.SELF_KNOWLEDGE,
						"Could not start self-knowledge manager. Check your initialization settings."
					)
				);

			currentGmailManager = gmailManager;
			CurrentAutobiographiesPath = autobiographiesPath;
			CurrentFeedbackPath = feedbackPath;

			Logger.Log(Constants.Tags.SELF_KNOWLEDGE, $"Initialized self-knowledge manager. Autobiographies will be stored at \"{CurrentAutobiographiesPath}\"; feedback messages will be stored at \"{CurrentFeedbackPath}\"");
		}

		public void ParseAllMails()
		{
			try
			{
				Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "Parsing all e-mails to get autobiographies and feedback messages...");
				var allEmails = currentGmailManager.GetAllEmails(currentGmailManager.CheckForAttachment);

				if ((allEmails?.Count ?? 0) == 0)
				{
					Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "No e-mails with attachments are available");
					return;
				}

				Logger.Log(Constants.Tags.SELF_KNOWLEDGE, $"Found {allEmails.Count} emails: [ {string.Join(", ", allEmails)} ]");

				var allNewAutobiographies = allEmails.Where(m =>
					m.Title.ContainsCaseInsensitive(Constants.AUTOBIOGRAPHY_MAIL_TAG) &&
					!autobiographyEmails.ContainsKey(m.Sender)).ToList();
				Logger.Log(Constants.Tags.SELF_KNOWLEDGE, $"Found {allNewAutobiographies.Count} new autobiographies: [ {string.Join(", ", allNewAutobiographies)} ]");

				var allNewFeedback = allEmails.Where(m =>
					m.Title.ContainsCaseInsensitive(Constants.FEEDBACK_MAIL_TAG) &&
					!feedbackEmails.ContainsKey(m.Sender)).ToList();
				Logger.Log(Constants.Tags.SELF_KNOWLEDGE, $"Found {feedbackEmails.Count} new feedback messages: [ {string.Join(", ", allNewFeedback)} ]");

				foreach (var newAutobiography in allNewAutobiographies)
				{
					try
					{
						if (!Directory.Exists(CurrentAutobiographiesPath))
							Directory.CreateDirectory(CurrentAutobiographiesPath);
						if (!currentGmailManager.SaveAttachment(newAutobiography, CurrentAutobiographiesPath))
						{
							Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
								$"Failed to retrieve attachments for {newAutobiography}",
								LogType.Warning);
							continue;
						}

						autobiographyEmails.Add(newAutobiography.Sender, newAutobiography);
					}
					catch (Exception e)
					{
						Logger.LogException(Constants.Tags.SELF_KNOWLEDGE, e);
					}
				}

				foreach (var newFeedback in allNewFeedback)
				{
					try
					{
						if (!Directory.Exists(CurrentFeedbackPath))
							Directory.CreateDirectory(CurrentFeedbackPath);
						if (!currentGmailManager.SaveAttachment(newFeedback, CurrentFeedbackPath))
						{
							Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
								$"Failed to retrieve attachments for {newFeedback}",
								LogType.Warning);
							continue;
						}

						feedbackEmails.Add(newFeedback.Sender, newFeedback);
					}
					catch (Exception e)
					{
						Logger.LogException(Constants.Tags.SELF_KNOWLEDGE, e);
					}
				}
			}
			catch (Exception e)
			{
				Logger.LogException(Constants.Tags.SELF_KNOWLEDGE, e);
			}
		}

		private void FlushEmails()
		{
			// TODO
		}
	}
}