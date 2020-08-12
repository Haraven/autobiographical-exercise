using System;
using System.Globalization;

namespace Haraven.Autobiographies.Utils
{
	public static class StringUtils
	{
		public static bool ContainsCaseInsensitive(this string str, string searchTerm)
		{
			return CultureInfo.CurrentCulture.CompareInfo.IndexOf(str, searchTerm,
				CompareOptions.IgnoreCase) >= 0;
		}

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