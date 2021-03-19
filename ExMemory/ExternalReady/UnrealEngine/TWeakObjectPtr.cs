using System;

namespace ExternalMemory.ExternalReady.UnrealEngine
{
	public class TWeakObjectPtr<T> : ExClass where T : ExClass
	{
		#region [ Offsets ]

		protected ExOffset<int> _objectIndex;
		protected ExOffset<int> _objectSerialNumber;

		#endregion

		#region [ Props ]

		public int ObjectIndex
		{
			get => _objectIndex.Value;
			set => _objectIndex.Write(value);
		}

		public int ObjectSerialNumber
		{
			get => _objectSerialNumber.Value;
			set => _objectSerialNumber.Write(value);
		}

		#endregion

		public TWeakObjectPtr() { }
		public TWeakObjectPtr(UIntPtr address) : base(address) { }

		protected override void InitOffsets()
		{
			base.InitOffsets();

			_objectIndex = new ExOffset<int>(0x00);
			_objectSerialNumber = new ExOffset<int>(0x04);
		}
	}
}
