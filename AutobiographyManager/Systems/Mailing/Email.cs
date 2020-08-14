using System;

namespace Haraven.Autobiographies
{
	public class Email
	{
		public string Title { get; set; }
		public string Sender { get; set; }

		public string AttachmentId { get; set; }
		public bool HasAttachment => AttachmentId != null;
		public bool AreAttachmentsInitialized { get; set; }

		public string MessageId { get; set; }
		public Guid AttachmentFileGuid { get; set; }
		public string AttachmentExtension { get; set; }

		public override string ToString()
		{
			return $"{{ from: {Sender}, Subject: {Title} }}";
		}
	}
}