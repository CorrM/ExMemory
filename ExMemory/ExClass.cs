using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExternalMemory.Helper;

namespace ExternalMemory
{
	public abstract class ExClass
	{
		private static readonly ConcurrentDictionary<Type, List<FieldInfo>> FieldsCache = new();

		internal bool IsInit { get; private set; }
		internal List<ExOffset> Offsets { get; private set; }
		internal int ClassSize { get; private set; }
		internal ReadOnlyMemory<byte> FullClassBytes { get; set; }

		public UIntPtr Address { get; set; }

		protected ExClass(UIntPtr address)
		{
			Address = address;

			Init();
		}
		protected ExClass() : this(UIntPtr.Zero) { }

		private void Init()
		{
			// ReSharper disable once VirtualMemberCallInConstructor
			InitOffsets();

			Type thisType = this.GetType();

			// Cache offsets field info
			if (!FieldsCache.ContainsKey(thisType))
			{
				List<FieldInfo> fields = thisType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
					.Where(f => f.FieldType.IsSubclassOfRawGeneric(typeof(ExOffset<>)))
					.ToList();

				FieldsCache.TryAdd(thisType, fields);
			}

			// Init offsets
			Offsets = FieldsCache[thisType]
				.Select(f =>
				{
					var curOffset = (ExOffset)f.GetValue(this);
					if (curOffset is null)
						throw new NullReferenceException($"ExOffset can't be null, Offset name '{f.DeclaringType?.Name}.{f.Name}'.");

					curOffset.Name = f.Name;
					curOffset.Parent = this;

					curOffset.Init();

					return curOffset;
				})
				.OrderBy(off => off.Offset)
				.ToList();

			// Get Size Of Class
			ClassSize = Utils.GetClassSize(Offsets);

			// Set init state
			IsInit = true;
		}

		/// <summary>
		/// Override this function to init offsets of your class.
		/// </summary>
		protected virtual void InitOffsets() { }

		/// <summary>
		/// Called after <see cref="UpdateData()"/>,
		/// Return value form that function will override return value of <see cref="UpdateData()"/>
		/// </summary>
		/// <param name="updated">State of <see cref="UpdateData()"/></param>
		/// <returns>Return value of <paramref name="updated"/></returns>
		protected virtual bool AfterUpdate(bool updated)
		{
			return updated;
		}

		/// <summary>
		/// Set data on this class (Doesn't call ReadMemory callback, it just fill class with that data),
		/// Be careful when using this function
		/// </summary>
		/// <param name="fullClassBytes">Full bytes of <see cref="ExClass"/></param>
		/// <param name="readList">List of read class to help not trap in Read Cycle</param>
		internal bool UpdateData(ReadOnlySpan<byte> fullClassBytes, HashSet<UIntPtr> readList)
		{
			if (!ExMemory.IsInit || Address == UIntPtr.Zero)
				return false;

			FullClassBytes = fullClassBytes.ToArray();

			bool ret = ExMemory.ProcessClass(this, fullClassBytes, readList);
			return AfterUpdate(ret);
		}

		/// <summary>
		/// Read Data And Set It On This Class.
		/// </summary>
		internal bool UpdateData(HashSet<UIntPtr> readList)
		{
			// Read Full Class
			if (ExMemory.ReadBytes(Address, (uint)ClassSize, out ReadOnlySpan<byte> fullClassBytes))
				return UpdateData(fullClassBytes, readList);

			// Clear All Class Offset
			//ExMemory.RemoveValueData(Offsets);
			return false;
		}

		/// <summary>
		/// Set data on this class (Doesn't call ReadMemory callback, it just fill class with that data),
		/// Be careful when using this function
		/// </summary>
		/// <param name="fullClassBytes">Full bytes of <see cref="ExClass"/></param>
		public bool UpdateData(ReadOnlySpan<byte> fullClassBytes)
		{
			return UpdateData(fullClassBytes, new HashSet<UIntPtr>());
		}

		/// <summary>
		/// Read Data And Set It On This Class.
		/// </summary>
		public bool UpdateData()
		{
			return UpdateData(new HashSet<UIntPtr>());
		}
	}
}
