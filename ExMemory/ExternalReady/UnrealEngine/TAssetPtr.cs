using System;

namespace ExternalMemory.ExternalReady.UnrealEngine
{
	public class TAssetPtr<T> : ExClass where T : ExClass
	{
		#region [ Offsets ]


		#endregion

		#region [ Props ]

		#endregion

		public TAssetPtr() { }
		public TAssetPtr(UIntPtr address) : base(address) { }

		protected override void InitOffsets()
		{
			base.InitOffsets();
		}
	}
}
