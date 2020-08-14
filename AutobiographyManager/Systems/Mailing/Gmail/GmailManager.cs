using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Haraven.Autobiographies.Utils;
using MimeKit;

namespace Haraven.Autobiographies
{
	/// <summary>
	/// Manager for all gmail-related actions (reading the inbox, sending emails, etc.)
	/// </summary>
	public class GmailManager
	{
		/// <summary>
		/// The current <see cref="GmailManager"/> instance.
		/// </summary>
		public static GmailManager Instance { get; private set; }

		/// <summary>
		/// The path to retrieve the Gmail authentication credentials from
		/// </summary>
		public string CredentialPath { get; }
		/// <summary>
		/// The path to store the authentication token at after the first login
		/// </summary>
		public string TokenPath { get; }
		/// <summary>
		/// The name of the Google application that will use Gmail
		/// </summary>
		public string ApplicationName { get; }
		/// <summary>
		/// The name of the current user that will have his inbox manipulated
		/// </summary>
		public string CurrentUser { get; }

		private string[] emailScopes;
		private CancellationToken cancellationToken;
		private UserCredential credential;

		private GmailService service;

		/// <summary>
		/// Initializes the manager.
		/// </summary>
		/// <param name="scopes">the Gmail API scopes (see <see cref="GmailService.Scope"/>)</param>
		/// <param name="applicationName">the name of the application that will use Gmail</param>
		/// <param name="currentUser">the name of the user who will use Gmail</param>
		/// <param name="credentialPath">the path to retrieve the API authentication credentials from</param>
		/// <param name="tokenPath">the path to save the authentication token at</param>
		/// <param name="token">the cancellation token to be used when the user wants to exit the application</param>
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

		/// <summary>
		/// Removes the formatting from the "from" header
		/// (Gmail emails' "from" header have a structure similar to:
		/// John Doe &lt;john.doe@johndoe.com&gt;)
		/// </summary>
		/// <param name="from"></param>
		/// <returns></returns>
		private static string FormatFromToEmail(string from)
		{
			if (string.IsNullOrEmpty(from)) return from;

			var emailStartTagIndex = from.IndexOf('<');

			if (emailStartTagIndex < 0) return from;

			return from.Substring(emailStartTagIndex + 1, from.IndexOf('>') - emailStartTagIndex - 1);
		}

		/// <summary>
		/// Retrieves all emails for the user.
		///
		/// <para>WARNING: This retrieves ALL inbox emails,
		/// including send emails and the likes</para>
		/// </summary>
		/// <returns></returns>
		public List<Email> GetAllEmails()
		{
			var emails = new List<Email>();

			Logger.Log(Constants.Tags.GMAIL, "Retrieving all e-mail messages...");

			try
			{
				var request = service.Users.Messages.List(CurrentUser);

				var messages = request.Execute().Messages;
				if ((messages?.Count ?? 0) == 0) return emails;

				foreach (var messageItem in messages)
				{
					try
					{
						var messageRequest =
							service.Users.Messages.Get(CurrentUser, messageItem.Id);
						var message = messageRequest.Execute();

						var sender =
							FormatFromToEmail(message.Payload.Headers
								                  .FirstOrDefault(h => h.Name.ContainsCaseInsensitive("from"))?.Value ??
							                  string.Empty);

						if (sender?.Equals(Constants.GmailApi.DEFAULT_EMAIL) ?? false) continue;

						var email = new Email
						{
							Title = message.Payload.Headers.FirstOrDefault(h =>
								h.Name.ContainsCaseInsensitive("subject"))?.Value ?? string.Empty,
							Sender = sender,
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
			Logger.Log(Constants.Tags.GMAIL, "Saving attachment of email...");

			if (email == null)
			{
				Logger.Log(Constants.Tags.GMAIL, "Attempted to save the attachments of a null e-mail.", LogType.Error);
				return false;
			}

			if (!string.IsNullOrEmpty(email.AttachmentId) && !CheckForAttachment(email))
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
			Logger.Log(Constants.Tags.GMAIL, $"Saved attachment at \"{filePath}\"");

			return true;
		}

		public bool SendAttachmentTo(string recipient, Email email, string attachmentRootPath, string subject,
			string body)
		{
			Logger.Log(Constants.Tags.GMAIL, $"Sending attachment to {recipient}");

			if (email == null)
			{
				Logger.Log(Constants.Tags.GMAIL, "Attempted to send the attachments of a null e-mail.", LogType.Error);
				return false;
			}

			try
			{
				var mail = new MailMessage
				{
					Subject = subject,
					Body = body,
					From = new MailAddress(Constants.GmailApi.DEFAULT_EMAIL),
					IsBodyHtml = true
				};
				var attachmentPath = Path.Combine(attachmentRootPath,
					email.AttachmentFileGuid + "." + email.AttachmentExtension).Replace("/", "\\");
				mail.Attachments.Add(new Attachment(attachmentPath));
				mail.To.Add(new MailAddress(recipient));
				var mimeMessage = MimeMessage.CreateFromMailMessage(mail);

				var message = new Message
				{
					Raw = StringUtils.Base64UrlEncode(mimeMessage.ToString())
				};

				service.Users.Messages.Send(message, Constants.GmailApi.CURRENT_USER).Execute();

				Logger.Log(Constants.Tags.GMAIL, $"Sent attachment to {recipient}");

				return true;
			}
			catch (Exception e)
			{
				Logger.LogException(Constants.Tags.GMAIL, e);
				return false;
			}
		}
	}
}