using System;

namespace ExternalMemory.ExternalReady.UnrealEngine
{
	public class FMulticastSparseDelegate : ExClass
	{
		#region [ Offsets ]

		// protected ExOffset<byte> _unknownData;

		#endregion

		public FMulticastSparseDelegate() {}
		public FMulticastSparseDelegate(UIntPtr address) : base(address) { }

		/// <inheritdoc />
		protected override void InitOffsets()
		{
			base.InitOffsets();

			// _unknownData = new ExOffset<byte>(0x00);
		}
	}
}
