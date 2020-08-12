using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Haraven.Autobiographies.Utils;

namespace Haraven.Autobiographies
{
	public class SelfKnowledgeManager
	{
		private static readonly string AUTOBIOGRAPHY_TEMPLATE_MESSAGE =
			"Salut,</br></br>Ai atasata o autobiografie. Te rog sa o citesti si sa oferi feedback-ul folosindu-te de ghidul primit la inscriere.</br></br>Multumesc pentru participare!";

		public class AutobiographyPairing
		{
			public string Sender { get; set; }
			public string SenderAutobiographyGuid { get; set; }
			public string Recipient { get; set; }
			public string RecipientFeedbackGuid { get; set; }

			public bool SentAutobiographyToRecipient { get; set; }
			public bool SentFeedbackToSender { get; set; }
		}

		public string CurrentAutobiographiesPath { get; set; }
		public string CurrentFeedbackPath { get; set; }

		private Dictionary<string, Email> autobiographyEmails = new Dictionary<string, Email>();
		private Dictionary<string, Email> feedbackEmails = new Dictionary<string, Email>();
		private List<AutobiographyPairing> pairings = new List<AutobiographyPairing>();

		public SelfKnowledgeManager(string autobiographiesPath, string feedbackPath)
		{
			if (string.IsNullOrEmpty(autobiographiesPath) || string.IsNullOrEmpty(feedbackPath))
				throw new Exception
				(
					Logger.FormatMessage
					(
						Constants.Tags.SELF_KNOWLEDGE,
						"Could not start self-knowledge manager. Check your initialization settings."
					)
				);

			CurrentAutobiographiesPath = autobiographiesPath;
			CurrentFeedbackPath = feedbackPath;

			Logger.Log(Constants.Tags.SELF_KNOWLEDGE, $"Initialized self-knowledge manager. Autobiographies will be stored at \"{CurrentAutobiographiesPath}\"; feedback messages will be stored at \"{CurrentFeedbackPath}\"");
		}

		public void ParseAllMails()
		{
			try
			{
				Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "Parsing all e-mails to get autobiographies and feedback messages...");
				var allEmails = GmailManager.Instance.GetAllEmails(GmailManager.Instance.CheckForAttachment);

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
						if (!GmailManager.Instance.SaveAttachment(newAutobiography, CurrentAutobiographiesPath))
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
						if (!GmailManager.Instance.SaveAttachment(newFeedback, CurrentFeedbackPath))
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

				SendAutobiographiesForFeedback(allNewAutobiographies);
				SendFeedbackToBiographyAuthors(allNewFeedback);
			}
			catch (Exception e)
			{
				Logger.LogException(Constants.Tags.SELF_KNOWLEDGE, e);
			}
		}

		private void SendAutobiographiesForFeedback(List<Email> newAutobiographies)
		{
			foreach (var newAutobiography in newAutobiographies)
			{
				var recipient = UserManager.Instance.Users.FirstOrDefault(u =>
					!u.Equals(newAutobiography.Sender) && !pairings.Any(p => p.Recipient.Equals(u)));
				if (recipient == null)
				{
					Logger.Log(Constants.Tags.SELF_KNOWLEDGE, $"Could not assign recipient for autobiography from {newAutobiography.Sender}", LogType.Error);
					continue;
				}

				Logger.Log(Constants.Tags.SELF_KNOWLEDGE, $"Sending autobiography from {newAutobiography.Sender} to {recipient}...");
				GmailManager.Instance.SendAttachmentTo(recipient, newAutobiography, CurrentFeedbackPath, $"[{Constants.AUTOBIOGRAPHY_MAIL_TAG}]", AUTOBIOGRAPHY_TEMPLATE_MESSAGE);
				pairings.Add(new AutobiographyPairing());
				Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "Sent email successfully");
			}
		}

		private void SendAutobiographiesForFeedback(List<Email> newFeedbacks)
		{
			foreach (var newFeedback in newFeedbacks)
			{

				if (recipient == null)
				{
					Logger.Log(Constants.Tags.SELF_KNOWLEDGE, $"Could not assign recipient for autobiography from {newFeedback.Sender}", LogType.Error);
					continue;
				}

				Logger.Log(Constants.Tags.SELF_KNOWLEDGE, $"Sending autobiography from {newFeedback.Sender} to {recipient}...");
				GmailManager.Instance.SendAttachmentTo(recipient, newFeedback, CurrentFeedbackPath, $"[{Constants.AUTOBIOGRAPHY_MAIL_TAG}]", AUTOBIOGRAPHY_TEMPLATE_MESSAGE);
				Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "Sent email successfully");
			}
		}

		private void FlushEmails()
		{
			// TODO
		}
	}
}