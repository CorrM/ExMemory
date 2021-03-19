using System;

namespace ExternalMemory.ExternalReady.UnrealEngine
{
	public class FString : ExClass
	{
		#region [ Offsets ]

		protected ExOffset<UIntPtr> _stringPointer;
		protected ExOffset<int> _stringData;
		protected ExOffset<int> _stringCount;

		#endregion

		#region [ Props ]

		public UIntPtr StringPointer
		{
			get => _stringPointer.Value;
			set => _stringPointer.Write(value);
		}

		public int StringData
		{
			get => _stringData.Value;
			set => _stringData.Write(value);
		}

		public int StringCount
		{
			get => _stringCount.Value;
			set => _stringCount.Write(value);
		}

		//public string Str => _stringData?.Value ?? string.Empty;

		#endregion

		public FString() {}
		public FString(UIntPtr address) : base(address) {}

		protected override void InitOffsets()
		{
			base.InitOffsets();

			int offset = 0x00;

			_stringPointer = new ExOffset<UIntPtr>(offset); offset += ExMemory.PointerSize;
			_stringData = new ExOffset<int>(offset); offset += 4;
			_stringCount = new ExOffset<int>(offset);
		}
	}
}
