namespace MapMaker {
	// A vec2, but for integers.
	// Our map generation can get RAM-intensive, so this helps a bit
	public class vec2i : ivec2<int> {

		public vec2i(int inX, int inY) : base(inX, inY) { }

		// Get the length (magnitude) of this vector
		public double Length {
			get { return System.Math.Sqrt((x * x) + (y * y)); }
		}

		// Get the corresponding unit vector
		public vec2 Unit {
			get {
				double length = Length;
				return new vec2(x / length, y / length);
			}
		}

		// --------------- Common Vector Functions ---------------
		public void Translate(vec2i other) {
			Translate(this, other);
		}

		public void Rotate(double theta) {
			Rotate(this, theta);
		}

		public void Scale(vec2i other) {
			Scale(this, other);
		}

		public void Scale(int scale) {
			Scale(this, scale);
		}

		public vec2 GetDirection(vec2i other) {
			return GetDirection(this, other);
		}

		public double GetDistance(vec2i other) {
			return GetDistance(this, other);
		}

		public vec2i Lerp(vec2i other, double weight) {
			return Lerp(this, other, weight);
		}

		// --------------- Static Versions Of Common Vector Functions ---------------
		public static void Translate(vec2i a, vec2i b) {
			a += b;
		}

		public static void Rotate(vec2i a, double theta) {
			double cosTheta = System.Math.Cos(Maths.DegreesToRadians(theta));
			double sinTheta = System.Math.Sin(Maths.DegreesToRadians(theta));

			double x = a.x * cosTheta - a.y * sinTheta;
			double y = a.x * sinTheta + a.y * cosTheta;

			a.x = (int)x;
			a.y = (int)y;
		}

		public static void Scale(vec2i a, vec2i scale) {
			a *= scale;
		}

		public void Scale(vec2i a, int scale) {
			a *= scale;
		}

		public static vec2 GetDirection(vec2i a, vec2i b) {
			return new vec2(b.x - a.x, b.y - a.y);
		}

		public static double GetDistance(vec2i a, vec2i b) {
			return GetDirection(a, b).Length;
		}

		public static vec2i Lerp(vec2i a, vec2i b, double weight) {
			int x = (int)((1.0 - weight) * a.x + weight * b.x);
			int y = (int)((1.0 - weight) * a.y + weight * b.y);

			return new vec2i(x, y);
		}

		public vec2i Abs() {
			return new vec2i(System.Math.Abs(x), System.Math.Abs(y));
		}

		public static vec2i Abs(vec2i a) {
			return new vec2i(System.Math.Abs(a.x), System.Math.Abs(a.y));
		}

		// --------------- Operators ---------------
		public static vec2i operator +(vec2i a, vec2i b) {
			return new vec2i(a.x + b.x, a.y + b.y);
		}

		public static vec2i operator +(vec2i a, int b) {
			return new vec2i(a.x + b, a.y + b);
		}

		public static vec2i operator -(vec2i a, vec2i b) {
			return new vec2i(a.x - b.x, a.y - b.y);
		}

		public static vec2i operator -(vec2i a, int b) {
			return new vec2i(a.x - b, a.y - b);
		}

		public static vec2i operator *(vec2i a, vec2i b) {
			return new vec2i(a.x * b.x, a.y * b.y);
		}

		public static vec2i operator *(vec2i a, int b) {
			return new vec2i((int)(a.x * b), (int)(a.y * b));
		}

		public static vec2 operator /(vec2i a, vec2i b) {
			return new vec2(a.x / b.x, a.y / b.y);
		}

		public static vec2 operator /(vec2i a, double b) {
			return new vec2(a.x / b, a.y / b);
		}

		public static vec2i operator %(vec2i a, int b) {
			return new vec2i(a.x % b, a.y % b);
		}

		public static vec2i operator %(vec2i a, vec2i b) {
			return new vec2i(a.x % b.x, a.y % b.y);
		}

		public static bool operator ==(vec2i a, vec2i b) {
			return (a.x == b.x) && (a.y == b.y);
		}

		public static bool operator !=(vec2i a, vec2i b) {
			return (a.x != b.x) || (a.y != b.y);
		}

		public static explicit operator vec2i(vec2 other) {
			return new vec2i((int)other.x, (int)other.y);
		}
	}
}
