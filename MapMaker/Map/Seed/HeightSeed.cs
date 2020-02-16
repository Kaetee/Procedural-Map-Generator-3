namespace MapMaker {
	public class HeightSeed<T> : BaseSeed<T> {
		public byte height;

		public HeightSeed() : this(0, 0) { }

		public HeightSeed(int inX, int inY) : base(inX, inY) {
			height = 0;
		}

		public HeightSeed(int inX, int inY, byte inHeight) : base(inX, inY) {
			height = inHeight;
		}

		public HeightSeed(int inX, int inY, byte inHeight, T inData) : base(inX, inY, inData) {
			height = inHeight;
		}

		public static HeightSeed<T> Default {
			get { return new HeightSeed<T>(0, 0, 0); }
		}
	}
}
