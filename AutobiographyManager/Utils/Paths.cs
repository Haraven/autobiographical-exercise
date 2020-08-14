namespace Haraven.Autobiographies.Utils
{
	public static class Paths
	{
		/// <summary>
		/// The root folder for all configuration files
		/// </summary>
		public const string CONFIG_ROOT_PATH = "config/";
		/// <summary>
		/// The Gmail credentials as downloaded from the dev console
		/// </summary>
		public const string GMAIL_CREDENTIALS_PATH = CONFIG_ROOT_PATH + "credentials.json";
		/// <summary>
		/// The Gmail token saved after logging in for the first time (needs to be deleted if <see cref="Constants.GmailApi.SCOPES"/> is changed)
		/// </summary>
		public const string GMAIL_TOKEN_PATH = CONFIG_ROOT_PATH + "token.json";

		/// <summary>
		/// The root folder for all email attachments
		/// </summary>
		public const string ATTACHMENTS_ROOT_PATH = "attachments/";
		/// <summary>
		/// The root folder for autobiography attachments
		/// </summary>
		public const string AUTOBIOGRAPHIES_PATH = ATTACHMENTS_ROOT_PATH + "autobiographies/";
		/// <summary>
		/// The root folder for feedback attachments
		/// </summary>
		public const string FEEDBACK_PATH = ATTACHMENTS_ROOT_PATH + "feedback/";

		/// <summary>
		/// The root folder to save persistent application data
		/// </summary>
		public const string DATA_ROOT_PATH = "data/";
		/// <summary>
		/// The file containing all registered users to be used by the application
		/// </summary>
		public const string REGISTERED_USERS_PATH = DATA_ROOT_PATH + "registered-users.json";
		/// <summary>
		/// The file containing all autobiography pairings that are stored by the application
		/// </summary>
		public const string PAIRING_DATA_FILE = DATA_ROOT_PATH + "pairings.json";
	}
}