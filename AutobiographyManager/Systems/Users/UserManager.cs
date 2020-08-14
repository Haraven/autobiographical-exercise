using System;
using System.Collections.Generic;
using System.IO;
using Haraven.Autobiographies.Utils;
using Newtonsoft.Json;

namespace Haraven.Autobiographies
{
	/// <summary>
	/// Class that manages all registered users.
	/// For now, performs a simple reading of a
	/// config file and exposes its contents in
	/// <see cref="Users"/>.
	/// </summary>
	public class UserManager
	{
		/// <summary>
		/// The current <see cref="UserManager"/> instance.
		/// </summary>
		public static UserManager Instance { get; private set; }

		/// <summary>
		/// All the registered users that can send and receive emails
		/// </summary>
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