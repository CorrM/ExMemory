using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ExMemory.Helper
{
	/// <summary>
	/// Static class providing tools for extracting information related to types.
	/// </summary>
	public class MarshalType
	{
		/// <summary>
		/// Gets if the type can be stored in a registers (for example ACX, ECX, ...).
		/// </summary>
		public bool CanBeStoredInRegisters { get; }
		/// <summary>
		/// State if the type is <see cref="IntPtr"/>.
		/// </summary>
		public bool IsIntPtr { get; }
		/// <summary>
		/// The real type.
		/// </summary>
		public Type RealType { get; }
		/// <summary>
		/// The size of the type.
		/// </summary>
		public int Size { get; }
		/// <summary>
		/// The TypeCode of the type.
		/// </summary>
		public TypeCode TypeCode { get; }

		/// <summary>
		/// Initializes static information related to the specified type.
		/// </summary>
		public MarshalType(Type type)
		{
			// Gather information related to the provided type
			IsIntPtr = type == typeof(IntPtr);
			RealType = type;
			Size = TypeCode == TypeCode.Boolean ? 1 : Marshal.SizeOf(RealType);
			TypeCode = Type.GetTypeCode(RealType);

			// Check if the type can be stored in registers
			CanBeStoredInRegisters = IsIntPtr
									 || TypeCode == TypeCode.Int64
									 || TypeCode == TypeCode.UInt64
									 || TypeCode == TypeCode.Boolean
									 || TypeCode == TypeCode.Byte
									 || TypeCode == TypeCode.Char
									 || TypeCode == TypeCode.Int16
									 || TypeCode == TypeCode.Int32
									 || TypeCode == TypeCode.Int64
									 || TypeCode == TypeCode.SByte
									 || TypeCode == TypeCode.Single
									 || TypeCode == TypeCode.UInt16
									 || TypeCode == TypeCode.UInt32;
		}

		/// <summary>
		/// Marshals a managed object to an array of bytes.
		/// </summary>
		/// <param name="obj">The object to marshal.</param>
		/// <returns>A array of bytes corresponding to the managed object.</returns>
		public ReadOnlySpan<byte> ObjectToByteArray(object obj)
		{
			// We'll tried to avoid marshalling as it really slows the process
			// First, check if the type can be converted without marshalling
			try
			{
				switch (TypeCode)
				{
					case TypeCode.Object:
						if (IsIntPtr)
						{
							switch (Size)
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
			using var unmanaged = new LocalUnmanagedMemory(Size);

			// Write the object inside the unmanaged memory
			unmanaged.Write(obj);

			// Return the content of the block of unmanaged memory
			return unmanaged.Read();
		}

		/// <summary>
		/// Marshals an array of byte to a managed object.
		/// </summary>
		/// <param name="byteArray">The array of bytes corresponding to a managed object.</param>
		/// <param name="index">[Optional] Where to start the conversion of bytes to the managed object.</param>
		/// <returns>A managed object.</returns>
		public object ByteArrayToObject(ReadOnlySpan<byte> byteArray, int index = 0)
		{
			// We'll tried to avoid marshalling as it really slows the process
			// First, check if the type can be converted without marshalling

			try
			{
				switch (TypeCode)
				{
					case TypeCode.Object:
						if (IsIntPtr)
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

								default:
									break;
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
			using var unmanaged = new LocalUnmanagedMemory(Size);

			// Write the array of bytes inside the unmanaged memory
			unmanaged.Write(byteArray, index);

			// Return a managed object created from the block of unmanaged memory
			return unmanaged.Read(RealType);
		}
	}
}
