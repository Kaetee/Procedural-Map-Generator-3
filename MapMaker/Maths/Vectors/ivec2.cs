namespace MapMaker {
	// Defines the behaviour of vector maths
	// ivec2: a 2-dimensional interface, used also for storing a vector of ranges vec2(<x_min, x_max>, <y_min, y_max>)
	public class ivec2<T> {
		public T x;
		public T y;

		public ivec2(T inX, T inY) {
			x = inX;
			y = inY;
		}

		public T this[int i] {
			get {
				switch (i) {
					case 0:
						return x;
					case 1:
						return y;
					default:
						return default(T);
				}
			}

			set {
				switch (i) {
					case 0:
						x = value;
						break;
					case 1:
						y = value;
						break;
				}
			}
		}
	}
}
