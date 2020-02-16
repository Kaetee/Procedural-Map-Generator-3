namespace MapMaker {
	public abstract class Spline {
		private vec2[] points;

		public Spline(int order) {
			points = new vec2[order + 2];
		}

		public vec2[] Points {
			get { return points; }
			set { points = value; }
		}

		/*
		public vec2 this[int i] {
			get { return points[i]; }
			set { points[i] = value; }
		}
		*/

		public int PointCount {
			get { return points.Length; }
		}
	}
}
