using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Haraven.Autobiographies.Utils;

namespace Haraven.Autobiographies
{
	public class GmailManager
	{
		public static GmailManager Instance { get; private set; }

		public string CredentialPath { get; }
		public string TokenPath { get; }
		public string ApplicationName { get; }
		public string CurrentUser { get; }

		private string[] emailScopes;
		private CancellationToken cancellationToken;
		private UserCredential credential;

		private GmailService service;

		public GmailManager(string[] scopes, string applicationName, string currentUser, string credentialPath,
			string tokenPath, CancellationToken token)
		{
			if (scopes == null || string.IsNullOrEmpty(applicationName) || string.IsNullOrEmpty(currentUser) ||
			    string.IsNullOrEmpty(credentialPath) || string.IsNullOrEmpty(tokenPath))
				throw new Exception
				(
					Logger.FormatMessage
					(
						Constants.Tags.GMAIL,
						"Could not start Gmail manager. Check your initialization settings."
					)
				);

			emailScopes = scopes;
			ApplicationName = applicationName;
			CurrentUser = currentUser;
			CredentialPath = credentialPath;
			TokenPath = tokenPath;
			cancellationToken = token;

			Initialize();
		}

		private void Initialize()
		{
			Logger.Log(Constants.Tags.GMAIL, "Initializing...");

			using (var stream =
				new FileStream(CredentialPath, FileMode.Open, FileAccess.Read))
			{
				// The file token.json stores the user's access and refresh tokens, and is created
				// automatically when the authorization flow completes for the first time
				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.Load(stream).Secrets,
					emailScopes,
					"user",
					cancellationToken,
					new FileDataStore(TokenPath, true)).Result;
				Logger.Log(Constants.Tags.GMAIL, $"Credential file saved to: {TokenPath}");
			}

			// Create Gmail API service
			service = new GmailService(new BaseClientService.Initializer
			{
				HttpClientInitializer = credential,
				ApplicationName = ApplicationName,
			});

			Instance = this;

			Logger.Log(Constants.Tags.GMAIL, "Finished initializing.");
		}

		public List<Email> GetAllEmails(Predicate<Email> predicate = null)
		{
			var emails = new List<Email>();

			Logger.Log(Constants.Tags.GMAIL, "Retrieving all e-mail messages...");

			try
			{
				var request = service.Users.Messages.List(Constants.GmailApi.CURRENT_USER);


				var messages = request.Execute().Messages;
				if ((messages?.Count ?? 0) == 0) return emails;

				foreach (var messageItem in messages)
				{
					try
					{
						var messageRequest =
							service.Users.Messages.Get(Constants.GmailApi.CURRENT_USER, messageItem.Id);
						var message = messageRequest.Execute();
						var email = new Email
						{
							Title = message.Payload.Headers.FirstOrDefault(h =>
								h.Name.ContainsCaseInsensitive("subject"))?.Value ?? string.Empty,
							Sender = message.Payload.Headers.FirstOrDefault(h =>
								h.Name.ContainsCaseInsensitive("from"))?.Value ?? string.Empty,
							MessageId = messageItem.Id
						};

						emails.Add(email);
					}
					catch (Exception e)
					{
						Logger.LogException(Constants.Tags.GMAIL, e);
					}
				}

				Logger.Log(Constants.Tags.GMAIL, "Retrieved all e-mail messages.");
				return emails;
			}
			catch (Exception e)
			{
				Logger.LogException(Constants.Tags.GMAIL, e);
			}

			return emails;
		}

		public bool CheckForAttachment(Email email)
		{
			if (email == null)
			{
				Logger.Log(Constants.Tags.GMAIL, "Attempted to check for attachments on a null e-mail.", LogType.Error);
				return false;
			}

			try
			{
				var messageRequest =
					service.Users.Messages.Get(Constants.GmailApi.CURRENT_USER, email.MessageId);

				var parts = messageRequest.Execute().Payload.Parts;
				foreach (var messagePart in parts)
				{
					if (string.IsNullOrEmpty(messagePart.Filename)) continue;

					email.AttachmentExtension =
						messagePart.Filename.Substring(messagePart.Filename.LastIndexOf('.') + 1);
					email.AttachmentId = messagePart.Body.AttachmentId;
					email.AreAttachmentsInitialized = true;
					return true;
				}
			}
			catch (Exception e)
			{
				Logger.LogException(Constants.Tags.GMAIL, e);
			}

			return false;
		}

		public bool SaveAttachment(Email email, string path)
		{
			Logger.Log(Constants.Tags.GMAIL, "Saving attachment of email");

			if (email == null)
			{
				Logger.Log(Constants.Tags.GMAIL, "Attempted to save the attachments of a null e-mail.", LogType.Error);
				return false;
			}

			if (!email.AreAttachmentsInitialized && !CheckForAttachment(email))
			{
				Logger.Log(Constants.Tags.GMAIL,
					$"{email} has no attachments to save.", LogType.Warning);
				return false;
			}

			var attachmentPart = service.Users.Messages.Attachments
				.Get(Constants.GmailApi.CURRENT_USER, email.MessageId, email.AttachmentId)
				.Execute();

			// Converting from RFC 4648 base64 to base64url encoding
			// see http://en.wikipedia.org/wiki/Base64#Implementations_and_history
			var attachmentData = attachmentPart.Data.Replace('-', '+').Replace('_', '/');
			var data = Convert.FromBase64String(attachmentData);

			email.AttachmentFileGuid = Guid.NewGuid();
			var filePath = Path.Combine(path, email.AttachmentFileGuid + "." + email.AttachmentExtension);
			File.WriteAllBytes(filePath, data);
			Logger.Log(Constants.Tags.GMAIL, $"Saved attachment at {filePath}");

			return true;
		}

		public bool SendAttachmentTo(string recipient, Email email, string attachmentRootPath)
		{
			Logger.Log(Constants.Tags.GMAIL, $"Sending attachment to {recipient}");

			if (email == null)
			{
				Logger.Log(Constants.Tags.GMAIL, "Attempted to send the attachments of a null e-mail.", LogType.Error);
				return false;
			}

			service.Users.Messages.Send()

			Logger.Log(Constants.Tags.GMAIL, $"Sent attachment to {recipient}");
		}
	}
}