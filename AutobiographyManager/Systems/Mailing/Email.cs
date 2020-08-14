using System;

namespace Haraven.Autobiographies
{
	/// <summary>
	/// Base wrapper for emails received via the <see cref="GmailManager"/>
	/// </summary>
	public class Email
	{
		/// <summary>
		/// The title of the email
		/// </summary>
		public string Title { get; set; }
		/// <summary>
		/// The email sender (just the email part, not the name)
		/// </summary>
		public string Sender { get; set; }

		/// <summary>
		/// Used by the <see cref="GmailManager"/> to track attachments for an email
		/// </summary>
		public string AttachmentId { get; set; }
		/// <summary>
		/// Used by the <see cref="GmailManager"/> to track email messages
		/// </summary>
		public string MessageId { get; set; }

		/// <summary>
		/// The unique identifier for a downloaded attachment
		/// </summary>
		public Guid AttachmentFileGuid { get; set; }

		/// <summary>
		/// The extension of the attachment (typically pdf)
		/// </summary>
		public string AttachmentExtension { get; set; }

		public override string ToString()
		{
			return $"{{ from: {Sender}, Subject: {Title} }}";
		}
	}
}