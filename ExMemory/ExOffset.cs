using System;
using System.Runtime.InteropServices;
using ExternalMemory.Helper;

namespace ExternalMemory
{
	public enum OffsetType
	{
		ValueType,
		ExClass,
		IntPtr,
		String
	}
	public enum ExType
	{
		Instance,
		Pointer
	}

	public abstract class ExOffset
	{
		public static ExOffset None { get; } = new ExOffset<byte>(null, 0x0, OffsetType.None);

		#region [ Proparites ]

		internal UIntPtr OffsetAddress { get; set; }
		public ExOffset Dependency { get; protected set; }
		public int Offset { get; protected set; }
		public OffsetType OffsetType { get; protected set; }
		internal MarshalType OffsetMarshalType { get; set; }

		#region [ GenricExternalClass ]

		/// <summary>
		/// DON'T USE, IT FOR `<see cref="ExOffset{T}"/>` And `<see cref="OffsetType"/>.ExternalClass` Only
		/// </summary>
		internal Type ExternalClassType { get; set; }

		/// <summary>
		/// DON'T USE, IT FOR `<see cref="ExOffset{T}"/>` And `<see cref="OffsetType"/>.ExternalClass` Only
		/// </summary>
		internal ExClass ExternalClassObject { get; set; }

		/// <summary>
		/// DON'T USE, IT FOR `<see cref="ExOffset{T}"/>` And `<see cref="OffsetType"/>.ExternalClass` Only
		/// </summary>
		internal bool ExternalClassIsPointer { get; set; }
		#endregion

		/// <summary>
		/// Offset Name
		/// </summary>
		internal string Name { get; set; }

		/// <summary>
		/// Offset Size
		/// </summary>
		internal int Size { get; set; }

		/// <summary>
		/// Offset Value As Object
		/// </summary>
		public object Value { get; internal set; }

		/// <summary>
		/// If Offset Is Pointer Then We Need A Place To Store
		/// Data It's Point To
		/// </summary>
		internal ReadOnlyMemory<byte> FullClassData { get; set; }

		internal bool DataAssigned { get; private set; }

		#endregion

		internal T Read<T>()
		{
			Type tType = typeof(T);
			if (OffsetType == OffsetType.ExternalClass && tType != typeof(UIntPtr) && tType != typeof(IntPtr))
				return (T)(object)ExternalClassObject;

			return (T)Value;
		}
		internal bool Write<T>(T value)
		{
			if (OffsetAddress == UIntPtr.Zero)
				return false;

			Value = value;
			return ExMemory.WriteBytes(OffsetAddress, OffsetMarshalType.ObjectToByteArray(Value));
		}

		internal void SetValueBytes(ReadOnlySpan<byte> fullDependencyBytes)
		{
			// (Dependency == None) Mean it's Base Class Data
			ReadOnlySpan<byte> valueBytes = (Dependency == None ? fullDependencyBytes : Dependency.FullClassData.Span).Slice(Offset, Size);

			// Set Value
			switch (OffsetType)
			{
				case OffsetType.String:
					Value = GetStringFromBytes(fullDependencyBytes, true);
					break;

				case OffsetType.UIntPtr:
				case OffsetType.ExternalClass when ExternalClassIsPointer:
					Value = OffsetMarshalType.ByteArrayToObject(valueBytes);
					break;

				case OffsetType.ExternalClass:
					// The value is ByteArray it's for Offsets inside the ExternalClass
					Value = valueBytes.ToArray();
					break;

				default:
					Value = OffsetMarshalType.ByteArrayToObject(valueBytes.ToArray());
					break;
			}
		}
		internal void SetData(ReadOnlySpan<byte> bytes)
		{
			DataAssigned = true;
			FullClassData = bytes.ToArray();
		}
		internal void RemoveValueAndData()
		{
			DataAssigned = false;

			// ToDo: Change This Logic
			if (Value is not null)
				Value = ExternalClassType is null ? default : Activator.CreateInstance(ExternalClassType);

			if (!FullClassData.IsEmpty)
				FullClassData = ReadOnlyMemory<byte>.Empty;
		}

		internal string GetStringFromBytes(ReadOnlySpan<byte> fullDependencyBytes, bool isUnicode)
		{
			int len = fullDependencyBytes.Length > (Offset + ExMemory.MaxStringLen)
				? ExMemory.MaxStringLen
				: fullDependencyBytes.Length - Offset;

			ReadOnlySpan<byte> buf = fullDependencyBytes.Slice(Offset, len);
			return Utils.BytesToString(buf, isUnicode).Split('\0')[0];
		}
	}

	public sealed class ExOffset<T> where T : new()
	{
		private MarshalType OffsetMarshalType { get; set; }

		public UIntPtr OffsetAddress { get; internal set; }
		public int Offset { get; }
		public OffsetType OffType { get; }
		public ExType ExternalType { get; }
		public T Value { get; }

		private ExOffset(int offset, OffsetType offType, ExType classType)
		{
			if (!typeof(T).IsSubclassOf(typeof(ExClass)))
				throw new InvalidCastException("This Constructor For `ExternalClass` Types Only.");

			Value = new T();
			Offset = offset;
			OffType = offType;
			ExternalType = classType;

			Init();
		}

		public ExOffset(int offset, ExType classType) : this(offset, OffsetType.ExClass, classType)
		{
			if (!typeof(T).IsSubclassOf(typeof(ExClass)))
				throw new InvalidCastException("This Constructor For `ExternalClass` Types Only.");
		}

		/// <summary>
		/// For Init Custom Types Like (<see cref="UIntPtr"/>, <see cref="int"/>, <see cref="float"/>, <see cref="string"/>, ..etc)
		/// </summary>
		public ExOffset(int offset) : this(offset, OffsetType.ValueType, ExType.Instance)
		{
			if (typeof(T).IsSubclassOf(typeof(ExClass)))
				throw new InvalidCastException("Use Other Constructor For `ExternalClass` Types.");
		}

		private void Init()
		{
			Type thisType = typeof(T);

			if (thisType == typeof(string))
			{
				OffsetType = OffsetType.String;
				OffsetMarshalType = new MarshalType(typeof(UIntPtr));
				Size = OffsetMarshalType.Size;
			}
			else if (thisType == typeof(IntPtr) || thisType == typeof(UIntPtr))
			{
				OffsetType = OffsetType.UIntPtr;
				OffsetMarshalType = new MarshalType(typeof(UIntPtr));
				Size = OffsetMarshalType.Size;
			}
			else if (thisType.IsSubclassOf(typeof(ExClass)) || thisType.IsSubclassOfRawGeneric(typeof(ExOffset<>)))
			{
				// OffsetType Set On Other Constructor If It's `ExternalClass`
				if (OffsetType != OffsetType.ExternalClass)
					throw new Exception("Use Other Constructor For `ExternalClass` Types.");

				// If externalClassIsPointer == true, ExternalClass Will Fix The Size Before Calc Class Size
				// So It's Okay To Leave It Like That
				Size = ExternalClassObject.ClassSize;

				OffsetMarshalType = ExternalClassIsPointer ? new MarshalType(typeof(UIntPtr)) : null;
			}
			else
			{
				Size = Marshal.SizeOf<T>();
				OffsetMarshalType = new MarshalType(thisType);
			}
		}

		public T Read() => Read<T>();
		public void Write(T value) => Write<T>(value);
	}
}
