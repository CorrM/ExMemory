using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExMemory.Helper
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

		internal static int GetDependenciesSize(ExOffset dependency, List<ExOffset> offsets)
		{
			// If it's Empty, Then It's Usually Dynamic Pointer (Like `Data` Member In `TArray`)
			List<ExOffset> dOffsets = offsets.Where(off => off.Dependency == dependency).ToList();
			if (!dOffsets.Any())
				return 0;

			// Get Biggest Offset
			int biggestOffset = dOffsets.Max(off => off.Offset);

			// Get Biggest Offset in size (Good for unions offsets)
			int biggestOffSize = dOffsets.Where(off => off.Offset == biggestOffset).Max(off => off.Size);

			// Get Offset
			ExOffset offset = offsets.Find(off => off.Dependency == dependency && off.Offset == biggestOffset && off.Size == biggestOffSize);

			// Get Size Of Data
			int valueSize = offset.OffsetType switch
			{
				OffsetType.String => ExMemorySharp.MaxStringLen,
				_ => offset.Size
			};

			return biggestOffset + valueSize;
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
