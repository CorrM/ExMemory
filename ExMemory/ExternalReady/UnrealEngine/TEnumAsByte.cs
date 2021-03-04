using System;

namespace ExternalMemory.ExternalReady.UnrealEngine
{
	public class TEnumAsByte<T> : ExClass where T : Enum
	{
		#region [ Offsets ]

		protected ExOffset<byte> _enumVal;

		#endregion

		#region [ Props ]

		public T Value => (T)(object)_enumVal.Read();

		#endregion

		public TEnumAsByte() { }
		public TEnumAsByte(UIntPtr address) : base(address) { }

		protected override void InitOffsets()
		{
			base.InitOffsets();

			_enumVal = new ExOffset<byte>(ExOffset.None, 0x00);
		}
	}
}
