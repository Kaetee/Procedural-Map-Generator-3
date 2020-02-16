using System;

namespace MapMaker {
	public class SpreadDirs {
		byte[,] intensities;

		public SpreadDirs() {
			intensities = new byte[3,3];
			StrengthIntensity = 1;

			SetAll(1);
		}

		public SpreadDirs(double direction, byte intensity, bool reflect = false) : this() {
			direction = Maths.RoundToSubdivision(direction, 360.0f / 8.0f);
			vec2 pos = Maths.AngleToPoint(1.0, direction) + 1.0;

			intensities[(int)pos.x, (int)pos.y] = intensity;

			if (reflect)
				intensities[2 - (int)pos.x, 2 - (int)pos.y] = intensity;
		}

		// A hidden, local version of Strength
		// For when we want to edit the strength within the class without adjusting other values
		// However, allowing other classes access to this would be unsafe
		private byte StrengthIntensity {
			get => intensities[1, 1];
			set => intensities[1, 1] = value;
		}

		public byte Strength {
			get => StrengthIntensity;
			set {
				for (int i = 0; i < 3; i++)
					for (int j = 0; j < 3; j++)
						if (i != 1 && j != 1)
							this[j, i] = (byte)(value * Math.Max(this[j, i] / StrengthIntensity, 1));

				StrengthIntensity = value;
			}
		}

		public byte this[int a, int b] {
			get => intensities[a, b];
			set => intensities[a, b] = value;
		}

		public byte[,] Intensities {
			get => intensities;
			set => intensities = value;
		}

		void SetAll(byte value) {
			for (int i = 0; i < 3; i++)
				for (int j = 0; j < 3; j++)
					if (i != 1 && j != 1)
						intensities[j, i] = value;
		}

		public SpreadDirs Clone() {
			SpreadDirs output = new SpreadDirs();

			output.StrengthIntensity = StrengthIntensity;
			for (int i = 0; i < 3; i++)
				for (int j = 0; j < 3; j++)
					output.intensities[j, i] = intensities[j, i];

			return output;
		}
	}
}
