using System;
using System.Runtime.InteropServices;
using ExternalMemory.Helper;
using ExternalMemory.Types;

namespace ExternalMemory
{
	public abstract class ExOffset
	{
		internal Type MarshalType;

		internal Type ExternalValueType { get; set; }
		internal OffsetType OffType { get; set; }
		internal object Value { get; set; }

		public UIntPtr OffsetAddress { get; internal set; }
		public ExKind ExternalType { get; internal set; }
		public int Offset { get; internal set; }
		public int Size { get; internal set; }
		public ReadOnlyMemory<byte> ValueBytes { get; internal set; }

		internal object GetValueFromBytes(ReadOnlySpan<byte> offsetBytes)
		{
			switch (OffType)
			{
				case OffsetType.IntPtr:
				case OffsetType.ExClass when ExternalType == ExKind.Pointer:
					return Helper.MarshalType.ByteArrayToObject(MarshalType, offsetBytes);

				case OffsetType.ExClass:
					// The value is ByteArray it's for Offsets inside the ExternalClass
					return ValueBytes.ToArray();

				case OffsetType.ValueType:
				default:
					return Helper.MarshalType.ByteArrayToObject(MarshalType, offsetBytes);
			}
		}
		internal void RemoveValueAndData()
		{
			Value = default;
			ValueBytes = ReadOnlyMemory<byte>.Empty;
		}
		internal void AssignDefaultExternalValue()
		{
			Value = Activator.CreateInstance(ExternalValueType);
		}
	}

	public sealed class ExOffset<T> : ExOffset
	{
		public new T Value => (T)base.Value;

		private ExOffset(int offset, OffsetType offType, ExKind exType)
		{
			Offset = offset;
			OffType = offType;
			ExternalType = exType;

			Init();
		}
		public ExOffset(int offset, ExKind exType) : this(offset, OffsetType.ExClass, exType)
		{
			if (!typeof(T).IsSubclassOf(typeof(ExClass)))
				throw new InvalidCastException("This Constructor For `ExternalClass` Types Only.");
		}
		public ExOffset(int offset) : this(offset, OffsetType.ValueType, ExKind.Instance)
		{
			if (typeof(T).IsSubclassOf(typeof(ExClass)))
				throw new InvalidCastException("Use Other Constructor For `ExternalClass` Types.");
		}

		private void Init()
		{
			Type thisType = typeof(T);

			if (thisType == typeof(IntPtr) || thisType == typeof(UIntPtr))
			{
				OffType = OffsetType.IntPtr;
				MarshalType = typeof(UIntPtr);
				Size = ExMemory.PointerSize;
				base.Value = default;
			}
			else if (thisType.IsSubclassOf(typeof(ExClass)) || thisType.IsSubclassOfRawGeneric(typeof(ExOffset<>)))
			{
				// OffType Set On Other Constructor If It's `ExternalClass`
				if (OffType != OffsetType.ExClass)
					throw new Exception("Use Other Constructor For `ExternalClass` Types.");

				MarshalType = ExternalType == ExKind.Pointer ? typeof(UIntPtr) : thisType;
				ExternalValueType = thisType;
				base.Value = Activator.CreateInstance<T>();
				Size = ExternalType == ExKind.Pointer ? ExMemory.PointerSize : ((ExClass)base.Value).ClassSize;
			}
			else
			{
				MarshalType = thisType;
				Size = Marshal.SizeOf<T>();
				base.Value = default;
			}
		}
		public bool Write(T value)
		{
			if (OffsetAddress == UIntPtr.Zero)
				return false;

			bool written = ExMemory.WriteBytes(OffsetAddress, Helper.MarshalType.ObjectToByteArray(MarshalType, Value));
			if (written)
				base.Value = value;

			return written;
		}
	}
}
