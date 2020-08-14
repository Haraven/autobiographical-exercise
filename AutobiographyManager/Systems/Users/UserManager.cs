using System;
using System.Collections.Generic;
using System.IO;
using Haraven.Autobiographies.Utils;
using Newtonsoft.Json;

namespace Haraven.Autobiographies
{
	public class UserManager
	{
		public static UserManager Instance { get; private set; }
		public List<string> Users { get; private set; } = new List<string>();

		public UserManager(string registeredUsersPath)
		{
			Instance = this;

			ReadRegisteredUsers(registeredUsersPath);
		}

		private void ReadRegisteredUsers(string registeredUsersPath)
		{
			try
			{
				Logger.Log(Constants.Tags.USERS, $"Reading all registered users from {registeredUsersPath}...");

				var content = File.ReadAllText(registeredUsersPath);

				Users = JsonConvert.DeserializeObject<List<string>>(content);

				Logger.Log(Constants.Tags.USERS, "Read all registered users successfully");
			}
			catch (Exception e)
			{
				Logger.LogException(Constants.Tags.USERS, e);
			}
		}
	}
}