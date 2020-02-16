using System;
using System.Collections.Generic;

namespace MapMaker {
	// Bezier Spline maths
	public class SplineBezier : Spline {
		public SplineBezier(int order) : base(order) { }

		// Returns the coordinates of the spline at position t, where 0.0 <= t <= 1.0
		public vec2 this[double t] {
			get {
				double x = 0;
				double y = 0;
				double multiplier;
				for (int p = 0; p < PointCount; p++) {
					if (p == 0 || p == PointCount - 1)
						multiplier = 1.0;
					else
						multiplier = PointCount - 1;

					x += multiplier * Math.Pow(1.0 - t, PointCount - (p + 1)) * Math.Pow(t, p) * Points[p].x;
					y += multiplier * Math.Pow(1.0 - t, PointCount - (p + 1)) * Math.Pow(t, p) * Points[p].y;
				}

				return new vec2(x, y);
			}
		}

		public static SplineBezier Generate(vec2 start, vec2 end, double maxDisplacement, int order, int seed) {
			SplineBezier spline = new SplineBezier(order);
			Random random = new Random(seed);
			vec2[] points = new vec2[order + 2];

			vec2 direction = (end - start).Unit;
			double distance = (end - start).Length;
			vec2 right = new vec2(direction.y, -direction.x);
			vec2 left = right * -1.0;

			vec2 deltaDir;

			points[0] = start;
			points[order + 1] = end;
			
			// Each control point can take an index between (lastMax) and (nextMin)

			double step = distance / (order + 2.0);
			double multiplier = 1.0 / (order + 2.0);

			for (int i = 1; i < order + 1; i++) {
				vec2 pos = start + (direction * step * (multiplier * (2.0 * random.NextDouble() - 0.5)));

				deltaDir = (random.Next() == 0) ? right : left;
				pos += deltaDir * (2.0 * (random.NextDouble() - 0.5) * maxDisplacement);

				points[i] = pos;
			}

			spline.Points = points;

			return spline;
		}

		// Returns a number of points along the spline, where points.count = "subdivisions" + 2
		public List<vec2> GeneratePath(int subdivisions) {
			List<vec2> path = new List<vec2>();
			double t = 0.0;
			double step = 1.0 / (subdivisions + 2);

			path.Add(Points[0]);
			for (int i = 0; i < subdivisions; i++) {
				t = step * (i + 1);

				path.Add(this[t]);

			}
			path.Add(Points[PointCount - 1]);

			return path;
		}
		
	}
}
