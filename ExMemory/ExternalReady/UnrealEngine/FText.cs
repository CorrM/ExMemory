using System;

namespace ExternalMemory.ExternalReady.UnrealEngine
{
	public class FText : ExClass
	{
		#region [ Offsets ]

		// protected ExOffset<byte[]> _unknownData;

		#endregion

		public FText() {}
		public FText(UIntPtr address) : base(address) { }

		/// <inheritdoc />
		protected override void InitOffsets()
		{
			base.InitOffsets();

			// _unknownData = new ExOffset<byte>(0x00); // Size 0x18
		}
	}
}
