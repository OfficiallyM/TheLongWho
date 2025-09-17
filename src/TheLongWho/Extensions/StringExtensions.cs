using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TheLongWho.Extensions
{
	public static class StringExtensions
	{
		public static string ToMachineName(this string input)
		{
			return input.Replace(" ", "_").ToLowerInvariant();
		}

		public static string Prettify(this string input)
		{
			return input.Replace("(Clone)", "").Trim();
		}
	}
}
