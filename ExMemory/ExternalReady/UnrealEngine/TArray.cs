using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace ExMemory.ExternalReady.UnrealEngine
{
	// ReSharper disable once InconsistentNaming
	/// <summary>
	/// TArray Class To Fit UnrealEngine, It's only Support Pointer To <see cref="ExClass"/> Only
	/// </summary>
	public class TArray<T> : ExClass where T : ExClass, new()
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

		private readonly int _itemSize;

		public List<T> Items { get; }

		#region [ Offsets ]

		protected ExternalOffset<UIntPtr> _data;
		protected ExternalOffset<int> _count;
		protected ExternalOffset<int> _max;

		#endregion

		#region [ Props ]

		public int MaxCountTArrayCanCarry { get; } = 0x20000;
		public DelayData DelayInfo { get; }
		public ReadData ReadInfo { get; }

		public UIntPtr Data => _data?.Read() ?? UIntPtr.Zero;
		public int Count => _count?.Read() ?? 0;
		public int Max => _max?.Read() ?? 0;

		#endregion

		public TArray(UIntPtr address) : base(address)
		{
			_itemSize = ((T)Activator.CreateInstance(typeof(T))).ClassSize;

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
			_data = new ExternalOffset<UIntPtr>(ExOffset.None, curOff); curOff += ExMemorySharp.Is64BitMemory ? 0x08 : 0x04;
			_count = new ExternalOffset<int>(ExOffset.None, curOff); curOff += 0x04;
			_max = new ExternalOffset<int>(ExOffset.None, curOff);
		}

		public override bool UpdateData(ExMemorySharp ems)
		{
			// Read Array (Base and Size)
			if (!Read(ems))
				return false;

			int counter = 0;
			int itemSize = ReadInfo.IsPointerToData ? (ExMemorySharp.Is64BitMemory ? 0x08 : 0x04) : _itemSize;
			itemSize += ReadInfo.BadSizeAfterEveryItem;

			// Get TArray Data
			ems.ReadBytes(Data, (uint)(Items.Count * itemSize), out ReadOnlySpan<byte> tArrayData);
			int offset = 0;

			foreach (T item in Items)
			{
				UIntPtr itemAddress;
				if (ReadInfo.IsPointerToData)
				{
					// Get Item Address (Pointer Value (aka Pointed Address))
					itemAddress = ExMemorySharp.Is64BitMemory
						? (UIntPtr)MemoryMarshal.Read<ulong>(tArrayData[offset..])
						: (UIntPtr)MemoryMarshal.Read<uint>(tArrayData[offset..]);
				}
				else
				{
					itemAddress = this.BaseAddress + offset;
				}

				// Update current item
				item.BaseAddress = itemAddress;

				// Set Data
				if (ReadInfo.IsPointerToData)
					item.UpdateData(ems);
				else
					item.UpdateData(ems, tArrayData.Slice(offset, itemSize).ToArray());

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

		private bool Read(ExMemorySharp ems)
		{
			if (!ems.ReadClass(this, BaseAddress))
				return false;

			int count = ReadInfo.UseMaxAsReadCount ? Max : Count;

			if (count > MaxCountTArrayCanCarry)
				return false;

			try
			{
				if (count <= 0)
					return false;

				if (Items.Count > count)
				{
					Items.RemoveRange(count, Items.Count - count);
				}
				else if (Items.Count < count)
				{
					foreach (int i in Enumerable.Range(Items.Count, count))
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

			return (Max > Count) && BaseAddress != UIntPtr.Zero;
		}

		#region Indexer
		public T this[int index] => Items[index];
		#endregion
	}
}
