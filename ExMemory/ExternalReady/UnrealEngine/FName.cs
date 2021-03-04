using System;

namespace ExternalMemory.ExternalReady.UnrealEngine
{
	public class FName : ExClass
	{
		#region Offsets
		protected ExOffset<int> _index;
		protected ExOffset<int> _number;
		#endregion

		#region Props
		public int Index => _index.Value;
		public int Number => _number.Value;
		#endregion

		public FName() {}
		public FName(UIntPtr address) : base(address) { }

		/// <inheritdoc />
		protected override void InitOffsets()
		{
			base.InitOffsets();

			_index = new ExOffset<int>(0x00);
			_number = new ExOffset<int>(0x04);
		}
	}
}
