using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Haraven.Autobiographies.Utils;

namespace Haraven.Autobiographies
{
	public class SelfKnowledgeManager
	{
		private static readonly string AUTOBIOGRAPHY_TEMPLATE_MESSAGE =
			"Salut, ai atasata o autobiografie. Te rog sa o citesti si sa oferi feedback-ul folosindu-te de ghidul primit la inscriere. Multumesc pentru participare!";

		private static readonly string FEEDBACK_TEMPLATE_MESSAGE =
			"Salut, ai atasat acestui mail feedback-ul la autobiografia trimisa de tine. Sper sa-ti fie util! ";

		public class AutobiographyPairing
		{
			public string AutobiographySender { get; set; }
			public string SenderAutobiographyGuid { get; set; }
			public string AutobiographyRecipient { get; set; }
			public string RecipientFeedbackGuid { get; set; }

			public bool SentAutobiographyToRecipient { get; set; }
			public bool SentFeedbackToSender { get; set; }

			public AutobiographyPairing(string autobiographySender, string senderAutobiographyGuid,
				string autobiographyRecipient, string recipientFeedbackGuid, bool sentAutobiographyToRecipient,
				bool sentFeedbackToSender)
			{
				AutobiographySender = autobiographySender;
				SenderAutobiographyGuid = senderAutobiographyGuid;
				AutobiographyRecipient = autobiographyRecipient;
				RecipientFeedbackGuid = recipientFeedbackGuid;
				SentAutobiographyToRecipient = sentAutobiographyToRecipient;
				SentFeedbackToSender = sentFeedbackToSender;
			}
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

			CurrentAutobiographiesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, autobiographiesPath);
			CurrentFeedbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, feedbackPath);

			Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
				$"Initialized self-knowledge manager. Autobiographies will be stored at \"{CurrentAutobiographiesPath}\"; feedback messages will be stored at \"{CurrentFeedbackPath}\"");
		}

		public void ParseAllMails()
		{
			try
			{
				Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
					"Parsing all e-mails to get autobiographies and feedback messages...");
				var allEmails = GmailManager.Instance.GetAllEmails(GmailManager.Instance.CheckForAttachment);

				if ((allEmails?.Count ?? 0) == 0)
				{
					Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "No e-mails with attachments are available");
					return;
				}

				Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
					$"Found {allEmails.Count} emails: [ {string.Join(", ", allEmails)} ]");

				var allNewAutobiographies = allEmails.Where(m =>
					m.Title.ContainsCaseInsensitive(Constants.AUTOBIOGRAPHY_MAIL_TAG) &&
					!autobiographyEmails.ContainsKey(m.Sender)).ToList();

				Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
					allNewAutobiographies.Count != 0
						? $"Found {allNewAutobiographies.Count} new autobiograph{(allNewAutobiographies.Count == 1 ? "y" : "ies")}: [ {string.Join(", ", allNewAutobiographies)} ]"
						: "Found no new autobiographies");

				var allNewFeedback = allEmails.Where(m =>
					m.Title.ContainsCaseInsensitive(Constants.FEEDBACK_MAIL_TAG) &&
					!feedbackEmails.ContainsKey(m.Sender)).ToList();

				Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
					allNewFeedback.Count != 0
						? $"Found {allNewFeedback.Count} new feedback message{(allNewFeedback.Count == 1 ? string.Empty : "s")}: [ {string.Join(", ", allNewFeedback)} ]"
						: "Found no new feedback");

				for (var i = allNewAutobiographies.Count - 1; i >= 0; --i)
				{
					try
					{
						var newAutobiography = allNewAutobiographies[i];
						if (!Directory.Exists(CurrentAutobiographiesPath))
							Directory.CreateDirectory(CurrentAutobiographiesPath);
						if (!GmailManager.Instance.SaveAttachment(newAutobiography, CurrentAutobiographiesPath))
						{
							Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
								$"Failed to retrieve attachments for {newAutobiography}. Removing from autobiography list...",
								LogType.Warning);
							allNewAutobiographies.RemoveAt(i);
							continue;
						}

						autobiographyEmails.Add(newAutobiography.Sender, newAutobiography);
					}
					catch (Exception e)
					{
						Logger.LogException(Constants.Tags.SELF_KNOWLEDGE, e);
					}
				}

				for (var i = allNewFeedback.Count - 1; i >= 0; --i)
				{
					try
					{
						var newFeedback = allNewFeedback[i];
						if (!Directory.Exists(CurrentFeedbackPath))
							Directory.CreateDirectory(CurrentFeedbackPath);
						if (!GmailManager.Instance.SaveAttachment(newFeedback, CurrentFeedbackPath))
						{
							Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
								$"Failed to retrieve attachments for {newFeedback}. Removing from feedback list...",
								LogType.Warning);
							allNewFeedback.RemoveAt(i);
							continue;
						}

						feedbackEmails.Add(newFeedback.Sender, newFeedback);
					}
					catch (Exception e)
					{
						Logger.LogException(Constants.Tags.SELF_KNOWLEDGE, e);
					}
				}

				if (allNewAutobiographies.Count > 0)
				{
					Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "Sending autobiographies for feedback...");
					SendAutobiographiesForFeedback(allNewAutobiographies);
					Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "Finished sending autobiographies");
				}

				if (allNewFeedback.Count > 0)
				{
					Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "Sending feedback back to autobiography authors...");
					SendFeedbackToBiographyAuthors(allNewFeedback);
					Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "Finished sending feedback");
				}
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
					!u.Equals(newAutobiography.Sender) && !pairings.Any(p => p.AutobiographyRecipient.Equals(u)));
				if (recipient == null)
				{
					Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
						$"Could not assign recipient for autobiography from {newAutobiography.Sender}", LogType.Error);
					continue;
				}

				Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
					$"Sending autobiography from {newAutobiography.Sender} to {recipient}...");
				GmailManager.Instance.SendAttachmentTo(recipient, newAutobiography, CurrentAutobiographiesPath,
					$"{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Constants.AUTOBIOGRAPHY_MAIL_TAG)} pentru feedback",
					AUTOBIOGRAPHY_TEMPLATE_MESSAGE);
				pairings.Add(new AutobiographyPairing(newAutobiography.Sender,
					newAutobiography.AttachmentFileGuid.ToString(), recipient, null, true, false));
				Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "Sent email successfully");
			}
		}

		private void SendFeedbackToBiographyAuthors(List<Email> newFeedback)
		{
			foreach (var feedback in newFeedback)
			{
				var pairedAutobiography =
					pairings.FirstOrDefault(p => p.AutobiographyRecipient.Equals(feedback.Sender));

				if (pairedAutobiography == null)
				{
					Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
						$"Could not send feedback from {feedback.Sender} because no pairing to an autobiography was found. Skipping...",
						LogType.Error);
					continue;
				}

				if (pairedAutobiography.SentFeedbackToSender)
				{
					Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
						$"Could not send feedback from {feedback.Sender} because feedback was already sent. Skipping...",
						LogType.Error);
					continue;
				}

				Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
					$"Sending feedback from {feedback.Sender} to {pairedAutobiography.AutobiographySender}...");
				GmailManager.Instance.SendAttachmentTo(pairedAutobiography.AutobiographySender, feedback,
					CurrentFeedbackPath,
					$"{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Constants.FEEDBACK_MAIL_TAG)} la autobiografie",
					FEEDBACK_TEMPLATE_MESSAGE);
				Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "Sent email successfully");
			}
		}
	}
}