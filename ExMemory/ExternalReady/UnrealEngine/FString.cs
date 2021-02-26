using System;

namespace ExMemory.ExternalReady.UnrealEngine
{
	public class FString : ExClass
	{
		#region [ Offsets ]

		protected ExOffset<UIntPtr> _stringPointer;
		protected ExOffset<string> _stringData;

		#endregion

		#region [ Props ]

		public string Str => _stringData?.Read() ?? string.Empty;

		#endregion

		public FString() {}
		public FString(UIntPtr address) : base(address) {}

		protected override void InitOffsets()
		{
			base.InitOffsets();

			_stringPointer = new ExOffset<UIntPtr>(0x00);
			_stringData = new ExOffset<string>(_stringPointer, 0x00);
		}
	}
}
