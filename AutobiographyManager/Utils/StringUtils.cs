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
	}
}