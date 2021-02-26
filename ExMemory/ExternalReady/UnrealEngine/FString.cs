using System;

namespace ExMemory.ExternalReady.UnrealEngine
{
	public class FString : ExClass
	{
		#region [ Offsets ]

		protected ExternalOffset<UIntPtr> _stringPointer;
		protected ExternalOffset<string> _stringData;

		#endregion

		#region [ Props ]

		public string Str => _stringData?.Read() ?? string.Empty;

		#endregion

		public FString() {}
		public FString(UIntPtr address) : base(address) {}

		protected override void InitOffsets()
		{
			base.InitOffsets();

			_stringPointer = new ExternalOffset<UIntPtr>(0x00);
			_stringData = new ExternalOffset<string>(_stringPointer, 0x00);
		}
	}
}
