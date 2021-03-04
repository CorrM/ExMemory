using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ExternalMemory.Helper
{
	internal readonly ref struct MarshallerInfo
	{
		/// <summary>
		/// Gets if the type can be stored in a registers (for example ACX, ECX, ...).
		/// </summary>
		public bool CanBeStoredInRegisters { get; init; }

		/// <summary>
		/// State if the type is <see cref="IntPtr"/>.
		/// </summary>
		public bool IsIntPtr { get; init; }

		/// <summary>
		/// The size of the type.
		/// </summary>
		public int Size { get; init; }

		/// <summary>
		/// The TypeCode of the type.
		/// </summary>
		public TypeCode TypeCode { get; init; }

		public static MarshallerInfo MakeInfo(Type type)
		{
			bool isIntPtr = type == typeof(IntPtr);
			TypeCode typeCode = Type.GetTypeCode(type);
			bool canBeStoredInRegisters = isIntPtr
										  || typeCode == TypeCode.Int64
										  || typeCode == TypeCode.UInt64
										  || typeCode == TypeCode.Boolean
										  || typeCode == TypeCode.Byte
										  || typeCode == TypeCode.Char
										  || typeCode == TypeCode.Int16
										  || typeCode == TypeCode.Int32
										  || typeCode == TypeCode.SByte
										  || typeCode == TypeCode.Single
										  || typeCode == TypeCode.UInt16
										  || typeCode == TypeCode.UInt32;

			return new MarshallerInfo()
			{
				IsIntPtr = isIntPtr,
				TypeCode = typeCode,
				CanBeStoredInRegisters = canBeStoredInRegisters,
				Size = typeCode == TypeCode.Boolean ? 1 : Marshal.SizeOf(type)
			};
		}
	}

	/// <summary>
	/// Static class providing tools for extracting information related to types.
	/// </summary>
	public static class MarshalType
	{
		/// <summary>
		/// Marshals a managed object to an array of bytes.
		/// </summary>
		/// <param name="type">Type to convert to byte array</param>
		/// <param name="obj">The object to marshal.</param>
		/// <returns>A array of bytes corresponding to the managed object.</returns>
		public static ReadOnlySpan<byte> ObjectToByteArray(Type type, object obj)
		{
			var mInfo = MarshallerInfo.MakeInfo(type);

			// We'll tried to avoid marshalling as it really slows the process
			// First, check if the type can be converted without marshalling
			try
			{
				switch (mInfo.TypeCode)
				{
					case TypeCode.Object:
						if (mInfo.IsIntPtr)
						{
							switch (mInfo.Size)
							{
								case 4:
									return BitConverter.GetBytes(((IntPtr)obj).ToInt32());
								case 8:
									return BitConverter.GetBytes(((IntPtr)obj).ToInt64());
							}
						}
						break;
					case TypeCode.Boolean:
						return BitConverter.GetBytes((bool)obj);
					case TypeCode.Char:
						return Encoding.UTF8.GetBytes(new[] { (char)obj });
					case TypeCode.Double:
						return BitConverter.GetBytes((double)obj);
					case TypeCode.Int16:
						return BitConverter.GetBytes((short)obj);
					case TypeCode.Int32:
						return BitConverter.GetBytes((int)obj);
					case TypeCode.Int64:
						return BitConverter.GetBytes((long)obj);
					case TypeCode.Single:
						return BitConverter.GetBytes((float)obj);
					case TypeCode.String:
						throw new InvalidCastException("This method doesn't support string conversion.");
					case TypeCode.UInt16:
						return BitConverter.GetBytes((ushort)obj);
					case TypeCode.UInt32:
						return BitConverter.GetBytes((uint)obj);
					case TypeCode.UInt64:
						return BitConverter.GetBytes((ulong)obj);
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine(e);
			}
			
			// Check if it's not a common type
			// Allocate a block of unmanaged memory
			using var unmanaged = new LocalUnmanagedMemory(mInfo.Size);

			// Write the object inside the unmanaged memory
			unmanaged.Write(obj);

			// Return the content of the block of unmanaged memory
			return unmanaged.Read();
		}

		/// <summary>
		/// Marshals an array of byte to a managed object.
		/// </summary>
		/// <param name="type">Type to convert from byte array</param>
		/// <param name="byteArray">The array of bytes corresponding to a managed object.</param>
		/// <param name="index">[Optional] Where to start the conversion of bytes to the managed object.</param>
		/// <returns>A managed object.</returns>
		public static object ByteArrayToObject(Type type, ReadOnlySpan<byte> byteArray, int index = 0)
		{
			// Todo: https://stackoverflow.com/questions/54537522/copy-byte-array-into-generic-type-without-boxing
			var mInfo = MarshallerInfo.MakeInfo(type);

			// We'll tried to avoid marshalling as it really slows the process
			// First, check if the type can be converted without marshalling
			try
			{
				switch (mInfo.TypeCode)
				{
					case TypeCode.Object:
						if (mInfo.IsIntPtr)
						{
							switch (byteArray.Length)
							{
								case 1:
									return new IntPtr(byteArray[index]);

								case 2:
									return new IntPtr(BitConverter.ToInt32(stackalloc byte[4] { byteArray[index], byteArray[index + 1], 0, 0 }));

								case 4:
									return new IntPtr(BitConverter.ToInt32(byteArray[index..]));

								case 8:
									return new IntPtr(BitConverter.ToInt64(byteArray[index..]));
							}
						}
						break;
					case TypeCode.Boolean:
						return BitConverter.ToBoolean(byteArray[index..]);

					case TypeCode.Byte:
						return byteArray[index];

					case TypeCode.Char:
						return Encoding.UTF8.GetString(byteArray)[index];

					case TypeCode.Double:
						return BitConverter.ToDouble(byteArray[index..]);

					case TypeCode.Int16:
						return BitConverter.ToInt16(byteArray[index..]);

					case TypeCode.Int32:
						return BitConverter.ToInt32(byteArray[index..]);

					case TypeCode.Int64:
						return BitConverter.ToInt64(byteArray[index..]);

					case TypeCode.Single:
						return BitConverter.ToSingle(byteArray[index..]);

					case TypeCode.String:
						throw new InvalidCastException("This method doesn't support string conversion.");

					case TypeCode.UInt16:
						return BitConverter.ToUInt16(byteArray[index..]);

					case TypeCode.UInt32:
						return BitConverter.ToUInt32(byteArray[index..]);

					case TypeCode.UInt64:
						return BitConverter.ToUInt64(byteArray[index..]);
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine(e);
			}

			// Allocate a block of unmanaged memory
			using var unmanaged = new LocalUnmanagedMemory(mInfo.Size);

			// Write the array of bytes inside the unmanaged memory
			unmanaged.Write(byteArray, index);

			// Return a managed object created from the block of unmanaged memory
			return unmanaged.Read(type);
		}

		/// <summary>
		/// Get size of type.
		/// </summary>
		/// <param name="type">Type to get size of.</param>
		/// <returns>Size of <paramref name="type"/></returns>
		public static int GetSizeOfType(Type type)
		{
			return MarshallerInfo.MakeInfo(type).Size;
		}
	}
}
