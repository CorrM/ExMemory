using System;
using System.Runtime.InteropServices;
using ExMemory.Helper;

namespace ExMemory
{
	public enum OffsetType
	{
		None,
		Custom,

		/// <summary>
		/// DON'T USE, It's For <see cref="ExOffset{T}"/> Only
		/// </summary>
		ExternalClass,

		UIntPtr,
		String
	}

	public abstract class ExOffset
	{
		public static ExOffset None { get; } = new ExOffset<byte>(null, 0x0, OffsetType.None);

		#region [ Proparites ]

		internal UIntPtr OffsetAddress { get; set; }
		public ExOffset Dependency { get; protected set; }
		public int Offset { get; protected set; }
		public OffsetType OffsetType { get; protected set; }
		protected MarshalType OffsetMarshalType { get; set; }

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
		internal object Value { get; set; }

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
					Value = (UIntPtr)(ExMemory.Is64BitMemory ? (ulong)OffsetMarshalType.ByteArrayToObject(valueBytes) : (uint)OffsetMarshalType.ByteArrayToObject(valueBytes));
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

	public sealed class ExOffset<T> : ExOffset
	{
		public ExOffset(int offset) : this(None, offset) {}
		internal ExOffset(int offset, OffsetType offsetType) : this(None, offset, offsetType) { }
		public ExOffset(int offset, bool externalClassIsPointer) : this(None, offset, externalClassIsPointer) {}

		/// <summary>
		/// For Init Custom Types Like (<see cref="UIntPtr"/>, <see cref="int"/>, <see cref="float"/>, <see cref="string"/>, ..etc)
		/// </summary>
		public ExOffset(ExOffset dependency, int offset) : this(dependency, offset, OffsetType.Custom)
		{
			if (typeof(T).IsSubclassOf(typeof(ExClass)))
				throw new InvalidCastException("Use Other Constructor For `ExternalClass` Types.");

			Init();
		}

		/// <summary>
		/// For Init <see cref="ExClass"/>
		/// </summary>
		public ExOffset(ExOffset dependency, int offset, bool externalClassIsPointer) : this(dependency, offset, OffsetType.ExternalClass)
		{
			if (!typeof(T).IsSubclassOf(typeof(ExClass)))
				throw new InvalidCastException("This Constructor For `ExternalClass` Types Only.");

			ExternalClassType = typeof(T);
			ExternalClassIsPointer = externalClassIsPointer;
			ExternalClassObject = (ExClass)Activator.CreateInstance(ExternalClassType);

			Init();
		}

		/// <summary>
		/// Main
		/// </summary>
		internal ExOffset(ExOffset dependency, int offset, OffsetType offsetType)
		{
			Dependency = dependency;
			Offset = offset;
			OffsetType = offsetType;
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
	}
}
