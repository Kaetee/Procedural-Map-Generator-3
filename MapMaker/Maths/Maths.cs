using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapMaker {
	// A list of common maths functions that aren't included in System.Math
	static class Maths {
		public static int Max(int a, int b) {
			return (a > b) ? a : b;
		}

		public static byte Max(byte a, byte b) {
			return (a > b) ? a : b;
		}

		public static float Max(float a, float b) {
			return (a > b) ? a : b;
		}

		public static double Max(double a, double b) {
			return (a > b) ? a : b;
		}

		public static int Min(int a, int b) {
			return (a < b) ? a : b;
		}

		public static byte Min(byte a, byte b) {
			return (a < b) ? a : b;
		}

		public static float Min(float a, float b) {
			return (a < b) ? a : b;
		}

		public static double Min(double a, double b) {
			return (a < b) ? a : b;
		}

		public static double Mix(double a, double b, double strength) {
			return ((1.0f - strength) * a) + (strength * b);
		}

		public static double DegreesToRadians(double angle) {
			return angle * (Math.PI / 180);
		}

		public static double RadiansToDegrees(double angle) {
			return angle * (180 / Math.PI);
		}

		public static double GetDistance(double ox, double oy, double dx, double dy) {
			return Math.Sqrt(((dx - ox) * (dx - ox)) + ((dy - oy) * (dy - oy)));
		}

		public static double GetAngle(int ox, int oy, int d1x, int d1y, int d2x, int d2y) {
			double o_to_d1 = GetDistance(ox, oy, d1x, d1y);
			double o_to_d2 = GetDistance(ox, oy, d2x, d2y);

			double d1_to_d2 = GetDistance(d1x, d1y, d2x, d2y);

			return Math.Acos((Math.Pow(o_to_d1, 2.0) + Math.Pow(o_to_d2, 2.0) - Math.Pow(d1_to_d2, 2.0)) / (2 * o_to_d1 * o_to_d2));
		}

		public static vec2 AngleToPoint(double radius, double theta) {
			double dx = radius * Math.Cos(theta);
			double dy = radius * Math.Sin(-theta);

			return new vec2(dx, dy);
		}

		public static double RoundToSubdivision(double angle, double subdivision) {
			return subdivision * Math.Round(angle / subdivision);
		}
	}
}
