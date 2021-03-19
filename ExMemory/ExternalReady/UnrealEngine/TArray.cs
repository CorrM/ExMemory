using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ExternalMemory.Helper;

namespace ExternalMemory.ExternalReady.UnrealEngine
{
	// ReSharper disable once InconsistentNaming
	/// <summary>
	/// TArray Class To Fit UnrealEngine
	/// </summary>
	public class TArray<T> : ExClass
	{
		public class DelayData
		{
			public int DelayEvery { get; set; } = 1;
			public int Delay { get; set; } = 0;
		}
		public class ReadData
		{
			public bool IsPointerToData { get; set; } = true;
			public int BadSizeAfterEveryItem { get; set; } = 0x0;
			public bool UseMaxAsReadCount { get; set; } = false;
		}

		private int _itemSize = -1;

		public List<T> Items { get; }

		#region [ Offsets ]

		protected ExOffset<UIntPtr> _data;
		protected ExOffset<int> _count;
		protected ExOffset<int> _max;

		#endregion

		#region [ Props ]

		public int MaxCountTArrayCanCarry { get; } = 0x20000;
		public DelayData DelayInfo { get; }
		public ReadData ReadInfo { get; }

		public UIntPtr Data => _data?.Value ?? UIntPtr.Zero;
		public int Count => _count?.Value ?? 0;
		public int Max => _max?.Value ?? 0;

		public T this[int index] => Items[index];

		#endregion

		public TArray(UIntPtr address) : base(address)
		{
			Items = new List<T>();
			DelayInfo = new DelayData();
			ReadInfo = new ReadData();
		}
		public TArray() : this(UIntPtr.Zero) { }
		public TArray(UIntPtr address, int maxCountTArrayCanCarry) : this(address)
		{
			MaxCountTArrayCanCarry = maxCountTArrayCanCarry;
		}

		protected override void InitOffsets()
		{
			base.InitOffsets();

			int curOff = 0x0;
			_data = new ExOffset<UIntPtr>(curOff); curOff += ExMemory.PointerSize;
			_count = new ExOffset<int>(curOff); curOff += 0x04;
			_max = new ExOffset<int>(curOff);
		}
		protected override bool AfterUpdate(bool updated)
		{
			if (!updated)
				return false;

			if (!InitItems())
				return false;

			if (_itemSize == -1)
			{
				if (typeof(T).IsSubclassOf(typeof(ExClass)))
				{
					if (ReadInfo.IsPointerToData)
						_itemSize = ExMemory.PointerSize;
					else
						_itemSize = ((ExClass)Activator.CreateInstance(typeof(T)))?.ClassSize ?? throw new NullReferenceException($"Can't create instance of '{typeof(T).Name}'.");
				}
				else
					_itemSize = MarshalType.GetSizeOfType(typeof(T));
			}

			int counter = 0;
			int itemSize = _itemSize + ReadInfo.BadSizeAfterEveryItem;

			// Get TArray Data
			ExMemory.ReadBytes(Data, (uint)(Items.Count * itemSize), out ReadOnlySpan<byte> tArrayData);
			int offset = 0;

			for (int i = 0; i < Items.Count; i++)
			{
				T item = Items[i];

				UIntPtr itemAddress;
				if (ReadInfo.IsPointerToData)
				{
					// Get Item Address (Pointer Value (aka Pointed Address))
					itemAddress = ExMemory.Is64BitMemory
						? (UIntPtr) MemoryMarshal.Read<ulong>(tArrayData[offset..])
						: (UIntPtr) MemoryMarshal.Read<uint>(tArrayData[offset..]);
				}
				else
				{
					itemAddress = this.Address + offset;
				}

				// Update current item
				if (typeof(T).IsSubclassOf(typeof(ExClass)))
				{
					var exItem = (ExClass)(object)item;
					exItem.Address = itemAddress;

					/*
					// Set Data
					if (ReadInfo.IsPointerToData)
						exItem.UpdateData();
					else
						exItem.UpdateData(tArrayData.Slice(offset, itemSize));
					*/
				}
				else
				{
					if (ReadInfo.IsPointerToData)
					{
						// Todo: Implement that
					}
					else
					{
						Items[i] = (T)MarshalType.ByteArrayToObject(typeof(T), tArrayData.Slice(offset, itemSize));
						// Items[i] = MemoryMarshal.Read<T>(tArrayData.Slice(offset, itemSize));
					}
				}

				// Move Offset
				offset += itemSize;

				if (DelayInfo.Delay == 0)
					continue;

				counter++;
				if (counter < DelayInfo.DelayEvery)
					continue;

				Thread.Sleep(DelayInfo.Delay);
				counter = 0;
			}

			return true;
		}

		private bool InitItems()
		{
			int count = ReadInfo.UseMaxAsReadCount ? Max : Count;
			if (count > MaxCountTArrayCanCarry || count < 0)
				return false;

			try
			{
				if (Items.Count > count)
				{
					Items.RemoveRange(count, Items.Count - count);
				}
				else if (Items.Count < count)
				{
					foreach (int _ in Enumerable.Range(Items.Count, count))
					{
						var instance = (T)Activator.CreateInstance(typeof(T));
						Items.Add(instance);
					}
				}
			}
			catch
			{
				return false;
			}

			return true;
		}

		public bool IsValid()
		{
			int count = ReadInfo.UseMaxAsReadCount ? Max : Count;

			if (count == 0 /*&& !Read()*/)
				return false;

			return (Max > Count) && Address != UIntPtr.Zero;
		}
	}
}
