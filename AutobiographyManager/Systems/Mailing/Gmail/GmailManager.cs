using System;
using System.Collections.Generic;
using System.Configuration;
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
				// create a request to retrieve the user's messages
				var request = service.Users.Messages.List(CurrentUser);

				// execute message listing request
				var messages = request.Execute().Messages;
				if ((messages?.Count ?? 0) == 0) return emails;

				foreach (var messageItem in messages)
				{
					try
					{
						// create a request to retrieve a single user message
						var messageRequest =
							service.Users.Messages.Get(CurrentUser, messageItem.Id);
						// execute message retrieval request
						var message = messageRequest.Execute();

						// get the formatted sender email from the message headers
						var sender =
							FormatFromToEmail(message.Payload.Headers
								                  .FirstOrDefault(h => h.Name.ContainsCaseInsensitive("from"))?.Value ??
							                  string.Empty);

						// no sender email = email is ignored
						if (sender?.Equals(ConfigurationManager.AppSettings[Constants.GmailApi.APPLICATION_EMAIL_CONFIG_FIELD]) ?? false) continue;

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

		/// <summary>
		/// Checks whether the email has an attachment.
		/// If it does, the email attachment fields (<see cref="Email.AttachmentId"/> && <see cref="Email.AttachmentExtension"/>) will be initialized.
		///
		/// <para>IMPORTANT: Only the first attachment will be taken into account</para>
		/// </summary>
		/// <param name="email"></param>
		/// <returns></returns>
		public bool CheckForAttachment(Email email)
		{
			if (email == null)
			{
				Logger.Log(Constants.Tags.GMAIL, "Attempted to check for attachments on a null e-mail.", LogType.Error);
				return false;
			}

			try
			{
				// create message retrieval request
				var messageRequest =
					service.Users.Messages.Get(Constants.GmailApi.CURRENT_USER, email.MessageId);

				// retrieve message parts upon retrieving the message
				var parts = messageRequest.Execute().Payload.Parts;
				foreach (var messagePart in parts)
				{
					// if the part has no filename, then it has no attachment
					if (string.IsNullOrEmpty(messagePart.Filename)) continue;

					// the email attachment name will be overridden, so the file extension needs to be saved
					email.AttachmentExtension =
						messagePart.Filename.Substring(messagePart.Filename.LastIndexOf('.') + 1);

					// the id of the attachment that can be retrieved for the email
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

		/// <summary>
		/// Saves the attachment corresponding to an email at the given path.
		///
		/// <para>Must have set the email's attachment ID and file extension (see <see cref="CheckForAttachment"/>)</para>
		/// </summary>
		/// <param name="email">the email to download the attachment for</param>
		/// <param name="path">the path to save the attachment at</param>
		/// <returns>true if the attachment was downloaded successfully, false otherwise</returns>
		public bool SaveAttachment(Email email, string path)
		{
			Logger.Log(Constants.Tags.GMAIL, "Saving attachment of email...");

			if (email == null)
			{
				Logger.Log(Constants.Tags.GMAIL, "Attempted to save the attachments of a null e-mail.", LogType.Error);
				return false;
			}

			if (string.IsNullOrEmpty(email.AttachmentId) && !CheckForAttachment(email))
			{
				Logger.Log(Constants.Tags.GMAIL,
					$"{email} has no attachments to save.", LogType.Warning);
				return false;
			}

			// get the part of the message that corresponds to the attachment
			var attachmentPart = service.Users.Messages.Attachments
				.Get(Constants.GmailApi.CURRENT_USER, email.MessageId, email.AttachmentId)
				.Execute();

			// convert from RFC 4648 base64 to base64url encoding
			// see http://en.wikipedia.org/wiki/Base64#Implementations_and_history
			var attachmentData = attachmentPart.Data.Replace('-', '+').Replace('_', '/');
			var data = Convert.FromBase64String(attachmentData);

			// save the attachment to disk
			email.AttachmentFileGuid = Guid.NewGuid();
			var filePath = Path.Combine(path, email.AttachmentFileGuid + "." + email.AttachmentExtension);
			File.WriteAllBytes(filePath, data);

			Logger.Log(Constants.Tags.GMAIL, $"Saved attachment at \"{filePath}\"");

			return true;
		}

		/// <summary>
		/// Sends the attachment associated with the email (at the given root path) to a recipient,
		/// with an email subject and body
		/// </summary>
		/// <param name="recipient">the email to send the attachment to</param>
		/// <param name="email">the email which has attachments (the attachment ID and the
		/// file extension must have been initialized and the file must have been downloaded beforehand - see <see cref="CheckForAttachment"/>, <see cref="SaveAttachment"/></param>
		/// <param name="attachmentRootPath">the root folder that the downloaded attachment is stored at</param>
		/// <param name="subject">the message subject</param>
		/// <param name="body">the message body</param>
		/// <returns>true if the attachment was successfully sent, false otherwise</returns>
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
				// using System.Net.Mail & MimeKit to create the email message
				var mail = new MailMessage
				{
					Subject = subject,
					Body = body,
					From = new MailAddress(ConfigurationManager.AppSettings[Constants.GmailApi.APPLICATION_EMAIL_CONFIG_FIELD]),
					IsBodyHtml = true
				};
				var attachmentPath = Path.Combine(attachmentRootPath,
					email.AttachmentFileGuid + "." + email.AttachmentExtension).Replace("/", "\\");
				mail.Attachments.Add(new Attachment(attachmentPath));
				mail.To.Add(new MailAddress(recipient));
				var mimeMessage = MimeMessage.CreateFromMailMessage(mail);

				// need to encode the mime message to base 64 so that it can be sent as an email
				var message = new Message
				{
					Raw = StringUtils.Base64UrlEncode(mimeMessage.ToString())
				};

				// perform the message send request using the Gmail API
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