﻿using System;

namespace ExternalMemory.ExternalReady.UnrealEngine
{
	public class FScriptMulticastDelegate : ExClass
	{
		#region [ Offsets ]

		// protected ExOffset<byte[]> _unknownData;

		#endregion

		public FScriptMulticastDelegate() {}
		public FScriptMulticastDelegate(UIntPtr address) : base(address) { }

		/// <inheritdoc />
		protected override void InitOffsets()
		{
			base.InitOffsets();

			// _unknownData = new ExOffset<byte>(0x00); // Size 0x10
		}
	}
}
