using System;

namespace ExMemory.ExternalReady
{
	public class PString : ExClass
	{
		#region Offsets
		protected ExternalOffset<UIntPtr> _stringPointer;
		protected ExternalOffset<string> _stringData;
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

			_stringPointer = new ExternalOffset<UIntPtr>(0x00);
			_stringData = new ExternalOffset<string>(_stringPointer, 0x00);
		}
	}
}
