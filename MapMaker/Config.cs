using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapMaker {
	// Just a list of configurable values
	// Makes all the major "magic numbers" central and lets us add a future config file that this can be read to/saved from
	public static class Config {

		// Each point on the map can spread to either one of its 8 neighbours.
		// However, each point is a pixel. If all are weighted equally, then the corners:
		//	| x . x |
		//	| .   . |
		//  | x . x |
		// Will look more significant on a map, as they are further away. While the distance between the centre and adjacent pixels is (1), the distance between the centre and its corners is (sqrt(2))
		// Therefore, we multiply the spread strength of each corner by (1 / sqrt(2)) to balance this out
		// Alternatively, we can increase the adjacent tile strength. Drawbacks to both:
		// Decreasing corner strength can make it go below 1 (must round to 1 and make it inaccurate, or have no strength)
		// Increasing adjacent tile strength can mess with the balance of the map spreading. It's slightly more accurate though as the upper bounds aren't as much a problem as the lower bounds.
		public static float AdjacentTileStrength = (float)System.Math.Sqrt(2.0f);
		public static float CornerTileStrength = 1.0f / AdjacentTileStrength;

		public static double CurveDivider = 50;
		public static int MapBorderWidth = 25;
		public static byte DefaultSeedStrength = 6;

		public static byte LongerBranchChanceMax = 6;
		public static byte LongerBranchChanceThreshold = 3;

		public static float StdBranchLengthMultMin = 0.5f;
		public static float StdBranchLengthMultMax = 1.0f;
		public static float LongBranchLengthMultMin = 1.0f;
		public static float LongBranchLengthMultMax = 2.0f;

		public static double WorldCurveRange = 1.0 / 8.0;

		public static byte PrefSeedStrMin = 1;
		public static byte PrefSeedStrMax = 6; // 5
		public static byte PrefSeedStrMult = 4; // 4
		public static int SeedStrMutChanceMin = 0;
		public static int SeedStrMutChanceMax = 15; // 15
		public static int SeedStrMutChanceThreshold = 13; // 13

		public static double MntGradientMin = 0.05;
		public static double MntGradientMax = 0.3;
		public static int MntGradientMult = 20;

		public static int MntSlopeChangeChanceMin = 0;
		public static int MntSlopeChangeChanceMax = 3;
		public static int MntSlopeChangeChanceThreshold = 2;

		public static byte DeepOceanCutoff = 40;
		public static byte LandCutoff = 140;
		public static byte MountainCutoff = 145;
		public static byte HeightSnowCutoff = 198;
		public static byte HeightMax = byte.MaxValue;
		public static double IceCutoff = 0.2;
		public static double EquatorCutoff = 0.2;

		public static RangeCollection SeedSpreadRanges = new RangeCollection {
			range = new Range(0, 4),
			ranges = new List<RangeCollectionElement> {
				new RangeCollectionElement(0, 2, 10),
				new RangeCollectionElement(1, 6, 20)
			}
		};
	}
}
