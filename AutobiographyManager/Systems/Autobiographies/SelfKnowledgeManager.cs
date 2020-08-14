using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Haraven.Autobiographies.Utils;
using Newtonsoft.Json;

namespace Haraven.Autobiographies
{
	/// <summary>
	/// Performs the brunt of the autobiography/feedback parsing and sending logic.
	///
	/// Will retrieve new autobiographies and feedback from the <see cref="GmailManager"/>,
	/// will download their attachments, then forward them to the appropriate recipients, while
	/// ensuring data persistence so as to not have to leave the application running constantly.
	/// </summary>
	public class SelfKnowledgeManager
	{
		/// <summary>
		/// The email body to use when sending autobiographies for feedback
		/// </summary>
		private const string AUTOBIOGRAPHY_TEMPLATE_MESSAGE =
			"Salut, ai atasata o autobiografie. Te rog sa o citesti si sa oferi feedback-ul folosindu-te de ghidul primit la inscriere. Multumesc pentru participare!";

		/// <summary>
		/// The email body to use when sending feedback back to the autobiography author
		/// </summary>
		private const string FEEDBACK_TEMPLATE_MESSAGE =
			"Salut, ai atasat acestui mail feedback-ul la autobiografia trimisa de tine. Sper sa-ti fie util! ";

		/// <summary>
		/// Pairing class used to store data about who sent an autobiography, who received it for feedback, and whether the mails were successfully sent
		/// </summary>
		public class AutobiographyPairing
		{
			public string AutobiographySender { get; set; }
			public string AutobiographyRecipient { get; set; }

			public bool SentAutobiographyToRecipient { get; set; }
			public bool SentFeedbackToSender { get; set; }

			public AutobiographyPairing(string autobiographySender,
				string autobiographyRecipient, bool sentAutobiographyToRecipient,
				bool sentFeedbackToSender)
			{
				AutobiographySender = autobiographySender;
				AutobiographyRecipient = autobiographyRecipient;
				SentAutobiographyToRecipient = sentAutobiographyToRecipient;
				SentFeedbackToSender = sentFeedbackToSender;
			}
		}

		/// <summary>
		/// The current path (from <see cref="AppDomain.CurrentDomain.BaseDirectory"/>) that autobiographies will be saved at
		/// </summary>
		public string CurrentAutobiographiesPath { get; set; }

		/// <summary>
		/// The current path (from <see cref="AppDomain.CurrentDomain.BaseDirectory"/>) that feedback will be saved at
		/// </summary>
		public string CurrentFeedbackPath { get; set; }

		//private readonly Dictionary<string, Email> autobiographyEmails = new Dictionary<string, Email>();
		//private readonly Dictionary<string, Email> feedbackEmails = new Dictionary<string, Email>();
		private List<AutobiographyPairing> autobiographyPairings = new List<AutobiographyPairing>();

		/// <summary>
		/// The current path (from <see cref="AppDomain.CurrentDomain.BaseDirectory"/>) that autobiography pairings will be saved at
		/// </summary>
		private readonly string pairingDataFile;

		public SelfKnowledgeManager(string autobiographiesPath, string feedbackPath, string pairingFile)
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

			pairingDataFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, pairingFile);

			LoadPairings();

			CurrentAutobiographiesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, autobiographiesPath);
			// clear previous autobiographies as they are no longer needed
			if (Directory.Exists(CurrentAutobiographiesPath))
				Directory.Delete(CurrentAutobiographiesPath, true);

			// clear previous feedback as it is no longer needed
			if (Directory.Exists(CurrentFeedbackPath))
				Directory.Delete(CurrentFeedbackPath, true);
			CurrentFeedbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, feedbackPath);

			Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
				$"Initialized self-knowledge manager. Autobiographies will be stored at \"{CurrentAutobiographiesPath}\"; feedback messages will be stored at \"{CurrentFeedbackPath}\"");
		}

		/// <summary>
		/// Reads all emails from the gmail manager, parsing them into new autobiographies/feedback and sending them
		/// back to their appropriate recipients, with persistence
		/// </summary>
		public void ParseAllMails()
		{
			try
			{
				Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
					"Parsing all emails to get autobiographies and feedback messages...");
				var allEmails = GmailManager.Instance.GetAllEmails();

				if ((allEmails?.Count ?? 0) == 0)
				{
					Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "No emails are available");
					return;
				}

				Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
					$"Found {allEmails.Count} emails: [ {string.Join(", ", allEmails)} ]");

				// new autobiography = the email sender is registered and the email hasn't already been paired and sent for feedback
				var allNewAutobiographies = allEmails.Where(m => UserManager.Instance.Users.Contains(m.Sender) &&
				                                                 m.Title.ContainsCaseInsensitive(Constants
					                                                 .AUTOBIOGRAPHY_MAIL_TAG) &&
				                                                 !autobiographyPairings.Any(p =>
					                                                 p.AutobiographySender.Equals(m.Sender) &&
					                                                 p.SentAutobiographyToRecipient)).ToList();

				Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
					allNewAutobiographies.Count != 0
						? $"Found {allNewAutobiographies.Count} new autobiograph{(allNewAutobiographies.Count == 1 ? "y" : "ies")}: [ {string.Join(", ", allNewAutobiographies)} ]"
						: "Found no new autobiographies");

				// new feedback = the email sender is registered and the email hasn't already been paired and sent back to the autobiography author
				var allNewFeedback = allEmails.Where(m => UserManager.Instance.Users.Contains(m.Sender) &&
				                                          m.Title.ContainsCaseInsensitive(Constants
					                                          .FEEDBACK_MAIL_TAG) &&
				                                          !autobiographyPairings.Any(p =>
					                                          p.AutobiographyRecipient.Equals(m.Sender) &&
					                                          p.SentFeedbackToSender)).ToList();

				Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
					allNewFeedback.Count != 0
						? $"Found {allNewFeedback.Count} new feedback message{(allNewFeedback.Count == 1 ? string.Empty : "s")}: [ {string.Join(", ", allNewFeedback)} ]"
						: "Found no new feedback");

				for (var i = allNewAutobiographies.Count - 1; i >= 0; --i)
				{
					try
					{
						var newAutobiography = allNewAutobiographies[i];

						// ensure autobiographies root exists
						if (!Directory.Exists(CurrentAutobiographiesPath))
							Directory.CreateDirectory(CurrentAutobiographiesPath);

						// save the attachments of the new autobiographical email
						if (!GmailManager.Instance.SaveAttachment(newAutobiography, CurrentAutobiographiesPath))
						{
							Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
								$"Failed to retrieve attachments for {newAutobiography}. Removing from autobiography list...",
								LogType.Warning);
							allNewAutobiographies.RemoveAt(i);
						}
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

						// ensure feedback root exists
						if (!Directory.Exists(CurrentFeedbackPath))
							Directory.CreateDirectory(CurrentFeedbackPath);

						// save the attachments of the new feedback email
						if (!GmailManager.Instance.SaveAttachment(newFeedback, CurrentFeedbackPath))
						{
							Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
								$"Failed to retrieve attachments for {newFeedback}. Removing from feedback list...",
								LogType.Warning);
							allNewFeedback.RemoveAt(i);
						}
					}
					catch (Exception e)
					{
						Logger.LogException(Constants.Tags.SELF_KNOWLEDGE, e);
					}
				}

				try
				{
					if (allNewAutobiographies.Count > 0)
					{
						Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "Sending autobiographies for feedback...");
						SendAutobiographiesForFeedback(allNewAutobiographies);
						Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "Finished sending autobiographies");
					}
				}
				catch (Exception e)
				{
					Logger.LogException(Constants.Tags.SELF_KNOWLEDGE, e);
				}

				try
				{
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

				FlushPairings();
			}
			catch (Exception e)
			{
				Logger.LogException(Constants.Tags.SELF_KNOWLEDGE, e);
			}
		}


		private void LoadPairings()
		{
			if (!File.Exists(pairingDataFile)) return;

			Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "Loading autobiography pairings from disk...");

			var pairingFileContents = File.ReadAllText(pairingDataFile);
			autobiographyPairings = JsonConvert.DeserializeObject<List<AutobiographyPairing>>(pairingFileContents);

			Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "Loaded autobiography pairings successfully");
		}

		private void FlushPairings()
		{
			if (autobiographyPairings.Count == 0) return;

			Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "Flushing autobiography pairings to disk...");

			File.WriteAllText(pairingDataFile, JsonConvert.SerializeObject(autobiographyPairings, Formatting.Indented));

			Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "Finished Flushing autobiography pairings");
		}

		private void SendAutobiographiesForFeedback(List<Email> newAutobiographies)
		{
			foreach (var newAutobiography in newAutobiographies)
			{
				// try to find a random user who hasn't already received an autobiography
				var recipient = UserManager.Instance.Users.FirstOrDefault(u =>
					!u.Equals(newAutobiography.Sender) &&
					!autobiographyPairings.Any(
						p => p.AutobiographyRecipient.Equals(u) && p.SentAutobiographyToRecipient));
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

				// save the paired autobiography author & future feedback sender
				autobiographyPairings.Add(new AutobiographyPairing(newAutobiography.Sender, recipient, true, false));
				Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "Sent email successfully");
			}
		}

		private void SendFeedbackToBiographyAuthors(List<Email> newFeedback)
		{
			foreach (var feedback in newFeedback)
			{
				// try to find an existing autobiography pairing (cannot send feedback if not paired to an autobiography)
				var pairedAutobiography =
					autobiographyPairings.FirstOrDefault(p => p.AutobiographyRecipient.Equals(feedback.Sender));

				if (pairedAutobiography == null)
				{
					Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
						$"Could not send feedback from {feedback.Sender} because no pairing to an autobiography was found. Skipping...",
						LogType.Error);
					continue;
				}

				Logger.Log(Constants.Tags.SELF_KNOWLEDGE,
					$"Sending feedback from {feedback.Sender} to {pairedAutobiography.AutobiographySender}...");
				GmailManager.Instance.SendAttachmentTo(pairedAutobiography.AutobiographySender, feedback,
					CurrentFeedbackPath,
					$"{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Constants.FEEDBACK_MAIL_TAG)} la autobiografie",
					FEEDBACK_TEMPLATE_MESSAGE);

				// mark feedback as sent
				pairedAutobiography.SentFeedbackToSender = true;
				Logger.Log(Constants.Tags.SELF_KNOWLEDGE, "Sent feedback successfully");
			}
		}
	}
}