using System;
using System.Numerics;

namespace ExMemory.ExternalReady.UnrealEngine
{
	public class FTransform : ExClass
	{
		public static readonly FTransform Zero = new();

		#region Offsets
		protected ExOffset<Vector4> _rotation;
		protected ExOffset<Vector3> _translation;
		protected ExOffset<Vector3> _scale3D;
		#endregion

		#region Props
		public Vector4 Rotation
		{
			get => _rotation.Read();
			set => WriteOffset(_rotation, value);
		}
		public Vector3 Translation
		{
			get => _translation.Read();
			set => WriteOffset(_translation, value);
		}
		public Vector3 Scale3D
		{
			get => _scale3D.Read();
			set => WriteOffset(_scale3D, value);
		}
		#endregion
		
		public FTransform() {}
		public FTransform(UIntPtr address) : base(address) {}

		protected override void InitOffsets()
		{
			base.InitOffsets();

			_rotation = new ExOffset<Vector4>(ExOffset.None, 0x00);
			_translation = new ExOffset<Vector3>(ExOffset.None, 0x10);
			_scale3D = new ExOffset<Vector3>(ExOffset.None, 0x1C);
		}

		public Matrix4x4 ToMatrixWithScale()
		{
			float x2 = Rotation.X + Rotation.X;
			float y2 = Rotation.Y + Rotation.Y;
			float z2 = Rotation.Z + Rotation.Z;

			float xx2 = Rotation.X * x2;
			float yy2 = Rotation.Y * y2;
			float zz2 = Rotation.Z * z2;

			float yz2 = Rotation.Y * z2;
			float wx2 = Rotation.W * x2;

			float xy2 = Rotation.X * y2;
			float wz2 = Rotation.W * z2;

			float xz2 = Rotation.X * z2;
			float wy2 = Rotation.W * y2;

			var m = new Matrix4x4
			{
				M41 = Translation.X,
				M42 = Translation.Y,
				M43 = Translation.Z,
				M11 = (1.0f - (yy2 + zz2)) * Scale3D.X,
				M22 = (1.0f - (xx2 + zz2)) * Scale3D.Y,
				M33 = (1.0f - (xx2 + yy2)) * Scale3D.Z,
				M32 = (yz2 - wx2) * Scale3D.Z,
				M23 = (yz2 + wx2) * Scale3D.Y,
				M21 = (xy2 - wz2) * Scale3D.Y,
				M12 = (xy2 + wz2) * Scale3D.X,
				M31 = (xz2 + wy2) * Scale3D.Z,
				M13 = (xz2 - wy2) * Scale3D.X,
				M14 = 0.0f,
				M24 = 0.0f,
				M34 = 0.0f,
				M44 = 1.0f
			};

			return m;
		}
	}
}
