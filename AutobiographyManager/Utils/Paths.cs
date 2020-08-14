namespace Haraven.Autobiographies.Utils
{
	public static class Paths
	{
		public const string CONFIG_ROOT_PATH = "config/";
		public const string GMAIL_CREDENTIALS_PATH = CONFIG_ROOT_PATH + "credentials.json";
		public const string GMAIL_TOKEN_PATH = CONFIG_ROOT_PATH + "token.json";

		public const string ATTACHMENTS_ROOT_PATH = "attachments/";
		public const string AUTOBIOGRAPHIES_PATH = ATTACHMENTS_ROOT_PATH + "autobiographies/";
		public const string FEEDBACK_PATH = ATTACHMENTS_ROOT_PATH + "feedback/";

		public const string DATA_ROOT_PATH = "data/";
		public const string REGISTERED_USERS_PATH = DATA_ROOT_PATH + "registered-users.json";
		public const string PAIRING_DATA_FILE = DATA_ROOT_PATH + "pairings.json";
	}
}