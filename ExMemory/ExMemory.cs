using System;
using System.Collections.Generic;
using ExternalMemory.Types;

namespace ExternalMemory
{
	public static class ExMemory
	{
		#region [ Delegates ]

		public delegate bool ReadCallBack(UIntPtr address, uint size, out ReadOnlySpan<byte> bytes);
		public delegate bool WriteCallBack(UIntPtr address, ReadOnlySpan<byte> bytes);

		#endregion

		public static bool IsInit { get; private set; }
		public static ReadCallBack ReadBytesCallBack { get; private set; }
		public static WriteCallBack WriteBytesCallBack { get; private set; }
		public static bool Is64BitMemory { get; private set; }
		public static int PointerSize { get; private set; }

		public static void Init(ReadCallBack readBytesDelegate, WriteCallBack writeBytesDelegate, bool is64BitMemory)
		{
			ReadBytesCallBack = readBytesDelegate;
			WriteBytesCallBack = writeBytesDelegate;
			Is64BitMemory = is64BitMemory;
			PointerSize = Is64BitMemory ? 0x8 : 0x4;
			IsInit = true;
		}

		internal static void RemoveValueData(IEnumerable<ExOffset> unrealOffsets)
		{
			foreach (ExOffset unrealOffset in unrealOffsets)
				unrealOffset.RemoveValueAndData();
		}
		internal static bool ReadBytes(UIntPtr address, uint size, out ReadOnlySpan<byte> bytes)
		{
			bytes = ReadOnlySpan<byte>.Empty;

			if (address == UIntPtr.Zero)
				return false;

			return ReadBytesCallBack?.Invoke(address, size, out bytes) ?? false;
		}
		internal static bool WriteBytes(UIntPtr address, ReadOnlySpan<byte> bytes)
		{
			return WriteBytesCallBack?.Invoke(address, bytes) ?? false;
		}

		internal static bool ProcessClass<T>(T instance, ReadOnlySpan<byte> fullClassBytes) where T : ExClass
		{
			// Set Bytes
			instance.FullClassBytes = fullClassBytes.ToArray();

			// Read Offsets
			foreach (ExOffset offset in instance.Offsets)
			{
				offset.OffsetAddress = instance.Address + offset.Offset;
				offset.ValueBytes = instance.FullClassBytes.Slice(offset.Offset, offset.Size);

				if (offset.OffType != OffsetType.ExClass)
				{
					offset.Value = offset.GetValueFromBytes(offset.ValueBytes.Span);
					continue;
				}

				// Nested external class pointer
				if (offset.ExternalType == ExKind.Pointer)
				{
					// Pointer read as IntPtr,
					var valPtr = (UIntPtr)offset.GetValueFromBytes(offset.ValueBytes.Span);

					// offset.AssignDefaultExternalValue();
					if (offset.Value is not ExClass exOffset)
						throw new InvalidOperationException($"Can't create instance of '{offset.GetType().Name}'.");

					// Set Class Info
					exOffset.Address = valPtr;

					// Null Pointer
					if (valPtr == UIntPtr.Zero)
						continue;

					// Read Nested Pointer Class
					if (!exOffset.UpdateData())
					{
						// throw new Exception($"Can't Read `{offset.ExternalClassType.Name}` As `ExternalClass`.", new Exception($"Value Count = {offset.Size}"));
						return false;
					}
				}

				// Nested external class instance
				else
				{
					if (offset.Value is not ExClass exOffset)
						throw new InvalidOperationException($"Can't create instance of '{offset.GetType().Name}'.");

					// Set Class Info
					exOffset.Address += offset.Offset;

					// Read Nested Instance Class
					if (!((T)exOffset).UpdateData(offset.ValueBytes.Span))
					{
						// throw new Exception($"Can't Read `{offset.ExternalClassType.Name}` As `ExternalClass`.", new Exception($"Value Count = {offset.Size}"));
						return false;
					}
				}
			}

			return true;
		}
	}
}
