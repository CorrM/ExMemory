using System;

namespace ExMemory.ExternalReady
{
	public class PString : ExClass
	{
		#region Offsets
		protected ExOffset<UIntPtr> _stringPointer;
		protected ExOffset<string> _stringData;
		#endregion

		#region Props
		public string Str => _stringData?.Read() ?? string.Empty;
		#endregion

		public PString() { }
		public PString(UIntPtr address) : base(address) {}

		/// <inheritdoc />
		protected override void InitOffsets()
		{
			base.InitOffsets();

			_stringPointer = new ExOffset<UIntPtr>(0x00);
			_stringData = new ExOffset<string>(_stringPointer, 0x00);
		}
	}
}
