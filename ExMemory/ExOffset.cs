using System;
using System.Runtime.InteropServices;
using ExternalMemory.Helper;
using ExternalMemory.Types;

namespace ExternalMemory
{
	public abstract class ExOffset
	{
		internal Type MarshalType { get; set; }

		internal string Name { get; set; }
		internal ExClass Parent { get; set; }
		internal Type ExternalValueType { get; set; }
		internal OffsetType OffType { get; set; }
		internal object Value { get; set; }
		internal object ValuePtrAsObj { get; set; }

		public UIntPtr OffsetAddress { get; internal set; }
		public ExKind ExternalType { get; internal set; }
		public int Offset { get; internal set; }
		public int Size { get; internal set; }
		public ReadOnlyMemory<byte> ValueBytes { get; internal set; }

		internal abstract void Init();
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

		public T PtrAsEx<T>() where T : ExClass, new()
		{
			if (ValuePtrAsObj is not null)
				return (T)ValuePtrAsObj;

			if (OffType != OffsetType.IntPtr)
				throw new ArgumentException($"'{Name}' is not a pointer.", Name);

			ValuePtrAsObj = new T();

			ExClass exClass = (T)ValuePtrAsObj;
			exClass.Address = (UIntPtr)Value;

			exClass.UpdateData();

			return (T)ValuePtrAsObj;
		}
	}

	public sealed class ExOffset<T> : ExOffset
	{
		public new T Value => (T)base.Value;

		public ExOffset(int offset, ExKind exType)
		{
			var offType = OffsetType.ExClass;
			if (!typeof(T).IsSubclassOf(typeof(ExClass)) && !typeof(T).IsSubclassOfRawGeneric(typeof(ExOffset<>)))
				offType = OffsetType.ValueType;

			Offset = offset;
			ExternalType = exType;
			OffType = offType;
		}
		public ExOffset(int offset)
		{
			if (typeof(T).IsSubclassOf(typeof(ExClass)) || typeof(T).IsSubclassOfRawGeneric(typeof(ExOffset<>)))
				throw new InvalidCastException("Use another constructor for `ExClass` types.");

			Offset = offset;
			ExternalType = ExKind.Instance;
			OffType = OffsetType.ValueType;
		}

		internal override void Init()
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
				if (OffType != OffsetType.ExClass)
					throw new Exception("Use another constructor for `ExClass` types.");

				ExternalValueType = thisType;

				if (ExternalType == ExKind.Pointer)
				{
					base.Value = null;
					MarshalType = typeof(UIntPtr);
					Size = ExMemory.PointerSize;
				}
				else
				{
					base.Value = Activator.CreateInstance<T>();
					MarshalType = thisType;
					Size = ((ExClass)base.Value).ClassSize;
				}
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
