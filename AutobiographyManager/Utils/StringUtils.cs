using System;
using System.Globalization;

namespace Haraven.Autobiographies.Utils
{
	public static class StringUtils
	{
		/// <summary>
		/// Checks whether the given string contains the search term, case-insensitive
		/// </summary>
		/// <param name="str">the string to search in</param>
		/// <param name="searchTerm">the term to search for</param>
		/// <returns></returns>
		public static bool ContainsCaseInsensitive(this string str, string searchTerm)
		{
			return CultureInfo.CurrentCulture.CompareInfo.IndexOf(str, searchTerm,
				CompareOptions.IgnoreCase) >= 0;
		}

		/// <summary>
		/// Encodes a string in email format
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static string Base64UrlEncode(string input)
		{
			var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
			return Convert.ToBase64String(inputBytes)
				.Replace('+', '-')
				.Replace('/', '_')
				.Replace("=", "");
		}
	}
}