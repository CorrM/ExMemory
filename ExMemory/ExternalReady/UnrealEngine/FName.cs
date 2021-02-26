using System;

namespace ExMemory.ExternalReady.UnrealEngine
{
	public class FName : ExClass
	{
		#region Offsets
		protected ExOffset<int> _index;
		protected ExOffset<int> _number;
		#endregion

		#region Props
		public int Index => _index.Read();
		public int Number => _number.Read();
		#endregion

		public FName() {}
		public FName(UIntPtr address) : base(address) { }

		/// <inheritdoc />
		protected override void InitOffsets()
		{
			base.InitOffsets();

			_index = new ExOffset<int>(ExOffset.None, 0x00);
			_number = new ExOffset<int>(ExOffset.None, 0x04);
		}
	}
}
