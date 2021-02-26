using System;

namespace ExMemory.ExternalReady.UnrealEngine
{
	public class TEnumAsByte<T> : ExClass where T : Enum
	{
		#region [ Offsets ]

		protected ExternalOffset<byte> _enumVal;

		#endregion

		#region [ Props ]

		public T Value => (T)(object)_enumVal.Read();

		#endregion

		public TEnumAsByte() { }
		public TEnumAsByte(UIntPtr address) : base(address) { }

		protected override void InitOffsets()
		{
			base.InitOffsets();

			_enumVal = new ExternalOffset<byte>(ExOffset.None, 0x00);
		}
	}
}
