using System;
using System.Collections.Generic;
using ExternalMemory.Helper;

namespace ExternalMemory
{
	public static class ExMemory
	{
		#region [ Delegates ]

		public delegate bool ReadCallBack(UIntPtr address, uint size, out ReadOnlySpan<byte> bytes);
		public delegate bool WriteCallBack(UIntPtr address, ReadOnlySpan<byte> bytes);

		#endregion

		public static ReadCallBack ReadBytesCallBack { get; private set; }
		public static WriteCallBack WriteBytesCallBack { get; private set; }
		public static int MaxStringLen { get; set; } = 64;
		public static bool Is64BitMemory { get; private set; }
		public static int PointerSize { get; private set; }

		public static void Init(ReadCallBack readBytesDelegate, WriteCallBack writeBytesDelegate, bool is64BitMemory)
		{
			ReadBytesCallBack = readBytesDelegate;
			WriteBytesCallBack = writeBytesDelegate;
			Is64BitMemory = is64BitMemory;
			PointerSize = Is64BitMemory ? 0x8 : 0x4;
		}

		private static void RemoveValueData(IEnumerable<ExOffset> unrealOffsets)
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

		internal static bool ReadClass<T>(T instance, ReadOnlySpan<byte> fullClassBytes) where T : ExClass
		{
			// Set Bytes
			instance.FullClassBytes = fullClassBytes.ToArray();

			// Read Offsets
			foreach (ExOffset offset in instance.Offsets)
			{
				#region [ Checks ]

				if (offset.Dependency is not null && offset.Dependency.OffsetType != OffsetType.UIntPtr && offset.Dependency != ExOffset.None)
					throw new ArgumentException("Dependency can only be pointer (UIntPtr) or 'ExOffset.None'");
				
				#endregion

				#region [ SetValue ]

				// if it's Base Offset
				if (offset.Dependency == ExOffset.None)
				{
					offset.SetValueBytes(instance.FullClassBytes.Span);
					offset.OffsetAddress = instance.BaseAddress + offset.Offset;
				}
				else if (offset.Dependency != null && offset.Dependency.DataAssigned)
				{
					offset.SetValueBytes(offset.Dependency.FullClassData.Span);
					offset.OffsetAddress += offset.Offset;
				}
				// Dependency Is Null-Pointer OR Bad Pointer Then Just Skip
				else if (offset.Dependency != null && (offset.Dependency.OffsetType == OffsetType.UIntPtr && !offset.Dependency.DataAssigned))
				{
					continue;
				}
				else
				{
					throw new Exception("Dependency Data Not Set !!");
				}

				#endregion

				#region [ Init For Dependencies ]

				// If It's Pointer, Read Pointed Data To Use On Other Offset Dependent On It
				if (offset.OffsetType == OffsetType.UIntPtr)
				{
					// Get Size Of Pointed Data
					int pointedSize = Utils.GetDependenciesSize(offset, instance.Offsets);

					// If Size Is Zero Then It's Usually Dynamic (Unknown Size) Pointer (Like `Data` Member In `TArray`)
					// Or Just An Pointer Without Dependencies
					if (pointedSize == 0)
						continue;

					// Set Base Address, So i can set correct address for Dependencies offsets `else if (offset.Dependency.DataAssigned)` UP.
					// So i just need to add offset to that address
					offset.OffsetAddress = offset.Read<UIntPtr>();

					// Can't Read Bytes
					if (!ReadBytes(offset.Read<UIntPtr>(), (uint)pointedSize, out ReadOnlySpan<byte> dataBytes))
						continue;

					offset.SetData(dataBytes);
				}

				// Nested External Class
				else if (offset.OffsetType == OffsetType.ExternalClass)
				{
					if (offset.ExternalClassIsPointer)
					{
						// Get Address Of Nested Class
						var valPtr = offset.Read<UIntPtr>();

						// Set Class Info
						offset.ExternalClassObject.BaseAddress = valPtr;

						// Null Pointer
						if (valPtr == UIntPtr.Zero)
							continue;

						// Read Nested Pointer Class
						if (!ReadClass(offset.ExternalClassObject))
						{
							// throw new Exception($"Can't Read `{offset.ExternalClassType.Name}` As `ExternalClass`.", new Exception($"Value Count = {offset.Size}"));
							return false;
						}
					}
					else
					{
						// Set Class Info
						offset.ExternalClassObject.BaseAddress += offset.Offset;

						// Read Nested Instance Class
						if (!ReadClass(offset.ExternalClassObject, (byte[])offset.Value))
						{
							// throw new Exception($"Can't Read `{offset.ExternalClassType.Name}` As `ExternalClass`.", new Exception($"Value Count = {offset.Size}"));
							return false;
						}
					}
				}

				#endregion
			}

			return true;
		}
		public static bool ReadClass<T>(T instance) where T : ExClass
		{
			// Read Full Class
			if (ReadBytes(instance.BaseAddress, (uint) instance.ClassSize, out ReadOnlySpan<byte> fullClassBytes))
				return ReadClass(instance, fullClassBytes);

			// Clear All Class Offset
			RemoveValueData(instance.Offsets);
			return false;
		}

		public static bool ReadClass<T>(T instance, int address) where T : ExClass => ReadClass(instance);
		public static bool ReadClass<T>(T instance, long address) where T : ExClass => ReadClass(instance);
	}
}
