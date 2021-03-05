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

			// Cache offsets field info
			if (!Cache.ContainsKey(thisType))
			{
				List<FieldInfo> fields = thisType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
					.Where(f => f.FieldType.IsSubclassOfRawGeneric(typeof(ExOffset<>)))
					.ToList();

				Cache.Add(thisType, fields);
			}

			// Init offsets
			Offsets = Cache[thisType]
				.Select(f =>
				{
					var curOffset = (ExOffset)f.GetValue(this);

					if (curOffset is null)
						throw new NullReferenceException($"ExOffset can't be null, Offset name '{f.DeclaringType?.Name}.{f.Name}'.");

					return curOffset;
				})
				.OrderBy(off => off.Offset)
				.ToList();

			// Get Size Of Class
			ClassSize = Utils.GetClassSize(Offsets);
		}
		protected ExClass() : this(UIntPtr.Zero) { }

		/// <summary>
		/// Override this function to init offsets of your class.
		/// </summary>
		protected virtual void InitOffsets() {}

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
		public bool UpdateData(ReadOnlySpan<byte> fullClassBytes)
		{
			if (!ExMemory.IsInit || Address == UIntPtr.Zero)
				return false;

			FullClassBytes = fullClassBytes.ToArray();

			bool ret = ExMemory.ProcessClass(this, fullClassBytes);
			return AfterUpdate(ret);
		}

		/// <summary>
		/// Read Data And Set It On This Class.
		/// </summary>
		public bool UpdateData()
		{
			// Read Full Class
			if (ExMemory.ReadBytes(Address, (uint)ClassSize, out ReadOnlySpan<byte> fullClassBytes))
				return UpdateData(fullClassBytes);

			// Clear All Class Offset
			//ExMemory.RemoveValueData(Offsets);
			return false;
		}
	}
}
