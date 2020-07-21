namespace Haraven.Autobiographies
{
	public class UserManager
	{
		public static UserManager Instance { get; private set; }

		public UserManager(string registeredUserPath)
		{
			Instance = this;
		}
	}
}