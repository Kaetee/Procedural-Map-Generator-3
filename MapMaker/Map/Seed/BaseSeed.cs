using System;

namespace MapMaker {
	public class BaseSeed<T> {
		public int x;
		public int y;
		public T data;

		public vec2i AsVector {
			get => new vec2i(x, y);
		}

		public BaseSeed(int inX, int inY) {
			x = inX;
			y = inY;
		}

		public BaseSeed(int inX, int inY, T inData) {
			x = inX;
			y = inY;
			data = inData;
		}

		public static BaseSeed<T> Generate(int rangeX, int rangeY, Random random) {
			return new BaseSeed<T>(random.Next(rangeX), random.Next(rangeY));
		}
	}
}
