using System;
using System.Runtime.InteropServices;

namespace ExternalMemory.Helper
{
	public static class HeapHelper
	{
		public struct StructAllocator<TStruct> : IDisposable
		{
			public IntPtr UnmanagedPtr { get; private set; }
			public TStruct ManagedStruct { get; private set; }

			/// <summary>
			/// Update unmanaged data from `<see cref="UnmanagedPtr"/>` to managed struct
			/// </summary>
			public bool Update()
			{
				if (UnmanagedPtr == IntPtr.Zero)
					UnmanagedPtr = Marshal.AllocHGlobal(Marshal.SizeOf<TStruct>());

				ManagedStruct = Marshal.PtrToStructure<TStruct>(UnmanagedPtr);
				return true;
			}

			public void Dispose()
			{
				if (UnmanagedPtr == IntPtr.Zero)
					return;

				Marshal.FreeHGlobal(UnmanagedPtr);
				UnmanagedPtr = IntPtr.Zero;
			}

			public static implicit operator IntPtr(StructAllocator<TStruct> w)
			{
				return w.UnmanagedPtr;
			}
		}

		public class StringAllocator : IDisposable
		{
			public enum StringType
			{
				Ansi,
				Unicode
			}

			public IntPtr Ptr { get; private set; }
			public int Length { get; set; }
			public StringType StrType { get; }
			public string ManagedString { get; private set; }

			public StringAllocator(int len, StringType stringType)
			{
				StrType = stringType;
				Length = StrType == StringType.Ansi ? len : len * 2;
				Ptr = Marshal.AllocHGlobal(Length);
			}

			~StringAllocator()
			{
				// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
				Dispose(false);
			}

			/// <summary>
			/// Change size of allocated string.
			/// </summary>
			/// <param name="len">New size of string</param>
			public void ReSize(int len)
			{
				Length = StrType == StringType.Ansi ? len : len * 2;
				Ptr = Marshal.ReAllocHGlobal(Ptr, (IntPtr)Length);
				Update();
			}

			/// <summary>
			/// Update unmanaged data from <see cref="Ptr"/> to managed struct
			/// </summary>
			public bool Update()
			{
				if (Ptr == IntPtr.Zero)
					return false;

				switch (StrType)
				{
					case StringType.Ansi:
						ManagedString = Marshal.PtrToStringAnsi(Ptr);
						break;
					case StringType.Unicode:
						ManagedString = Marshal.PtrToStringUni(Ptr);
						break;
					default:
						return false;
				}

				return true;
			}

			public static implicit operator IntPtr(StringAllocator w)
			{
				return w.Ptr;
			}

			public static implicit operator string(StringAllocator w)
			{
				return w.ManagedString;
			}

			#region IDisposable Support

			private bool _disposedValue; // To detect redundant calls

			protected virtual void Dispose(bool disposing)
			{
				if (_disposedValue)
					return;

				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
				}

				if (Ptr == IntPtr.Zero)
					return;

				Marshal.FreeHGlobal(Ptr);
				Ptr = IntPtr.Zero;

				_disposedValue = true;
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			#endregion
		}
	}
}
