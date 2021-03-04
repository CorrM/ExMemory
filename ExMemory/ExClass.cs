using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExternalMemory.Helper;

namespace ExternalMemory
{
	public abstract class ExClass
	{
		private static readonly Dictionary<Type, List<FieldInfo>> Cache = new();

		internal List<ExOffset> Offsets { get; }
		internal int ClassSize { get; }
		internal ReadOnlyMemory<byte> FullClassBytes { get; set; }

		public UIntPtr Address { get; set; }

		protected ExClass(UIntPtr address)
		{
			Address = address;

			// ReSharper disable once VirtualMemberCallInConstructor
			InitOffsets();

			Type thisType = this.GetType();

			// Cache offsets
			if (!Cache.ContainsKey(thisType))
			{
				List<FieldInfo> fields = thisType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
					.Where(f => f.FieldType == typeof(ExOffset) || f.FieldType.IsSubclassOfRawGeneric(typeof(ExOffset<>)))
					.ToList();

				Cache.Add(thisType, fields);
			}

			// Init offsets
			Offsets = Cache[thisType]
				.Select(f =>
				{
					var curOffset = (ExOffset)f.GetValue(this);

					if (curOffset is null)
						throw new NullReferenceException($"ExOffset can't be null, Offset name '{f.Name}'.");


					// Set Info
					curOffset.Name = f.Name;
					curOffset.Size = curOffset.OffsetType switch
					{
						OffsetType.UIntPtr => ExMemory.PointerSize,
						OffsetType.ExternalClass when curOffset.ExternalClassIsPointer => ExMemory.PointerSize,
						_ => curOffset.Size
					};

					return curOffset;
				})
				.OrderBy(off => off.Offset)
				.ToList();

			// Sort By Dependencies
			Offsets = Offsets.Sort(off => Offsets.Where(offset => offset == off.Dependency));

			// Get Size Of Class
			ClassSize = Utils.GetDependenciesSize(ExOffset.None, Offsets);
		}
		protected ExClass() : this(UIntPtr.Zero) { }

		/// <summary>
		/// Override this function to init offsets of your class.
		/// </summary>
		protected virtual void InitOffsets() {}

		/// <summary>
		/// Read Data And Set It On This Class.
		/// </summary>
		public virtual bool UpdateData()
		{
			return Address != UIntPtr.Zero && ExMemory.ReadClass(this);
		}

		/// <summary>
		/// Set Data On This Class (Doesn't Call ReadMemory Callback, it just fill class with that data),
		/// Be Careful When Using This Function
		/// </summary>
		/// <param name="fullClassBytes">Full bytes of <see cref="ExClass"/></param>
		public virtual bool UpdateData(ReadOnlyMemory<byte> fullClassBytes)
		{
			FullClassBytes = fullClassBytes;
			return Address != UIntPtr.Zero && ExMemory.ReadClass(this, fullClassBytes.Span);
		}
	}
}
