using System;

namespace ExternalMemory.ExternalReady.UnrealEngine
{
	public class TMap<T1, T2> : ExClass where T1 : ExClass where T2 : ExClass
	{
		#region [ Offsets ]

		// protected ExOffset<byte[]> _unknownData;

		#endregion

		public TMap() {}
		public TMap(UIntPtr address) : base(address) { }

		/// <inheritdoc />
		protected override void InitOffsets()
		{
			base.InitOffsets();

			// _unknownData = new ExOffset<byte>(0x00); // Size 0x50
		}
	}
}
