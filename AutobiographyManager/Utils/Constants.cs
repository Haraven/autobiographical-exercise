﻿using System.Drawing;
using Google.Apis.Gmail.v1;

namespace Haraven.Autobiographies.Utils
{
	public static class Constants
	{
		public const string AUTOBIOGRAPHY_MAIL_TAG = "autobiografie";
		public const string FEEDBACK_MAIL_TAG = "feedback";
		public const double EMAIL_CHECKING_INTERVAL = 1d;

		public static class GmailApi
		{
			public static readonly string[] SCOPES = {GmailService.Scope.GmailModify};
			public const string APPLICATION_NAME = "AutobiographyManager";
			public const string CURRENT_USER = "me";
			public const string APPLICATION_EMAIL_CONFIG_FIELD = "applicationEmail";
		}

		public static class Logging
		{
			public static readonly Color THREAD_MESSAGE_COLOR = Color.Aquamarine;
			public static readonly Color INFO_COLOR = Color.DarkGray;
			public static readonly Color ERROR_COLOR = Color.DarkRed;
			public static readonly Color WARNING_COLOR = Color.DarkOrange;
			public static readonly Color DETAILS_COLOR = Color.CadetBlue;

			public const string OUTPUT_LOG_FILENAME = "output.log";
			public const int NEW_LINE_FLUSH_COUNT = 5;
		}

		public static class Tags
		{
			public const string SYSTEM = "system";
			public const string THREADING = "task";

			public const string GMAIL = "gmail-manager";
			public const string SELF_KNOWLEDGE = "self-knowledge-manager";
			public const string USERS = "user-manager";
		}
	}
}