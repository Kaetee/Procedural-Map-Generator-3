namespace MapMaker {
	// a double vector
	// lowercase as it's to be used as a simple type like int/float/etc
	public class vec2 : ivec2<double> {
		public vec2(double inX, double inY) : base(inX, inY) { }

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
		public void Translate(vec2 other) {
			Translate(this, other);
		}

		public void Rotate(double theta) {
			Rotate(this, theta);
		}

		public void Scale(vec2 other) {
			Scale(this, other);
		}

		public void Scale(double scale) {
			Scale(this, scale);
		}

		public vec2 GetDirection(vec2 other) {
			return GetDirection(this, other);
		}

		public double GetDistance(vec2 other) {
			return GetDistance(this, other);
		}

		public vec2 Lerp(vec2 other, double weight) {
			return Lerp(this, other, weight);
		}

		// --------------- Static Versions Of Common Vector Functions ---------------
		public static void Translate(vec2 a, vec2 b) {
			a += b;
		}

		public static void Rotate(vec2 a, double theta) {
			double cosTheta = System.Math.Cos(Maths.DegreesToRadians(theta));
			double sinTheta = System.Math.Sin(Maths.DegreesToRadians(theta));

			double x = a.x * cosTheta - a.y * sinTheta;
			double y = a.x * sinTheta + a.y * cosTheta;

			a.x = x;
			a.y = y;
		}

		public static void Scale(vec2 a, vec2 scale) {
			a *= scale;
		}

		public void Scale(vec2 a, double scale) {
			a *= scale;
		}

		public void Round(double scale) {
			x = scale * System.Math.Round(x / scale);
			y = scale * System.Math.Round(y / scale);
		}

		public static vec2 GetDirection(vec2 a, vec2 b) {
			return new vec2(b.x - a.x, b.y - a.y);
		}

		public static double GetDistance(vec2 a, vec2 b) {
			return GetDirection(a, b).Length;
		}

		public static vec2 Lerp(vec2 a, vec2 b, double weight) {
			double x = (1.0 - weight) * a.x + weight * b.x;
			double y = (1.0 - weight) * a.y + weight * b.y;

			return new vec2(x, y);
		}

		// --------------- Operators ---------------
		public static vec2 operator +(vec2 a, vec2 b) {
			return new vec2(a.x + b.x, a.y + b.y);
		}

		public static vec2 operator +(vec2 a, double b) {
			return new vec2(a.x + b, a.y + b);
		}

		public static vec2 operator -(vec2 a, vec2 b) {
			return new vec2(a.x - b.x, a.y - b.y);
		}

		public static vec2 operator -(vec2 a, double b) {
			return new vec2(a.x - b, a.y - b);
		}

		public static vec2 operator *(vec2 a, vec2 b) {
			return new vec2(a.x * b.x, a.y * b.y);
		}

		public static vec2 operator *(vec2 a, double b) {
			return new vec2(a.x * b, a.y * b);
		}

		public static vec2 operator /(vec2 a, vec2 b) {
			return new vec2(a.x / b.x, a.y / b.y);
		}

		public static vec2 operator /(vec2 a, double b) {
			return new vec2(a.x / b, a.y / b);
		}

		public static vec2 operator %(vec2 a, double b) {
			return new vec2(a.x % b, a.y % b);
		}

		public static vec2 operator %(vec2 a, vec2 b) {
			return new vec2(a.x % b.x, a.y % b.y);
		}

		public static bool operator ==(vec2 a, vec2 b) {
			return (a.x == b.x) && (a.y == b.y);
		}

		public static bool operator !=(vec2 a, vec2 b) {
			return (a.x != b.x) || (a.y != b.y);
		}

		public static implicit operator vec2(vec2i other) {
			return new vec2(other.x, other.y);
		}
	}
}
