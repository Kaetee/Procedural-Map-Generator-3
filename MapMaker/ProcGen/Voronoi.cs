using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapMaker.ProcGen {
	// I couldn't find any code or explanation of the Voronoi noise generation algorithm
	// But based on what I've seen off the final result (on Google Images), this is my closest approximation of it
	class Voronoi {
		// Inefficient way of doing Voronoi - I only need to store positions
		// However,I mainly use this to create tectonic plates where instead of the distance between points, I care about which point is the closest
		// (i.e., on which tectonic plate the point lies)
		// Could adapt it to instead use the for loop index, but that can come later
		List<List<BaseSeed<byte>>> seeds;
		vec2i mapSize;
		Random random;
		int octaves;

		public Voronoi(int seed) {
			seeds = new List<List<BaseSeed<byte>>>();
			random = new Random(seed);
			octaves = 0;
			mapSize = new vec2i(0, 0);
		}

		// This class didn't need to be too complex, so I haven't looked into making an infinite semi-random permutation of seeds yet,
		// but that could come with the next iteration of the project
		// For now, I'll just preset the map size and octave count
		// Furthermore, making it infinite would require extra work - we don't want the noise to stretch indefinitely, we want it to wrap around the north/south poles
		// So doing it this way *for now* makes more sense
		public void Initialise(int pointCount, vec2i inMapSize, int inOctaves) {
			int pointsPerOctave = pointCount;
			mapSize = inMapSize;

			octaves = inOctaves;

			for (int o = 0; o < octaves; o++) {
				seeds.Add(new List<BaseSeed<byte>>());

				if (o > 0) {
					for (int p = 0; p < seeds[o - 1].Count; p++)
						seeds[o].Add(seeds[o - 1][p]);

					if (o > 1)
						pointsPerOctave *= 2;
				}

				for (int p = 0; p < pointsPerOctave; p++) {
					int x = random.Next(mapSize.x);
					int y = random.Next(mapSize.y);

					seeds[o].Add(new BaseSeed<byte>(x, y, (byte)p));
				}
			}
		}

		public float Noise(int x, int y, float persistence) {
			float totalIntensity = 0.0f;

			float octaveIntensity = 1.0f;
			for (int o = 0; o < octaves; o++) {
				if (o > 0)
					octaveIntensity *= persistence;

				totalIntensity += octaveIntensity;
			}

			octaveIntensity = 1.0f;

			float output = 0.0f;
			for (int o = 0; o < 1; o++) {
				List<BaseSeed<byte>> closestSeed = FindClosestSeed(seeds[o], x, y, mapSize.x, mapSize.y, 2);

				float dSeedsToMidpoint = (float)Maths.GetDistance(closestSeed[0].x, closestSeed[0].y, closestSeed[1].x, closestSeed[1].y) / 2.0f;
				float dCurrentToClosestSeed = (float)DistanceWrapped(x, y, closestSeed[0].x, closestSeed[0].y, mapSize.x, mapSize.y);
				float dCurrentToOtherSeed = (float)DistanceWrapped(x, y, closestSeed[1].x, closestSeed[1].y, mapSize.x, mapSize.y);
				

				// Scale the distance to closest seed as instead the distance to the midpoint
				float midDistance = (dCurrentToClosestSeed + dCurrentToOtherSeed) / 2.0f;

				//output += octaveIntensity * closestSeed[0].data;
				output = (midDistance - dCurrentToClosestSeed) / 20.0f;

				octaveIntensity *= persistence;
			}

			//output /= totalIntensity;
			output = Maths.Min(output, 1.0f);

			return output;
		}

		public byte ClosestSeedID(int x, int y, int octave) {
			List<BaseSeed<byte>> closestSeed = FindClosestSeed(seeds[octave], x, y, mapSize.x, mapSize.y, 3);
			return closestSeed[0].data;
		}

		List<BaseSeed<byte>> FindClosestSeed(List<BaseSeed<byte>> seeds, int x, int y, int sizeX, int sizeY, int count) {
			int[] closestSeed = new int[count];
			float[] closestDistance = new float[count];

			for (int i = 0; i < count; i++) {
				closestSeed[i] = -1;
				closestDistance[i] = float.MaxValue;
			}

			for (int i = 0; i < seeds.Count; i++) {
				float tempDistance = 0;
				int tempIndex = 0;
				float distanceHolder = (float)DistanceWrapped(x, y, seeds[i].x, seeds[i].y, sizeX, sizeY);
				int indexHolder = i;

				for (int j = 0; j < count; j++) {
					if (distanceHolder < closestDistance[j]) {
						tempIndex = closestSeed[j];
						tempDistance = closestDistance[j];

						closestSeed[j] = indexHolder;
						closestDistance[j] = distanceHolder;

						indexHolder = tempIndex;
						distanceHolder = tempDistance;
					}
				}
			}

			List<BaseSeed<byte>> closestSeeds = new List<BaseSeed<byte>>();

			for (int i = 0; i < count; i++)
				closestSeeds.Add(seeds[closestSeed[i]]);

			return closestSeeds;
		}

		// Finds the distance between two points on a map
		// Takes into account that sometimes going over the north/south pole or around the east/west border can lead to shorter distances
		public double DistanceWrapped(int ox, int oy, int dx, int dy, int sizeX, int sizeY) {
			double[] dist = new double[3];
			int ox_n, oy_n, dx_n, dy_n;

			// Order the coordinates so that <origin> is on the left and <displacement> is on the right
			// Saves us making assumptions and extra calculations
			if (ox < dx) {
				ox_n = ox;
				dx_n = dx;
				oy_n = oy;
				dy_n = dy;
			}
			else {
				ox_n = dx;
				dx_n = ox;
				oy_n = dy;
				dy_n = oy;
			}

			if (dx_n - ox_n > sizeX / 2)
				dx_n -= sizeX;

			// dist[0] = basic distance between both points
			dist[0] = Maths.GetDistance(ox_n, oy_n, dx_n, dy_n);

			// If y-coord of the second point is below above the equator (y < sizeY / 2), the closest distance can wrap around the north pole
			// Otherwise, it either wraps around the south pole or is on the equator (doesn't wrap around either pole)
			if (dy_n < sizeY / 2) {
				// dist[1] = distance between both points, if the second point is wrapped around the north pole
				dist[1] = Maths.GetDistance(ox_n, oy_n, dx_n - (sizeX / 2), -dy_n);
				dist[2] = Maths.GetDistance(ox_n, oy_n, dx_n + (sizeX / 2), -dy_n);
			}
			else {
				// dist[1] = distance between both points, if the second point is wrapped around the south pole
				// Get the distance between the point and the pole (sizeY - dy_n) and add it on the other side of the pole (sizeY + (sizeY - dy_n))
				dist[1] = Maths.GetDistance(ox_n, oy_n, dx_n - (sizeX / 2), sizeY + (sizeY - dy_n));
				dist[2] = Maths.GetDistance(ox_n, oy_n, dx_n + (sizeX / 2), sizeY + (sizeY - dy_n));
			}

			// Return the shortest distance calculated
			return Math.Min(Math.Min(dist[0], dist[1]), dist[2]);
		}
		
		// The old code I used inside the GeneratorAdvanced class before splitting it off into its own self-contained procedural generator
		// Haven't gotten to test the new generator alongside the old one much, so I've kept the old code in case I made a mistake transferring it
		/*
		public void GenerateVoronoiNoise(ref BaseMap<byte> map, Random random, int pointCount, int octaves, float persistence) {
			List<List<BaseSeed<byte>>> seeds = new List<List<BaseSeed<byte>>>();
			int pointsPerOctave = pointCount;

			for (int o = 0; o < octaves; o++) {
				seeds.Add(new List<BaseSeed<byte>>());

				if (o > 0) {
					for (int p = 0; p < seeds[o - 1].Count; p++)
						seeds[o].Add(seeds[o - 1][p]);

					if (o > 1)
						pointsPerOctave *= 2;
				}

				for (int p = 0; p < pointsPerOctave; p++) {
					int x = random.Next(map.SizeX);
					int y = random.Next(map.SizeY);

					seeds[o].Add(new BaseSeed<byte>(x, y, (byte)p));
				}
			}

			float octaveIntensity = 1.0f;
			float totalIntensity = 0.0f;

			for (int o = 0; o < octaves; o++) {
				if (o > 0)
					octaveIntensity *= persistence;

				totalIntensity += octaveIntensity;
			}

			octaveIntensity = 1.0f;

			for (int o = 0; o < octaves; o++) {
				if (o > 0)
					octaveIntensity *= persistence;

				for (int i = 0; i < map.SizeY; i++) {
					for (int j = 0; j < map.SizeX; j++) {
						List<BaseSeed<byte>> closestSeed = FindClosestSeed(seeds[o], j, i, map.SizeX, map.SizeY, 3);
						map.Data[j + i * map.SizeX] = closestSeed[0].data;
					}
				}
			}
		}*/
	}
}
