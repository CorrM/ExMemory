using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExternalMemory.Helper
{
	public static class Utils
	{
		/// <summary>
		/// Alternative version of <see cref="Type.IsSubclassOf"/> that supports raw generic types (generic types without any type parameters).
		/// </summary>
		/// <param name="baseType">The base type class for which the check is made.</param>
		/// <param name="toCheck">To type to determine for whether it derives from <paramref name="baseType"/>.</param>
		public static bool IsSubclassOfRawGeneric(this Type toCheck, Type baseType)
		{
			while (toCheck != typeof(object))
			{
				Type cur = toCheck != null && toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
				if (baseType == cur)
					return true;

				if (toCheck != null)
					toCheck = toCheck.BaseType;
			}

			return false;
		}

		internal static int GetClassSize(List<ExOffset> offsets)
		{
			if (offsets.Count == 0)
				return 0;

			// Get Biggest Offset
			int biggestOffset = offsets.Max(off => off.Offset);

			// Get Biggest Offset in size (Good for unions offsets)
			int biggestOffSize = offsets.Where(off => off.Offset == biggestOffset).Max(off => off.Size);

			// Get Offset
			ExOffset offset = offsets.Find(off => off.Offset == biggestOffset && off.Size == biggestOffSize);
			if (offset is null)
				throw new NullReferenceException("'offset' can not be null.");

			return biggestOffset + offset.Size;
		}

		public static string GetStringFromBytes(ReadOnlySpan<byte> bytes, bool isUnicode)
		{
			return Utils.BytesToString(bytes, isUnicode).Split('\0')[0];
		}
		public static byte[] StringToBytes(string str, bool isUnicode)
		{
			return isUnicode ? Encoding.Unicode.GetBytes(str) : Encoding.ASCII.GetBytes(str);
		}
		public static string BytesToString(ReadOnlySpan<byte> strBytes, bool isUnicode)
		{
			return isUnicode ? Encoding.Unicode.GetString(strBytes) : Encoding.ASCII.GetString(strBytes);
		}
	}
}
