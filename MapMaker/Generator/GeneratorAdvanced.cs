using MapMaker.ProcGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MapMaker {
	// Huge class
	// Mostly necessary - intended to give tools that can be used in many different ways
	// i.e., map spreading, biome generation, mountain range generation, and all that based on supplied seeds
	// The order of these and which tools specifically are used with other tools are meant to be determined by the user
	// That way, I can make a number of GenerateWorld() functions and adjust the values and order of operations easily, until I arrive at a world generator that creates realistic-looking worlds
	// (Note: I may need to upload the presentation/research I did for uni on this, but in short: true macro-randomness = bad, but the appearance of macro-randomness within a predetermined structure = good)
	// (Because, as humans, there's many specific things we expect to see in order to believe a structure (in which case, a world) is "real")

	// Might still be able to break this down into smaller sub-generators in the future, but that wasn't deemed necessary for this step of the project
	public class GeneratorAdvanced : GeneratorBase {
		Perlin perlin;

		GenMap[] subMaps;
		int subMapID;
		int subMapCount;

		public GeneratorAdvanced(int inSizeX, int inSizeY, int threadCount, int randomSeed) {
			random = new Random(randomSeed);
			sizeX = inSizeX;
			sizeY = inSizeY;

			perlin = new Perlin(random.Next(int.MaxValue));

			// The generator uses multiple GenMaps to create the desired effect near the start of the operation
			subMaps = new GenMap[1];
			subMaps[0] = new GenMap(inSizeX, inSizeY);
			subMapCount = 1;

			waitThread = new bool[threadCount];
			threads = new Thread[threadCount];
			threadInstructions = new ThreadInstructions[threadCount];

			for (int i = 0; i < threadCount; i++) {
				int threadID = i;
				waitThread[i] = true;
				threadInstructions[i] = new ThreadInstructions(0, 0, 0, 0, 0, 0, 0, 0);

				threads[i] = new Thread(() => ThreadWorker(threadID));
				threads[i].Start();
			}
		}

		public void WorkOn(int subMapIndex) {
			subMapID = subMapIndex;
		}

		// A simplified function to easily fetch the CurrentMap from the currently worked-on submap
		public ref BiomeMap CurrentMap {
			get => ref subMaps[subMapID].CurrentMap;
		}

		// A simplified function to easily fetch the OtherMap from the currently worked-on submap
		public ref BiomeMap OtherMap {
			get => ref subMaps[subMapID].OtherMap;
		}

		// A simplified function to easily fetch the FunctionMaps from the currently worked-on submap
		public ref List<ABasicMap> FunctionMaps {
			get => ref subMaps[subMapID].function_maps;
		}

		public ref GenMap CurrentSubmap {
			get => ref subMaps[subMapID];
		}

		public ref GenMap[] SubMaps {
			get => ref subMaps;
		}

		// We're going to use a blur function to smooth the map
		// We don't need the result to be particularly precise; this isn't something the user will see
		// Nor will it *obviously* affect the resulting map. It's just a nice touch
		// So, we can just blur it in two runs. Horizontal first, then blur the result vertically
		// The result is a circle-like smoothing function
		void Blur(ref BaseMap<float> map, int radius, float dropoff, int repetitions) {
			for (int r = 0; r < repetitions; r++) {
				BlurHorizontal(ref map, radius, dropoff);
				BlurVertical(ref map, radius, dropoff);
			}
		}

		void BlurVertical(ref BaseMap<float> map, int radius, float dropoff) {
			int xCarry = 0;
			int yCarry = 0;

			BaseMap<float> tempMap = map.Clone();

			// save processing: precalculate the multipliers
			float[] multiplier = new float[2 * radius + 1];
			float totalStrength = 0.0f;
			for (int i = -radius; i <= radius; i++) {
				multiplier[radius + i] = (float)Math.Pow(dropoff, Math.Abs(i));
				totalStrength += multiplier[radius + i];
			}

			for (int i = 0; i < multiplier.Length; i++)
				multiplier[i] /= totalStrength;

			for (int i = 0; i < map.SizeY; i++) {
				for (int j = 0; j < map.SizeX; j++) {
					float average = 0.0f;
					// For each point, check the surrounding <radius>
					for (int y = -radius; y <= radius; y++) {
						yCarry = 0;
						FixRange(j, i + y, map.SizeX, map.SizeY, ref xCarry, ref yCarry, true);

						average += multiplier[y + radius] * map.Data[(xCarry + j) + ((y + yCarry + i) * map.SizeX)];
					}

					tempMap.Data[j + i * map.SizeX] = average;
					//tempMap.Data[j + i * map.SizeX] = average / (2.0f * radius + 1.0f);
				}
			}

			map = tempMap;
		}

		void BlurHorizontal(ref BaseMap<float> map, int radius, float dropoff) {
			int xCarry = 0;
			int yCarry = 0;

			BaseMap<float> tempMap = map.Clone();

			// save processing: precalculate the multipliers
			float[] multiplier = new float[2 * radius + 1];
			float totalStrength = 0.0f;
			for (int i = -radius; i <= radius; i++) {
				multiplier[radius + i] = (float)Math.Pow(dropoff, Math.Abs(i));
				totalStrength += multiplier[radius + i];
			}

			for (int i = 0; i < multiplier.Length; i++)
				multiplier[i] /= totalStrength;

			for (int i = 0; i < map.SizeY; i++) {
				for (int j = 0; j < map.SizeX; j++) {
					float average = 0.0f;

					// For each point, check the surrounding <radius>
					for (int x = -radius; x <= radius; x++) {
						xCarry = 0;
						FixRange(j + x, i, map.SizeX, map.SizeY, ref xCarry, ref yCarry, true);

						average += multiplier[x + radius] * map.Data[(x + xCarry + j) + ((yCarry + i) * map.SizeX)];
					}

					tempMap.Data[j + i * map.SizeX] = average;
				}
			}

			map = tempMap;
		}

		// Perlin or Map Fill
		public void GenerateGroundHardness(int sectionsX, int sectionsY, int softSeedsPerSection, int midSeedsPerSection, int hardSeedsPerSection, float softRange, float midRange, float hardRange) {
			int softSeedCount = sectionsX * sectionsY * (softSeedsPerSection);
			int midSeedCount = sectionsX * sectionsY * (midSeedsPerSection);
			int hardSeedCount = sectionsX * sectionsY * (hardSeedsPerSection);

			int sectionSizeX = sizeX / sectionsX;
			int sectionSizeY = sizeY / sectionsY;

			List<BaseSeed<float>> softSeeds = new List<BaseSeed<float>>();
			List<BaseSeed<float>> midSeeds = new List<BaseSeed<float>>();
			List<BaseSeed<float>> hardSeeds = new List<BaseSeed<float>>();

			float hardness;
			for (int i = 0; i < softSeedCount; i++) {
				int x = random.Next(sectionSizeX);
				int y = random.Next(sectionSizeY);
				hardness = softRange * (float)random.NextDouble();

				softSeeds.Add(new BaseSeed<float>(x, y, (hardness)));
			}

			for (int i = 0; i < midSeedCount; i++) {
				int x = random.Next(sectionSizeX);
				int y = random.Next(sectionSizeY);
				hardness = 0.45f + (midRange * (2.0f * (float)random.NextDouble() - 1.0f));

				midSeeds.Add(new BaseSeed<float>(x, y, (hardness)));
			}

			for (int i = 0; i < hardSeedCount; i++) {
				int x = random.Next(sectionSizeX);
				int y = random.Next(sectionSizeY);
				hardness = 1.0f - (hardRange * (float)random.NextDouble());

				hardSeeds.Add(new BaseSeed<float>(x, y, (hardness)));
			}

			BaseMap<float>[] tempMaps = new BaseMap<float>[2];
			tempMaps[0] = new BaseMap<float>(sizeX, sizeY, -1.0f);

			int currentSoftSeed = 0;
			int currentMidSeed = 0;
			int currentHardSeed = 0;

			for (int sectionY = 0; sectionY < sectionsY; sectionY++) {
				for (int sectionX = 0; sectionX < sectionsX; sectionX++) {
					for (int seedIndex = 0; seedIndex < softSeedsPerSection; seedIndex++) {
						BaseSeed<float> seed = softSeeds[currentSoftSeed++];
						tempMaps[0].Data[(seed.x + (sectionX * sectionSizeX)) + (seed.y + sectionY * sectionSizeY) * sizeX] = seed.data;
					}

					for (int seedIndex = 0; seedIndex < midSeedsPerSection; seedIndex++) {
						BaseSeed<float> seed = midSeeds[currentMidSeed++];
						tempMaps[0].Data[(seed.x + (sectionX * sectionSizeX)) + (seed.y + sectionY * sectionSizeY) * sizeX] = seed.data;
					}

					for (int seedIndex = 0; seedIndex < hardSeedsPerSection; seedIndex++) {
						BaseSeed<float> seed = hardSeeds[currentHardSeed++];
						tempMaps[0].Data[(seed.x + (sectionX * sectionSizeX)) + (seed.y + sectionY * sectionSizeY) * sizeX] = seed.data;
					}
				}
			}

			tempMaps[1] = tempMaps[0].Clone();
			bool mapFilled = false;
			List<float> potentialValues = new List<float>();

			while (!mapFilled) {
				mapFilled = true;
				tempMaps[0] = tempMaps[1].Clone();

				for (int i = 0; i < sizeY; i++) {
					for (int j = 0; j < sizeX; j++) {
						potentialValues.Clear();

						if (tempMaps[0].Data[j + i * sizeX] == -1.0f) {
							mapFilled = false;

							// check surrounding area
							for (int y = -1; y < 2; y++) {
								for (int x = -1; x < 2; x++) {
									int xCarry = 0;
									int yCarry = 0;
									FixRange(x + j, y + i, sizeX, sizeY, ref xCarry, ref yCarry, true);

									hardness = tempMaps[0].Data[(x + xCarry + j) + ((y + yCarry + i) * sizeX)];

									if (hardness >= 0.0f) {
										potentialValues.Add(hardness);
									}
								}
							}

							if (potentialValues.Count > 0)
								tempMaps[1].Data[j + i * sizeX] = potentialValues[random.Next(potentialValues.Count)];
						}
					}
				}
			}

			Blur(ref tempMaps[0], 3, 0.75f, 6);
			FunctionMaps[GenMap.MAP_BEDROCK_HARDNESS] = tempMaps[0];
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
		}

		// Voronoi or seeds-based fill
		// Ended up going for Voronoi; the seed-based map fill created more interesting shapes,
		// but it also created really ragged tectonic borders that too far too most post-processing to smooth out
		// The shape wasn't worth the extra processing
		public void GenerateTectonicPlates(int sectionsX, int sectionsY, int seedsPerSection, int radius) {
			BaseMap<byte> tempMap = new BaseMap<byte>(sizeX, sizeY, byte.MaxValue);

			Voronoi voronoi = new Voronoi(random.Next());
			voronoi.Initialise(sectionsX * sectionsY * seedsPerSection, new vec2i(sizeX, sizeY), 1);

			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {
					tempMap[x + y * sizeX] = voronoi.ClosestSeedID(x, y, 0);
				}
			}

			GenerateVoronoiNoise(ref tempMap, random, sectionsX * sectionsY * seedsPerSection, 1, 1.0f);
			FunctionMaps[GenMap.MAP_TECTONIC_PLATES] = tempMap;
		}

		public void GeneratePerlinNoise(ref BaseMap<float> map, int z, float scaleX, float scaleY, int octaves, float persistence) {
			float indexX;
			float indexY;

			float min = float.MaxValue;
			float max = float.MinValue;

			for (int i = 0; i < map.SizeY; i++) {
				indexY = ((float)i) / ((float)map.SizeY);

				for (int j = 0; j < map.SizeX; j++) {
					indexX = ((float)j) / ((float)map.SizeX);

					map[j + i * map.SizeX] = (float)Math.Min(Math.Max(perlin.Noise(indexX * scaleX, indexY * scaleY, z, octaves, persistence) + 0.5, 0.0), 1.0);

					if (map[j + i * map.SizeX] > max)
						max = map[j + i * map.SizeX];
					else if (map[j + i * map.SizeX] < min)
						min = map[j + i * map.SizeX];
				}
			}
		}

		public void GenerateNoise(float scaleX, float scaleY, int octaves, float persistence) {
			BaseMap<float> noise_0 = new BaseMap<float>(sizeX, sizeY);
			BaseMap<float> noise_1 = new BaseMap<float>(sizeX, sizeY);
			BaseMap<float> noise_2 = new BaseMap<float>(sizeX, sizeY);
			BaseMap<float> noise_3 = new BaseMap<float>(sizeX, sizeY);
			BaseMap<float> noise_4 = new BaseMap<float>(sizeX, sizeY);

			BaseMap<float> noise_map = (BaseMap<float>)FunctionMaps[GenMap.MAP_NOISE_0];

			GeneratePerlinNoise(ref noise_0, 5, scaleX, scaleY, octaves, persistence);
			GeneratePerlinNoise(ref noise_1, 5, scaleX * 2.0f, scaleY * 2.0f, octaves, persistence);
			GeneratePerlinNoise(ref noise_2, 5, scaleX * 4.0f, scaleY * 4.0f, octaves, persistence);
			GeneratePerlinNoise(ref noise_3, 5, scaleX * 8.0f, scaleY * 8.0f, octaves, persistence);
			GeneratePerlinNoise(ref noise_4, 5, scaleX * 16.0f, scaleY * 16.0f, octaves, persistence);

			float mult = 0.8f;

			float mult_0 = mult;
			float mult_1 = mult_0 * mult;
			float mult_2 = mult_1 * mult;
			float mult_3 = mult_2 * mult;
			float mult_4 = mult_3 * 1.0f;

			float total = 5.0f;
			total = mult_0 + mult_1 + mult_2 + mult_3 + mult_4;

			float min = float.MaxValue;
			float max = float.MinValue;
			for (int i = 0; i < sizeY; i++) {
				for (int j = 0; j < sizeX; j++) {
					noise_map[j + i * sizeX] = ((mult_0 * noise_0[j + i * sizeX])
										   + (mult_1 * noise_1[j + i * sizeX])
										   + (mult_2 * noise_2[j + i * sizeX])
										   + (mult_3 * noise_3[j + i * sizeX])
										   + (mult_4 * noise_4[j + i * sizeX])) / total;
					noise_map[j + i * sizeX] = (float)Math.Pow(noise_map[j + i * sizeX], 2) * 1.5f;

					if (max < noise_map[j + i * sizeX])
						max = noise_map[j + i * sizeX];

					if (min > noise_map[j + i * sizeX])
						min = noise_map[j + i * sizeX];
				}
			}

			for (int i = 0; i < sizeY; i++) {
				for (int j = 0; j < sizeX; j++) {
					noise_map[j + i * sizeX] = (noise_map[j + i * sizeX] - min) / (max - min);
				}
			}
		}

		vec2 ToDisplacement(double radius, double theta) {
			double dx = radius * Math.Cos(theta);
			double dy = radius * Math.Sin(-theta);

			return new vec2(dx, dy);
		}

		bool IsInList(ref List<vec2i> list, vec2i point) {
			foreach (vec2i v in list)
				if (v == point)
					return true;

			return false;
		}

		// it's very unlikely that the tectonic plate continues in the same direction as lastDirection
		// Therefore, we consecutively check the surrounding pixels (in a circle/cone) to see where the next *actual* pixel falls
		// To stop directional-preference manifesting on the map, we:
		//    1: Determine the first direction we test at random
		//    2: Test each direction one step at a time (+1 right, +1 left, +2 right, +2 left, (...))
		bool GetNextAngle(ref List<vec2i> points, vec2i point, ref vec2 displacement, int radius, ref double lastDirection, double divisionAngle) {
			double direction = (random.Next(2) == 0) ? 1.0 : -1.0;
			displacement = ToDisplacement(radius, lastDirection);

			if (IsInList(ref points, point + (vec2i)displacement))
				return true;

			int anglesPerSide = 4 * radius;
			for (int angle = 1; angle < anglesPerSide; angle++) {
				for (int angleDir = 0; angleDir < 2; angleDir++) {
					displacement = ToDisplacement(radius, lastDirection + direction * divisionAngle * (angle));
					displacement.Round(divisionAngle);

					if (IsInList(ref points, point + (vec2i)displacement))
						return true;

					direction *= -1;
				}
			}

			return false;
		}

		HeightSeed<SpreadDirs> GenerateRandomSeed(byte inHeight, int posX, int posY, byte strengthBaseline, bool mutate) {
			HeightSeed<SpreadDirs> seed = new HeightSeed<SpreadDirs>(posX, posY, inHeight, new SpreadDirs());

			int preferred = random.Next(0, 4);
			byte preferredChance = (byte)(random.Next(Config.PrefSeedStrMin, Config.PrefSeedStrMax) * Config.PrefSeedStrMult);

			switch (preferred) {
				case 0:
					//seed.data[2, 2] = preferredChance;
					//seed.data[0, 0] = preferredChance;
					seed.data[2, 2] = (byte)Math.Max(preferredChance * Config.CornerTileStrength, 1);
					seed.data[0, 0] = (byte)Math.Max(preferredChance * Config.CornerTileStrength, 1);
					break;
				case 1:
					//seed.data[0, 1] = (byte)(preferredChance * Config.AdjacentTileStrength);
					//seed.data[2, 1] = (byte)(preferredChance * Config.AdjacentTileStrength);
					seed.data[0, 1] = preferredChance;
					seed.data[2, 1] = preferredChance;
					break;
				case 2:
					//seed.data[0, 2] = preferredChance;
					//seed.data[2, 0] = preferredChance;
					seed.data[0, 2] = (byte)Math.Max(preferredChance * Config.CornerTileStrength, 1);
					seed.data[2, 0] = (byte)Math.Max(preferredChance * Config.CornerTileStrength, 1);
					break;
				case 3:
					//seed.data[1, 2] = (byte)(preferredChance * Config.AdjacentTileStrength);
					//seed.data[1, 0] = (byte)(preferredChance * Config.AdjacentTileStrength);
					seed.data[1, 2] = preferredChance;
					seed.data[1, 0] = preferredChance;
					break;
				default:
					Console.WriteLine("ERROR :: GeneratorAdvanced.GenerateRandomSeed:: preferred < 0 || preferred > 3 [" + preferred + "]");
					break;
			}

			if (mutate && random.Next(Config.SeedStrMutChanceMin, Config.SeedStrMutChanceMax) > Config.SeedStrMutChanceThreshold)
				seed.data.Strength = (byte)(random.Next(1, 4) * 3);
			else
				seed.data.Strength = strengthBaseline;

			return seed;

		}

		vec2i GenerateDependentLocalSeed(vec2i mapSize, ivec2<RangeD> localRange, List<vec2i> origins) {
			double displacementDistanceX = localRange.x.min + (random.NextDouble() * localRange.x.max);
			double displacementDistanceY = localRange.y.min + (random.NextDouble() * localRange.y.max);

			double displacementAngle = random.NextDouble() * 2.0 * Math.PI;

			vec2 displacement = ToDisplacement(1.0, displacementAngle);

			double displacementX = (int)(displacement.x * displacementDistanceX);
			double displacementY = (int)(displacement.y * displacementDistanceY);

			// The first <globalOrigins> array slots in gOrigins will be filled with global cluster origins
			int parentID = random.Next(origins.Count);
			int parentX = origins[parentID].x;
			int parentY = origins[parentID].y;

			int newX = (int)(parentX + displacementX);
			int newY = (int)(parentY + displacementY);

			int x = newX;
			int y = newY;

			int xCarry = 0;
			int yCarry = 0;
			FixRange(x, y, mapSize.x, mapSize.y, ref xCarry, ref yCarry, false);

			return new vec2i(x + xCarry, y + yCarry);
		}

		vec2i GenerateIndependentGlobalSeed(vec2i mapSize, vec2i sectionCount, vec2i sectionDisplacement, ivec2<RangeD> globalRange, int globalOriginCount, bool allSectionsFilled, bool[,] filledSections) {
			int sectionX = random.Next(sectionCount.x);
			int sectionY = random.Next(sectionCount.y);

			// Try to fill all sections
			// If all sections can be filled (there are more clusters to be generated than sections that exist)
			// And not all sections have yet been filled, then
			// find an empty slot.
			//if (!allSectionsFilled && (sectionCount.x * sectionCount.y) <= globalOriginCount) {
			//	while (!filledSections[sectionX, sectionY] == false) {
			//		sectionX = random.Next(sectionCount.x);
			//		sectionY = random.Next(sectionCount.y);
			//	}
			//}

			int sectionSizeX = mapSize.x / sectionCount.x;
			int sectionSizeY = mapSize.y / sectionCount.y;

			int x = (int)(sectionDisplacement.x + (sectionSizeX * sectionX) + (globalRange.x.max * randomDoubleFullRange()));
			int y = (int)(sectionDisplacement.y + (sectionSizeY * sectionY) + (globalRange.y.max * randomDoubleFullRange()));

			int xCarry = 0;
			int yCarry = 0;
			FixRange(x, y, mapSize.x, mapSize.y, ref xCarry, ref yCarry, false);

			return new vec2i(x + xCarry, y + yCarry);
		}

		bool ValidateLatitude(int latitude, int sizeY, ref int northSeedCount, ref int southSeedCount, Range northSeedRange, Range southSeedRange) {
			// tells us which y-quarter the seed is in
			// Where the 0th quarter = north, 1st quarter = mid, 2nd quarter = south and 3rd quarter = unspawnable
			// (seeds can only spawn from y = 0 to y = (3 * (sizeY / 4) - 1))
			// (No seeds can spawn between y = (3 * (sizeY / 4)) and y = (sizeY - 1))
			int seedYQuarterPosition = (latitude / (sizeY / 4));

			switch (seedYQuarterPosition) {
				// If this is a northern seed
				case 0:
					// If we NEED more northern seeds
					if (northSeedCount < northSeedRange.min) {
						northSeedCount++;
						return true;
					}
					// If we CAN HAVE more northern seeds
					else if (northSeedCount < northSeedRange.max) {
						northSeedCount++;
						return true;
					}
					break;
				// If this is a mid seed
				case 1:
					// If we NEED more northern or southern seeds, force them to spawn first
					if (northSeedCount >= northSeedRange.min || southSeedCount >= southSeedRange.min) {
						return true;
					}
					break;
				// If this is a southern (acceptable) seed
				case 2:
					// If we NEED more southern seeds
					if (southSeedCount < southSeedRange.min) {
						southSeedCount++;
						return true;
					}
					// If we CAN HAVE more southern seeds
					else if (southSeedCount < southSeedRange.max) {
						southSeedCount++;
						return true;
					}
					// If we CANNOT have any more sourthern seeds
					break;
				// If this is a southern (unacceptable) seed
				default:
					break;
			}

			return false;
		}

		public void GenerateUniformSeeds(Range heightSpawnRange, Range heightRange, int globalOriginCount, int localOriginCount, Range northSeedRange, Range southSeedRange, vec2i sectionCount, ivec2<RangeD> globalSeedRange, ivec2<RangeD> localSeedRange, ref List<List<SeedBranch<SpreadDirs>>> branchQueue) {
			ref BiomeMap currentMap = ref CurrentMap;

			vec2i mapSize = new vec2i(currentMap.SizeX, currentMap.SizeY);

			int sectionSizeX = currentMap.SizeX / sectionCount.x;
			int sectionSizeY = currentMap.SizeY / sectionCount.y;

			vec2i sectionDisplacement = new vec2i(0, sectionSizeY / 2);

			ivec2<RangeD> localRange = new ivec2<RangeD>(
				new RangeD(currentMap.SizeX * localSeedRange.x.min, currentMap.SizeX * localSeedRange.x.max),
				new RangeD(currentMap.SizeY * localSeedRange.y.min, currentMap.SizeY * localSeedRange.y.max));
			ivec2<RangeD> globalRange = new ivec2<RangeD>(
				new RangeD(sectionSizeX * globalSeedRange.x.min, sectionSizeX * globalSeedRange.x.max),
				new RangeD(sectionSizeY * globalSeedRange.y.min, sectionSizeY * globalSeedRange.y.max));
			
			bool[,] filledSections = new bool[sectionCount.x, sectionCount.y];
			for (int i = 0; i < sectionCount.y; i++)
				for (int j = 0; j < sectionCount.x; j++)
					filledSections[j, i] = false;
			
			int southSeedCount = 0;
			int northSeedCount = 0;
			bool foundCorrectLatitude;
			bool allSectionsFilled = false;
			int originCount = 0;
			List<vec2i> origins = new List<vec2i>();
			vec2i pos = new vec2i(0, 0);

			List<SeedBranch<SpreadDirs>> branch = new List<SeedBranch<SpreadDirs>>();

			for (int a = 0; a < (globalOriginCount + localOriginCount); a++) {
				foundCorrectLatitude = false;

				// Confirm whether or not all sections already have a cluster in them.
				if (!allSectionsFilled) {
					allSectionsFilled = true;

					for (int i = 0; i < sectionCount.y; i++)
						for (int j = 0; j < sectionCount.x; j++)
							if (filledSections[j, i] == false)
								allSectionsFilled = false;
				}

				// Naturally, more "earth-like" worlds seem more realistic to us.
				// This is greatly helped by a similar distribution of land.
				// Therefore, we generate most of the land in the northern hemisphere
				// i.e., correctLatitude = either "north" or "south," depending on how many southern seeds we have left
				while (!foundCorrectLatitude) {
					// In most cases, place the new seed in a set range around an old seed
					if (originCount >= globalOriginCount) {
						pos = GenerateDependentLocalSeed(mapSize, localRange, origins);
					}
					// If there aren't enough seeds to compare to yet, spawn the seed randomly
					else {
						//sectionDisplacement = new vec2i((int)(sectionSizeX * random.NextDouble()), sectionSizeY / 2);
						pos = GenerateIndependentGlobalSeed(mapSize, sectionCount, sectionDisplacement, globalRange, globalOriginCount, allSectionsFilled, filledSections);
					}
					
					foundCorrectLatitude = ValidateLatitude(pos.y, sizeY, ref northSeedCount, ref southSeedCount, northSeedRange, southSeedRange);
				}
				
				int height = (heightSpawnRange.min + random.Next(heightSpawnRange.max - heightSpawnRange.min));

				HeightSeed<SpreadDirs> seed = GenerateRandomSeed((byte)height, pos.x, pos.y, Config.DefaultSeedStrength, true);

				branch.Add(new SeedBranch<SpreadDirs>(seed));

				origins.Add(new vec2i(seed.x, seed.y));
				originCount++;
			}

			branchQueue.Add(branch);
		}

		// Even when branches spawn within the correct lattitude, they can spawn many of their sub-branch seeds far below where they're allowed
		// Therefore, we must correct any mis-spawned branches
		// Put simply, this finds the difference between the maximum spawnable height in the map (maxAcceptableY) and the heighest-y (furthest south) seed in the branch (maxY),
		// And then, if maxY is further south than maxAcceptableY (maxY > maxAcceptableY), the whole branch is moved by the difference between them
		// (for each seed in branch, seed.y += (maxAcceptableY - maxY)
		void FixBranchLatitude(vec2i mapSize, ref List<SeedBranch<SpreadDirs>> branch) {
			int maxY = -1;

			List<vec2i> coords = new List<vec2i>();

			for (int i = 0; i < branch.Count; i++)
				coords.AddRange(branch[i].GetAllCoordinates());

			foreach (vec2i p in coords)
				if (p.y > maxY)
					maxY = p.y;

			int maxAcceptableY = (3 * sizeY / 4);
			maxAcceptableY += sizeY / 8;

			if (maxY > maxAcceptableY) {
				vec2i displacement = new vec2i(0, maxAcceptableY - maxY);

				for (int i = 0; i < branch.Count; i++)
					branch[i].TranslateBranch(displacement, mapSize);
			}
		}

		// Determines how long the branch will be.
		// There is a chance that each branch can either be between (0.5 * maxLength -> maxLength) or (maxLength -> maxLength * 2) seeds long
		int GenerateBranchLength(int maxSeedLength) {
			if (random.Next(Config.LongerBranchChanceMax) > Config.LongerBranchChanceThreshold)
				return random.Next((int)(Config.LongBranchLengthMultMin * maxSeedLength), (int)(Config.LongBranchLengthMultMax * maxSeedLength));
			else
				return random.Next((int)(Config.StdBranchLengthMultMin * maxSeedLength), (int)(Config.StdBranchLengthMultMax * maxSeedLength));
		}

		// The seeds spawn independently of each other, but in clusters
		// "global origins" are seeds spawned completely away from other clusters of seeds
		// "local origins" are seeds spawned around global origins
		public void GenerateSeedBranches(Range heightSpawnRange, Range heightRange, int heightChangeDirection, int globalOriginCount, int localOriginCount, Range northSeedRange, Range southSeedRange, vec2i sectionCount, ivec2<RangeD> globalSeedRange, ivec2<RangeD> localSeedRange, ref List<List<SeedBranch<SpreadDirs>>> branchQueue, int maxSeedLength, double decayRate) {
			ref BiomeMap currentMap = ref CurrentMap;

			vec2i mapSize = new vec2i(currentMap.SizeX, currentMap.SizeY);

			int sectionSizeX = currentMap.SizeX / sectionCount.x;
			int sectionSizeY = currentMap.SizeY / sectionCount.y;
			
			//int sectionDisplacementX = (int)(sectionSizeX * random.NextDouble());
			vec2i sectionDisplacement = new vec2i(0, sectionSizeY / 2);

			ivec2<RangeD> localRange = new ivec2<RangeD>(
				new RangeD(currentMap.SizeX * localSeedRange.x.min, currentMap.SizeX * localSeedRange.x.max),
				new RangeD(currentMap.SizeY * localSeedRange.y.min, currentMap.SizeY * localSeedRange.y.max));
			ivec2<RangeD> globalRange = new ivec2<RangeD>(
				new RangeD(sectionSizeX * globalSeedRange.x.min, sectionSizeX * globalSeedRange.x.max),
				new RangeD(sectionSizeY * globalSeedRange.y.min, sectionSizeY * globalSeedRange.y.max));

			bool[,] filledSections = new bool[sectionCount.x, sectionCount.y];
			for (int i = 0; i < sectionCount.y; i++)
				for (int j = 0; j < sectionCount.x; j++)
					filledSections[j, i] = false;
			
			int southSeedCount = 0;
			int northSeedCount = 0;
			bool foundCorrectLatitude;
			bool allSectionsFilled = false;
			int originCount = 0;
			List<vec2i> origins = new List<vec2i>();
			vec2i pos = new vec2i(0, 0);

			for (int a = 0; a < (globalOriginCount + localOriginCount); a++) {
				foundCorrectLatitude = false;

				// Confirm whether or not all sections already have a cluster in them.
				if (!allSectionsFilled) {
					allSectionsFilled = true;

					for (int i = 0; i < sectionCount.y; i++)
						for (int j = 0; j < sectionCount.x; j++)
							if (filledSections[j, i] == false)
								allSectionsFilled = false;
				}

				// Naturally, more "earth-like" worlds seem more realistic to us.
				// This is greatly helped by a similar distribution of land.
				// Therefore, we generate most of the land in the northern hemisphere
				// i.e., correctLatitude = either "north" or "south," depending on how many southern seeds we have left
				while (!foundCorrectLatitude) {
					// In most cases, place the new seed in a set range around an old seed
					if (originCount >= globalOriginCount) {
						pos = GenerateDependentLocalSeed(mapSize, localRange, origins);
					}
					// If there aren't enough seeds to compare to yet, spawn the seed randomly
					else {
						//sectionDisplacement = new vec2i((int)(sectionSizeX * random.NextDouble()), sectionSizeY / 2);
						pos = GenerateIndependentGlobalSeed(mapSize, sectionCount, sectionDisplacement, globalRange, globalOriginCount, allSectionsFilled, filledSections);
					}

					foundCorrectLatitude = ValidateLatitude(pos.y, sizeY, ref northSeedCount, ref southSeedCount, northSeedRange, southSeedRange);
				}

				double gradient = randomDouble(Config.MntGradientMin, Config.MntGradientMax);
				int height = (heightSpawnRange.min + random.Next(heightSpawnRange.max - heightSpawnRange.min));

				HeightSeed<SpreadDirs> seed = GenerateRandomSeed((byte)height, pos.x, pos.y, Config.DefaultSeedStrength, true);
				List<SeedBranch<SpreadDirs>> branch = new List<SeedBranch<SpreadDirs>>();
				branch.Add(new SeedBranch<SpreadDirs>(seed));

				// Create branching seeds
				int seedCount = GenerateBranchLength(maxSeedLength);

				// A recursive function; spawns sub-branches recursively, reducing seedCount until it runs out and branches cease spawning
				GenerateSeedSubBranches(heightRange, height, heightChangeDirection, new vec2i(pos.x, pos.y), seedCount, random.Next(0, 4), ref branch, gradient, decayRate);

				FixBranchLatitude(mapSize, ref branch);

				branchQueue.Add(branch);

				// Add all seeds from the branch to the list of global origins
				foreach (SeedBranch<SpreadDirs> sb in branch)
					origins.AddRange(sb.GetAllCoordinates());
				originCount++;
			}

			Console.WriteLine("Finished Generating Mountain Branches");
		}

		public List<vec2i> GetGreaterThanRange(int range) {
			ref BiomeMap currentMap = ref CurrentMap;
			ref BiomeMap otherMap = ref OtherMap;
			
			List<vec2i> points = new List<vec2i>();

			for (int y = 0; y < currentMap.SizeY; y++) {
				for (int x = 0; x < currentMap.SizeX; x++) {
					bool containsLand = false;
					
					for (int i = -range; i <= range; i++) {
						for (int j = -range; j <= range; j++) {
							int xCarry = 0;
							int yCarry = 0;

							FixRange(x + j, y + i, currentMap.SizeX, currentMap.SizeY, ref xCarry, ref yCarry, true);

							double distanceToPoint = Maths.GetDistance(0, 0, j, i);

							if (distanceToPoint <= range) {
								if (currentMap[(x + xCarry + j) + (y + yCarry + i) * currentMap.SizeX] >= Config.LandCutoff) {
									containsLand = true;
								}
							}
						}
					}
					
					if (!containsLand) {
						points.Add(new vec2i(x, y));
						otherMap[x + y * currentMap.SizeX] = 0;
					}
					else {
						otherMap[x + y * currentMap.SizeX] = 255;
					}
				}
			}

			currentMap = otherMap.Clone();

			return points;
		}

		public List<vec2i> GetWithinRange(Range range) {
			ref BiomeMap currentMap = ref CurrentMap;
			
			List<vec2i> points = new List<vec2i>();

			for (int y = 0; y < currentMap.SizeY; y++) {
				for (int x = 0; x < currentMap.SizeX; x++) {
					bool isOutOfMinRange = true;
					bool isWithinMaxRange = false;

					for (int i = -range.max; i <= range.max; i++) {
						for (int j = -range.max; j <= range.max; j++) {
							int xCarry = 0;
							int yCarry = 0;

							FixRange(x + j, y + i, currentMap.SizeX, currentMap.SizeY, ref xCarry, ref yCarry, true);
							
							double distanceToPoint = Maths.GetDistance(0, 0, j, i);

							if (distanceToPoint <= range.max && distanceToPoint > range.min) {
								if (currentMap[(x + xCarry + j) + (y + yCarry + i) * currentMap.SizeX] >= Config.LandCutoff)
									isWithinMaxRange = true;
							}
							else if (distanceToPoint <= range.min) {
								if (currentMap[(x + xCarry + j) + (y + yCarry + i) * currentMap.SizeX] >= Config.LandCutoff)
									isOutOfMinRange = false;
							}
						}
					}

					if (isOutOfMinRange && isWithinMaxRange)
						points.Add(new vec2i(x, y));
				}
			}

			return points;
		}

		void RemoveInRange(vec2i point, ref List<vec2i> points, int range) {
			int i = 0;

			while (i < points.Count) {
				if (point.GetDistance(points[i]) <= range)
					points.RemoveAt(i);
				else
					i++;
			}
		}

		public void GenerateOrbitSeedBranches(Range heightSpawnRange, Range heightRange, int heightChangeDirection, int globalOriginCount, int localOriginCount, Range northSeedRange, Range southSeedRange, vec2i sectionCount, ivec2<RangeD> globalSeedRange, ivec2<RangeD> localSeedRange, Range landSpawnDistance, ref List<List<SeedBranch<SpreadDirs>>> branchQueue, int maxSeedLength, double decayRate) {
			ref BiomeMap currentMap = ref CurrentMap;

			vec2i mapSize = new vec2i(currentMap.SizeX, currentMap.SizeY);

			int sectionSizeX = currentMap.SizeX / sectionCount.x;
			int sectionSizeY = currentMap.SizeY / sectionCount.y;

			//int sectionDisplacementX = (int)(sectionSizeX * random.NextDouble());
			vec2i sectionDisplacement = new vec2i(0, sectionSizeY / 2);

			ivec2<RangeD> localRange = new ivec2<RangeD>(
				new RangeD(currentMap.SizeX * localSeedRange.x.min, currentMap.SizeX * localSeedRange.x.max),
				new RangeD(currentMap.SizeY * localSeedRange.y.min, currentMap.SizeY * localSeedRange.y.max));
			ivec2<RangeD> globalRange = new ivec2<RangeD>(
				new RangeD(sectionSizeX * globalSeedRange.x.min, sectionSizeX * globalSeedRange.x.max),
				new RangeD(sectionSizeY * globalSeedRange.y.min, sectionSizeY * globalSeedRange.y.max));

			bool[,] filledSections = new bool[sectionCount.x, sectionCount.y];
			for (int i = 0; i < sectionCount.y; i++)
				for (int j = 0; j < sectionCount.x; j++)
					filledSections[j, i] = false;

			int southSeedCount = 0;
			int northSeedCount = 0;
			bool foundCorrectLatitude;
			bool allSectionsFilled = false;
			int originCount = 0;
			List<vec2i> origins = new List<vec2i>();
			vec2i pos = new vec2i(0, 0);

			List<vec2i> validPoints = GetWithinRange(landSpawnDistance);
			List<vec2i> validPointsBackup = GetWithinRange(new Range(2, 8));

			for (int a = 0; a < (globalOriginCount + localOriginCount); a++) {
				foundCorrectLatitude = false;

				// Confirm whether or not all sections already have a cluster in them.
				if (!allSectionsFilled) {
					allSectionsFilled = true;

					for (int i = 0; i < sectionCount.y; i++)
						for (int j = 0; j < sectionCount.x; j++)
							if (filledSections[j, i] == false)
								allSectionsFilled = false;
				}

				// Naturally, more "earth-like" worlds seem more realistic to us.
				// This is greatly helped by a similar distribution of land.
				// Therefore, we generate most of the land in the northern hemisphere
				// i.e., correctLatitude = either "north" or "south," depending on how many southern seeds we have left
				while (!foundCorrectLatitude) {
					// In most cases, place the new seed in a set range around an old seed
					if (validPoints.Count == 0) {
						Console.WriteLine("GenerateOrbitSeedBranches :: Out of spawnable points");
						return;
					}

					if (originCount >= globalOriginCount) {
						pos = GenerateDependentLocalSeed(mapSize, localRange, origins);
					}
					// If there aren't enough seeds to compare to yet, spawn the seed randomly
					else {
						//sectionDisplacement = new vec2i((int)(sectionSizeX * random.NextDouble()), sectionSizeY / 2);
						int index = random.Next(validPoints.Count);

						pos = validPoints[index];
						validPoints.RemoveAt(index);

						//pos = GenerateIndependentGlobalSeed(mapSize, sectionCount, sectionDisplacement, globalRange, globalOriginCount, allSectionsFilled, filledSections);
					}

					foundCorrectLatitude = ValidateLatitude(pos.y, sizeY, ref northSeedCount, ref southSeedCount, northSeedRange, southSeedRange);
				}

				RemoveInRange(pos, ref validPoints, landSpawnDistance.min);

				double gradient = randomDouble(Config.MntGradientMin, Config.MntGradientMax);
				int height = (heightSpawnRange.min + random.Next(heightSpawnRange.max - heightSpawnRange.min));

				HeightSeed<SpreadDirs> seed = GenerateRandomSeed((byte)height, pos.x, pos.y, Config.DefaultSeedStrength, true);
				List<SeedBranch<SpreadDirs>> branch = new List<SeedBranch<SpreadDirs>>();
				branch.Add(new SeedBranch<SpreadDirs>(seed));

				// Create branching seeds
				int seedCount = GenerateBranchLength(maxSeedLength);

				// A recursive function; spawns sub-branches recursively, reducing seedCount until it runs out and branches cease spawning
				GenerateSeedSubBranches(heightRange, height, heightChangeDirection, new vec2i(pos.x, pos.y), seedCount, random.Next(0, 4), ref branch, gradient, decayRate);

				FixBranchLatitude(mapSize, ref branch);

				branchQueue.Add(branch);

				// Add all seeds from the branch to the list of global origins
				foreach (SeedBranch<SpreadDirs> sb in branch)
					origins.AddRange(sb.GetAllCoordinates());
				originCount++;
			}

			Console.WriteLine("Finished Generating Mountain Branches");
		}

		public void GenerateBiomes() {
			CurrentSubmap.InitialiseBiomes();
		}

		// Spawns a seed in the worldmap
		public void PlaceSeed(HeightSeed<SpreadDirs> seed) {
			CurrentMap.Lock();

			CurrentMap[seed.x + seed.y * CurrentMap.SizeX] = seed.height;
			CurrentMap.Unlock();

			int index = seed.x + seed.y * CurrentMap.SizeX;
			
			((BaseMap<SpreadDirs>)FunctionMaps[GenMap.MAP_SPREAD_DIRS])[seed.x + seed.y * CurrentMap.SizeX] = seed.data;
		}

		// Generates a random double between -1.0 and 1.0
		double randomDoubleFullRange() {
			return 2.0 * (random.NextDouble() - 0.5);
		}

		// Generates a random double between min and max
		double randomDouble(double min, double max) {
			return min + (max - min) * random.NextDouble();
		}

		public void GenerateSeedSubBranches(Range heightRange, int height, int heightChangeDirection, vec2i parentPos, int seedCount, double parentAngle, ref List<SeedBranch<SpreadDirs>> parentSeedBranch, double gradient, double decayRate) {
			// Chance to change angle by +/- 0-45 degrees
			// New branch spawns at +/- 90 + (+/- 0-45) degrees
			int rangeMin = 1;
			int rangeMax = 3;
			
			double lastAngle = parentAngle;
			double angle;

			double directionCount = 4 * (rangeMax * rangeMax);
			double angleCount = 0;

			double angleRange = 35;
			int x = parentPos.x;
			int y = parentPos.y;

			double distance;

			int lastX = x;
			int lastY = y;
			int xCarry = 0;
			int yCarry = 0;

			angleRange = (angleRange / 180.0) * Math.PI;
			double seedDecayMin = Math.Pow(0.5, decayRate);
			double seedDecayMax = Math.Pow(0.75, decayRate);

			byte lastSeedStrength = Config.DefaultSeedStrength;

			int nextHeight = height;

			for (int seedID = 0; seedID < seedCount; seedID++) {
				List<SeedBranch<SpreadDirs>> childBranch = new List<SeedBranch<SpreadDirs>>();

				// Each node in this branch has a chance of spawning its own sub-branch
				if (ChanceEqual(0, 3, 0)) {
					int branchSeedCount = random.Next((int)((seedCount - seedID) * seedDecayMin), (int)((seedCount - seedID) * seedDecayMax));
					double childGradient = gradient + randomDouble(Config.MntGradientMin, Config.MntGradientMax);

					int childHeight = (int)(height + heightChangeDirection * childGradient * Config.MntGradientMult);

					GenerateSeedSubBranches(heightRange, height, heightChangeDirection, new vec2i(lastX, lastY), branchSeedCount, 90.0 * randomDoubleFullRange(), ref childBranch, childGradient, decayRate);
				}

				xCarry = 0;
				yCarry = 0;

				distance = random.Next(rangeMin, rangeMax);
				angleCount = 4.0 * (distance * distance);

				//      scale back up          (scale the angle down to 1 int per angle  )   (Create a new angle between
				//                             ((0, 22.5, 45 -> 0, 1, 2) for rounding    )   (-angleRange(-35.0) -> +angleRange(+35.0)
				angle = (angleRange * randomDoubleFullRange());
				
				vec2 displacement = ToDisplacement(distance, lastAngle + angle);

				x = lastX + (int)Math.Round(displacement.x);
				y = lastY + (int)Math.Round(displacement.y);

				FixRange(x, y, CurrentMap.SizeX, CurrentMap.SizeY, ref xCarry, ref yCarry, true);

				if (nextHeight < heightRange.min)
					nextHeight = (byte)heightRange.min;

				if (nextHeight > heightRange.max)
					nextHeight = (byte)heightRange.max;

				HeightSeed<SpreadDirs> seed = GenerateRandomSeed((byte)nextHeight, x + xCarry, y + yCarry, lastSeedStrength, true);
				SeedBranch<SpreadDirs> currentSeed = new SeedBranch<SpreadDirs>(seed, childBranch);
				
				nextHeight += (int)(heightChangeDirection * gradient * Config.MntGradientMult);
				parentSeedBranch.Add(currentSeed);

				lastX = x + xCarry;
				lastY = y + yCarry;
				lastAngle += angle;
				lastSeedStrength = seed.data.Strength;
			}
		}

		// Spawns all the seeds in all the branches
		// This was written because, at first, I was planning on having separate seed branch queues for different sub-maps.
		// This wasn't necessary so far, but I might still do it  if I intend to spawn a branch over multiple spreads/increases in size
		// If I don't do this, I can remove the "branchQueue" parameter as I can simply use the global one
		public void SpawnAllSeedBranches(List<List<SeedBranch<SpreadDirs>>> branchQueue) {
			while (branchQueue.Count > 0) {
				for (int seedID = 0; seedID < branchQueue.Count; seedID++) {
					SeedBranch<SpreadDirs> seed = branchQueue[seedID][0];

					PlaceSeed(seed.Seed);

					if (seed.SubBranch.Count > 0)
						branchQueue.Add(seed.SubBranch);

					branchQueue[seedID].RemoveAt(0);

					if (branchQueue[seedID].Count == 0)
						branchQueue.RemoveAt(seedID);
				}
			}
		}

		// Spawns the first seed for from each branch (including separating sub-branches into their own branches), for a specific number of iterations
		// I.e., doesn't spawn all the seeds, just the first batch
		public void SpawnSeedBranches(int iterations, List<List<SeedBranch<SpreadDirs>>> branchQueue) {
			for (int i = 0; i < iterations; i++) {
				int seedID = 0;

				while (seedID < branchQueue.Count) {
					SeedBranch<SpreadDirs> seed = branchQueue[seedID][0];

					PlaceSeed(seed.Seed);

					if (seed.SubBranch.Count > 0)
						branchQueue.Add(seed.SubBranch);

					branchQueue[seedID].RemoveAt(0);

					if (branchQueue[seedID].Count == 0)
						branchQueue.RemoveAt(seedID);
					else
						seedID++;
				}
			}
		}
		
		bool ChanceEqual(int min, int max, int threshold) {
			return (random.Next(min, max) == threshold);
		}

		bool ChanceGreater(int min, int max, int threshold) {
			return (random.Next(min, max) > threshold);
		}

		public void SpreadMap(int iterations, int landSpreads, int waterSpreads, int inThreadCount) {
			int threadCount = Math.Min(inThreadCount, threads.Length);

			ref BiomeMap currentMap = ref CurrentMap;

			ref List<List<SeedBranch<SpreadDirs>>> branchQueueMnt = ref CurrentSubmap.branchQueueMnt;
			ref List<List<SeedBranch<SpreadDirs>>> branchQueueOcean = ref CurrentSubmap.branchQueueOcean;

			int totalMntSeedCount = 0;
			for (int i = 0; i < branchQueueMnt.Count; i++)
				for (int j = 0; j < branchQueueMnt[i].Count; j++)
					totalMntSeedCount += branchQueueMnt[i][j].GetTotalSize();

			int totalOceanSeedCount = 0;
			for (int i = 0; i < branchQueueOcean.Count; i++)
				for (int j = 0; j < branchQueueOcean[i].Count; j++)
					totalOceanSeedCount += branchQueueOcean[i][j].GetTotalSize();

			int totalLandSpreads = Math.Max((landSpreads * iterations) / 2, 1);
			int seedCountMnt = totalMntSeedCount / totalLandSpreads;

			int totalWaterSpreads = Math.Max((waterSpreads * iterations) / 2, 1);
			int seedCountOcean = totalOceanSeedCount / totalWaterSpreads;

			for (int it = 0; it < iterations; it++) {
				for (int t = 0; t < threadCount; t++) {
					int threadID = t;
					int xStep = currentMap.SizeX / threadCount;
					int xStart = xStep * t;
					int xEnd = (t == threadCount - 1) ? currentMap.SizeX - 1 : xStep * (t + 1) - 1;
					int spreadIterations = landSpreads;

					threadInstructions[t].function = ThreadInstructions.FUNC_LAND_SPREAD_STD;
					threadInstructions[t].seed = random.Next();
					threadInstructions[t].spreadIterations = spreadIterations;
					threadInstructions[t].startY = 0;
					threadInstructions[t].endY = currentMap.SizeY - 1;
					threadInstructions[t].startX = xStart;
					threadInstructions[t].endX = xEnd;

					waitThread[t] = false;
				}

				// Wait for all threads to complete
				for (int t = 0; t < threadCount; t++)
					while (!waitThread[t])
						Thread.Sleep(20);

				for (int ls = 0; ls < landSpreads; ls++) {
					// Spawn some seeds from the queue
					SpawnSeedBranches((it == iterations - 1) ? (totalMntSeedCount - seedCountMnt * it) : seedCountMnt * (it + 1), branchQueueMnt);
					
					// Allow all threads to proceed
					for (int t = 0; t < threadCount; t++)
						waitThread[t] = false;

					// Wait for threads to finish processing
					for (int t = 0; t < threadCount; t++)
						while (!waitThread[t])
							Thread.Sleep(20);

					while (!CurrentMap.CanRead())
						Thread.Sleep(20);

					CurrentMap.Lock();
					CurrentMap = OtherMap.Clone();
					CurrentMap.Unlock();
				}
			}

			for (int it = 0; it < iterations; it++) {
				for (int t = 0; t < threadCount; t++) {
					int threadID = t;
					int xStep = currentMap.SizeX / threadCount;
					int xStart = xStep * t;
					int xEnd = (t == threadCount - 1) ? currentMap.SizeX - 1 : xStep * (t + 1) - 1;
					int spreadIterations = waterSpreads;

					threadInstructions[t].function = ThreadInstructions.FUNC_OCEAN_SPREAD_STD;
					threadInstructions[t].seed = random.Next();
					threadInstructions[t].spreadIterations = spreadIterations;
					threadInstructions[t].startY = 0;
					threadInstructions[t].endY = currentMap.SizeY - 1;
					threadInstructions[t].startX = xStart;
					threadInstructions[t].endX = xEnd;

					waitThread[t] = false;
				}

				// Wait for all threads to complete
				for (int t = 0; t < threadCount; t++)
					while (!waitThread[t])
						Thread.Sleep(20);

				for (int ws = 0; ws < waterSpreads; ws++) {
					// Spawn some seeds from the queue
					SpawnSeedBranches((it == iterations - 1) ? (totalOceanSeedCount - seedCountOcean * it) : seedCountOcean * (it + 1), branchQueueOcean);

					// Allow all threads to proceed
					for (int t = 0; t < threadCount; t++)
						waitThread[t] = false;

					// Wait for threads to finish processing
					for (int t = 0; t < threadCount; t++)
						while (!waitThread[t])
							Thread.Sleep(20);

					while (!CurrentMap.CanRead())
						Thread.Sleep(20);

					CurrentMap.Lock();
					CurrentMap = OtherMap.Clone();
					CurrentMap.Unlock();
				}
			}
		}

		public void SpreadMapLand(int threadID) {
			int xCarry;
			int yCarry;

			int spreadCount = threadInstructions[threadID].spreadIterations;

			int startY = threadInstructions[threadID].startY;
			int startX = threadInstructions[threadID].startX;
			int endY = threadInstructions[threadID].endY + 1;
			int endX = threadInstructions[threadID].endX + 1;
			int randomSeed = threadInstructions[threadID].seed;

			ref BiomeMap currentMap = ref CurrentMap;
			ref BiomeMap otherMap = ref OtherMap;

			BaseMap<float> bedrockHardness = (BaseMap<float>)FunctionMaps[GenMap.MAP_BEDROCK_HARDNESS];
			BaseMap<float> soilHardess = (BaseMap<float>)FunctionMaps[GenMap.MAP_SOIL_HARDNESS];

			BaseMap<SpreadDirs> growthMap = (BaseMap<SpreadDirs>)(FunctionMaps[GenMap.MAP_SPREAD_DIRS]);

			int spreaderHeight = 0;
			int otherHeight = 0;
			int newHeight = 0;
			Random random = new Random(randomSeed);
			int spreadChance;

			int neighbourIndex = 0;
			int outcome;

			float hardnessHeightIncrese = 0.025f;
			float hardnessLongevityIncrease = 0.025f;
			float hardnessIncrease = 0.0f;

			for (int spreadID = 0; spreadID < spreadCount; spreadID++) {
				// Syncronise threads at the end/start of every spread
				while (waitThread[threadID])
					Thread.Sleep(20);

				for (int y = startY; y < endY; y++) {
					for (int x = startX; x < endX; x++) {
						// For each turn that a land tile is a land tile, its soil hardness is slightly strengthened
						// Therefore, it can only increase in strength if the tile wasn't just converted from an ocean tile
						hardnessIncrease = 0.0f;
						bool tileConverted = false;

						// Check surrounding square
						for (int i = -1; i < 2; i++) {
							for (int j = -1; j < 2; j++) {
								if (i == 0 && j == 0) continue;

								xCarry = 0;
								yCarry = 0;

								FixRange(x + j, y + i, currentMap.SizeX, currentMap.SizeY, ref xCarry, ref yCarry, true);
								neighbourIndex = (x + xCarry + j) + (y + yCarry + i) * currentMap.SizeX;

								if (currentMap[neighbourIndex] >= Config.LandCutoff) {
									spreadChance = growthMap[neighbourIndex][1 - j, 1 - i];
									float hardness = 0.5f * ((BaseMap<float>)FunctionMaps[GenMap.MAP_BEDROCK_HARDNESS])[x + y * currentMap.SizeX] + 0.5f * soilHardess[x + y * currentMap.SizeX];

									outcome = random.Next(spreadChance, 164);

									if ((int)(outcome * (1.0f + 0.05f * hardness)) > (int)(154)) {
										spreaderHeight = currentMap[neighbourIndex];
										otherHeight = currentMap[x + y * currentMap.SizeX];

										newHeight = Average(otherHeight, spreaderHeight);

										if (newHeight < Config.LandCutoff)
											newHeight = Config.LandCutoff;
										
										if (newHeight < otherHeight)
											newHeight = Average(otherHeight, newHeight);

										otherMap[x + y * currentMap.SizeX] = (byte)newHeight;
										growthMap[x + y * currentMap.SizeX] = growthMap[neighbourIndex].Clone();
										//soilHardess[x + y * currentMap.SizeX] += hardnessIncrease;
										tileConverted = true;
									}
								}
							}
						}

						if (currentMap[x + y * currentMap.SizeX] >= Config.LandCutoff && !tileConverted) {
							float hardness = soilHardess[x + y * currentMap.SizeX];
							float groundMultiplier = 0.5f + hardness;

							// Opposite for water spreading: 1.5f - hardness
							// Soil hardness CAN be over the max hardness - for example, if some really hard, low ground was suddenly flooded
							// Hardness will then decay until it reaches maxSoilHardess
							// 0.5 - 1.0
							float maxSoilHardness = 0.5f + 0.5f * (currentMap[x + y * currentMap.SizeX] / 255.0f);
							maxSoilHardness *= maxSoilHardness;

							// Each turn, hardness increases for land with soil on it.
							// Hardness increases based on how much soil there is in that spot, and based on how hard the soil is already
							// i.e., the stronger the soil, the slow it decays
							hardnessIncrease = hardness * hardnessLongevityIncrease + maxSoilHardness * hardnessHeightIncrese;

							soilHardess[x + y * currentMap.SizeX] = Math.Min(soilHardess[x + y * currentMap.SizeX] + hardnessIncrease, maxSoilHardness);
						}
					}
				}

				waitThread[threadID] = true;
			}
		}

		public void SpreadMapWater(int threadID) {
			int xCarry;
			int yCarry;

			int spreadCount = threadInstructions[threadID].spreadIterations;

			int startY = threadInstructions[threadID].startY;
			int startX = threadInstructions[threadID].startX;
			int endY = threadInstructions[threadID].endY + 1;
			int endX = threadInstructions[threadID].endX + 1;
			int randomSeed = threadInstructions[threadID].seed;

			ref BiomeMap currentMap = ref CurrentMap;
			ref BiomeMap otherMap = ref OtherMap;

			BaseMap<float> bedrockHardness = (BaseMap<float>)FunctionMaps[GenMap.MAP_BEDROCK_HARDNESS];
			BaseMap<float> soilHardess = (BaseMap<float>)FunctionMaps[GenMap.MAP_SOIL_HARDNESS];

			BaseMap<SpreadDirs> growthMap = (BaseMap<SpreadDirs>)(FunctionMaps[GenMap.MAP_SPREAD_DIRS]);

			int spreaderHeight = 0;
			int otherHeight = 0;
			int newHeight = 0;
			Random random = new Random(randomSeed);
			int spreadChance;

			int neighbourIndex = 0;
			int outcome;

			float hardnessHeightIncrese = 0.0125f;
			float hardnessLongevityIncrease = 0.0125f;
			float hardnessIncrease = 0.0f;

			for (int it = 0; it < spreadCount; it++) {
				while (waitThread[threadID])
					Thread.Sleep(20);

				for (int y = startY; y < endY; y++) {
					for (int x = startX; x < endX; x++) {
						hardnessIncrease = 0.0f;
						bool tileConverted = false;

						// Check surrounding square
						for (int i = -1; i < 2; i++) {
							for (int j = -1; j < 2; j++) {
								if (i == 0 && j == 0) continue;

								xCarry = 0;
								yCarry = 0;

								FixRange(x + j, y + i, currentMap.SizeX, currentMap.SizeY, ref xCarry, ref yCarry, true);
								neighbourIndex = (x + xCarry + j) + (y + yCarry + i) * currentMap.SizeX;
								
								if (currentMap[neighbourIndex] < Config.LandCutoff) { // was < 50
									spreadChance = growthMap[neighbourIndex][i + 1, j + 1];
									float hardness = 0.5f * bedrockHardness[x + y * currentMap.SizeX] + 0.5f * soilHardess[x + y * currentMap.SizeX];

									outcome = random.Next(spreadChance, 164);
									if ((int)(outcome * (1.0f - 0.05f * hardness)) > (int)(154)) {
										spreaderHeight = currentMap[neighbourIndex];
										otherHeight = otherMap[x + y * currentMap.SizeX];

										newHeight = Average(otherHeight, spreaderHeight);

										if (newHeight >= Config.LandCutoff)
											newHeight = Config.LandCutoff - 1;

										if (newHeight > otherHeight)
											newHeight = Average(otherHeight, newHeight);

										otherMap[x + y * currentMap.SizeX] = (byte)newHeight;
										growthMap[x + y * currentMap.SizeX] = growthMap[neighbourIndex].Clone();
										tileConverted = true;
									}
								}
							}
						}

						if (currentMap[x + y * currentMap.SizeX] < Config.LandCutoff && !tileConverted) {
							float hardness = soilHardess[x + y * currentMap.SizeX];
							float groundMultiplier = 0.5f + hardness;

							// Opposite for water spreading: 1.5f - hardness
							// Soil hardness CAN be over the max hardness - for example, if some really hard, low ground was suddenly flooded
							// Hardness will then decay until it reaches maxSoilHardess
							float maxSoilHardness = 0.5f + 0.5f * (currentMap[x + y * currentMap.SizeX] / 255.0f);
							maxSoilHardness *= maxSoilHardness;

							// Each turn, hardness increases for land with soil on it.
							// Hardness increases based on how much soil there is in that spot, and based on how hard the soil is already
							// i.e., the stronger the soil, the slow it decays
							hardnessIncrease = -(hardness * hardnessLongevityIncrease + maxSoilHardness * hardnessHeightIncrese);

							soilHardess[x + y * currentMap.SizeX] = Math.Max(soilHardess[x + y * currentMap.SizeX] + hardnessIncrease, 0.0f);
						}
					}
				}

				waitThread[threadID] = true;
			}
		}

		int Clamp(int value, int min, int max) {
			return Math.Max(Math.Min(value, max), min);
		}

		float Clamp(float value, float min, float max) {
			return Math.Max(Math.Min(value, max), min);
		}

		public void FillMapWater() {
			bool unfilledCellsExist = true;

			ref BiomeMap currentMap = ref CurrentMap;
			ref BiomeMap otherMap = ref OtherMap;
			
			otherMap = currentMap.Clone();

			BaseMap<SpreadDirs> currentMapWaterDir = (BaseMap<SpreadDirs>)(FunctionMaps[GenMap.MAP_SPREAD_DIRS]);
			BaseMap<SpreadDirs> otherMapWaterDir = ((BaseMap<SpreadDirs>)(FunctionMaps[GenMap.MAP_SPREAD_DIRS])).Clone();

			ref List<List<SeedBranch<SpreadDirs>>> branchQueueOcean = ref CurrentSubmap.branchQueueOcean;

			SpreadDirs[] neighbourDirections = new SpreadDirs[8];
			int[] spreadAttempts = new int[8];

			int sizeX = currentMap.SizeX;
			int sizeY = currentMap.SizeY;
			int xCarry, yCarry;
			int maxOutcome = 0;

			while (unfilledCellsExist || branchQueueOcean.Count > 0) {
				unfilledCellsExist = false;

				SpawnSeedBranches(4, branchQueueOcean);

				for (int y = 0; y < sizeY; y++) {
					for (int x = 0; x < sizeX; x++) {
						// Check whether unfilled cells exist in the map
						if (x + y * sizeX < 0)
							Console.WriteLine("[x :: " + x + "][y :: " + y + "][sizeX :: " + sizeX + "]");

						if (currentMap[x + y * sizeX] >= Config.LandCutoff)
							unfilledCellsExist = true;

						int totalSpreadChance = 0;
						int totalSpreadingNeighbours = 0;
						int totalSpreaderHeight = 0;

						// Check surrounding square
						for (int i = -1; i < 2; i++) {
							for (int j = -1; j < 2; j++) {
								if (i == 0 && j == 0) continue;

								xCarry = 0;
								yCarry = 0;

								FixRange(x + j, y + i, sizeX, sizeY, ref xCarry, ref yCarry, true);

								int neighbourIndex = (x + xCarry + j) + (y + yCarry + i) * sizeX;

								if (neighbourIndex < 0)
									Console.WriteLine("[neighbourIndex :: " + neighbourIndex + "] || [x :: " + x + "][y :: " + y + "][sizeX :: " + sizeX + "] || [j :: " + j + "][i :: " + i + "] || [xCarry :: " + xCarry + "][yCarry :: " + yCarry + "]");
								// If the neighbouring square [j, i] is also a water tile, then it can spread to this one
								if (currentMap[neighbourIndex] < Config.LandCutoff) {
									// The direction from the neighbour to this point, as an array position in the GrowthDirs.instensities format [0 - 2][0 - 2]
									// i.e., the flipped x & y positions (-x, -y) + 1
									int relativeX = 1 - j;
									int relativeY = 1 - i;

									neighbourDirections[totalSpreadingNeighbours] = currentMapWaterDir[neighbourIndex];
									spreadAttempts[totalSpreadingNeighbours] = currentMapWaterDir[neighbourIndex][relativeX, relativeY];
									totalSpreadChance += currentMapWaterDir[neighbourIndex][relativeX, relativeY];
									totalSpreaderHeight += currentMap[neighbourIndex];
									totalSpreadingNeighbours++;
								}
							}
						}

						// If there ARE neighbours that can spread to this point...
						if (totalSpreadingNeighbours > 0) {
							// Determines the upper threshold of the spread chance. Changes based on how far from the equator y is

							int highestOutcome = 0;
							int highestOutcomeNeighbour = -1;
							
							for (int s = 0; s < totalSpreadingNeighbours; s++) {
								int outcome = random.Next(spreadAttempts[s], 200);

								if (spreadAttempts[s] > maxOutcome)
									maxOutcome = spreadAttempts[s];

								if (outcome > highestOutcome) {
									highestOutcome = outcome;
									highestOutcomeNeighbour = s;
								}
							}

							// Generate a random number based on our spread chance.
							// This lets us determine whether the neighbour will spread here
							if (highestOutcome > 164) {
								int currentHeight = currentMap[x + y * sizeX];
								int spreaderHeight = totalSpreaderHeight / totalSpreadingNeighbours;

								if (currentHeight < Config.LandCutoff)
									spreaderHeight = Average(spreaderHeight, currentHeight);
								int newHeight = Clamp(spreaderHeight, 0, Config.LandCutoff - 1);

								otherMap[x + y * sizeX] = (byte)newHeight;
								otherMapWaterDir[x + y * sizeX] = neighbourDirections[highestOutcomeNeighbour];
							}
						}
					}
				}

				while (!CurrentMap.CanRead())
					Thread.Sleep(20);

				currentMap.Lock();
				currentMap = otherMap.Clone();
				currentMap.Unlock();
				currentMapWaterDir = otherMapWaterDir.Clone();
			}

			Console.WriteLine("Max Spread Attempts :: " + maxOutcome);
		}

		// Kills the threads within the generator
		public void End() {
			for (int t = 0; t < threads.Length; t++) {
				int threadID = t;
				threadInstructions[t].function = ThreadInstructions.FUNC_KILL_THREAD;
				waitThread[t] = false;
			}

			for (int t = 0; t < threads.Length; t++) {
				threads[t].Join();
			}
		}

		public double Average(double i, double j) {
			return (i + j) / 2.0;
		}

		public int Average(int i, int j) {
			return (i + j) / 2;
		}

		public void FixRange(int x, int y, int rangeX, int rangeY, ref int xCarry, ref int yCarry, bool rotatePoles) {
			xCarry = 0;
			yCarry = 0;

			if (y < 0) {
				yCarry += 2 * (Math.Abs(y));

				if (rotatePoles)
					xCarry += rangeX / 2;
			}

			// If the square goes under the bottom of the map, move 50% to the right and read up
			if (y > rangeY - 1) {
				yCarry -= 2 * (y - (rangeY - 1));

				if (rotatePoles)
					xCarry += rangeX / 2;
			}

			// If the square goes off the left side of the map, start reading from the right
			if (x + xCarry < 0)
				xCarry += rangeX;

			// If the square goes off the right side of the map, start reading from the left
			if (x + xCarry > rangeX - 1)
				xCarry += -rangeX;

			if (x + xCarry > rangeX - 1)
				xCarry -= rangeX;

			if (x + xCarry < 0)
				xCarry += rangeX;
		}

		public void ThreadWorker(int threadID) {
			while (threadInstructions[threadID].function != ThreadInstructions.FUNC_KILL_THREAD) {

				// Wait for further instructions
				// Used to syncronise threads in between iterations
				while (waitThread[threadID])
					Thread.Sleep(20);

				switch (threadInstructions[threadID].function) {
					case ThreadInstructions.FUNC_LAND_SPREAD_STD:
						//Console.WriteLine("[" + threadID + "] :: Spreading Land");
						waitThread[threadID] = true;
						SpreadMapLand(threadID);
						break;
					case ThreadInstructions.FUNC_OCEAN_SPREAD_STD:
						//Console.WriteLine("[" + threadID + "] :: Spreading Water");
						waitThread[threadID] = true;
						SpreadMapWater(threadID);
						//SpreadMapWater(threadID);
						break;
					case ThreadInstructions.FUNC_KILL_THREAD:
						//Console.WriteLine("[" + threadID + "] :: Ending Thread");
						break;
					default:
						//Console.WriteLine("[" + threadID + "] :: Invalid Function");
						break;
				}
			}
		}

		public void Paint(byte height) {
			ref GenMap currentSubmap = ref CurrentSubmap;
			ref BiomeMap otherMap = ref OtherMap;

			int k = 0;

			for (int i = 0; i < currentSubmap.SizeY; i++) {
				for (int j = 0; j < currentSubmap.SizeX; j++) {
					otherMap[j + (i * currentSubmap.SizeX)] = height;
					k++;
				}
			}

			CurrentMap = otherMap.Clone();
		}

		public void AddSubmap() {
			subMapCount++;
			Array.Resize(ref subMaps, subMaps.Length + 1);

			subMaps[subMaps.Length - 1] = new GenMap(sizeY, sizeX);
		}

		public void ResetSubmap(int index) {
			subMaps[index] = new GenMap(sizeY, sizeX);
		}

		public void AddSubmap(ref GenMap map) {
			subMapCount++;
			Array.Resize(ref subMaps, subMaps.Length + 1);

			subMaps[subMaps.Length - 1] = map.Clone();
		}

		public void RemoveSubmap(int i) {
			GenMap[] newSubmaps = new GenMap[subMapCount - 1];

			if (i > 0)
				Array.Copy(subMaps, 0, newSubmaps, 0, i);

			if (i < subMapCount - 1)
				Array.Copy(subMaps, i + 1, newSubmaps, i, subMapCount - (i + 1));

			subMaps = newSubmaps;
		}

		public void RemoveSubmap() {
			GenMap[] newSubmaps = new GenMap[subMapCount - 1];

			Array.Copy(subMaps, 0, newSubmaps, 0, subMapCount - 1);

			subMaps = newSubmaps;
		}

		public void SetMap(int mapIndex, GenMap map) {
			subMaps[mapIndex] = map.Clone();
		}

		public void Enlarge(int scale) {
			foreach (GenMap submap in subMaps) {
				submap.Enlarge(scale);
			}

			foreach (ABasicMap map in FunctionMaps) {
				map.Enlarge(scale);
			}

			sizeX *= scale;
			sizeY *= scale;
		}

		public void MergeLand(int map_0, int map_1, bool clearAfterMerge) {
			subMaps[map_0].MergeLand(ref subMaps[map_1]);

			if (clearAfterMerge)
				ResetSubmap(map_1);
		}

		public void MergeFlat(int map_0, int map_1, bool clearAfterMerge) {
			subMaps[map_0].MergeFlat(ref subMaps[map_1]);

			if (clearAfterMerge)
				ResetSubmap(map_1);
		}

		public void CurveTop() {
			CurrentSubmap.CurveTop();
		}

		public void Curve() {
			CurrentSubmap.Curve();
		}

		public double ChangeRange(int value, int oldRange, int newRange) {
			double value_d = value;
			double oldRange_d = oldRange;
			double newRange_d = newRange;

			value_d /= oldRange_d;
			value_d *= newRange_d;

			return value_d;
		}

		public int ChangeOriginAndRange(int value, int oldRange, int newRange, int newOrigin) {
			double value_d = value;
			double oldRange_d = oldRange;
			double newRange_d = newRange;
			double newOrigin_d = newOrigin;

			double oldOrigin = oldRange_d / 2.0;
			double oldDisplacement = value_d - oldOrigin;
			double oldDisplacementScale = oldDisplacement / oldRange_d;

			double newDisplacement = oldDisplacementScale * newRange_d;
			double newValue = newOrigin_d + newDisplacement;

			return (int)newValue;
		}

		/*
		public void GenerateTectonicPlates_OLD(int sectionsX, int sectionsY, int seedsPerSection, int radius) {
			int seedCount = sectionsX * sectionsY * seedsPerSection;

			int sectionSizeX = sizeX / sectionsX;
			int sectionSizeY = sizeY / sectionsY;

			List<BaseSeed<byte>> seeds = new List<BaseSeed<byte>>();

			for (int i = 0; i < seedCount; i++) {
				int x = random.Next(sectionSizeX);
				int y = random.Next(sectionSizeY);

				seeds.Add(new BaseSeed<byte>(x, y, (byte)i));
			}

			BaseMap<byte>[] tempMaps = new BaseMap<byte>[2];
			tempMaps[0] = new BaseMap<byte>(sizeX, sizeY, byte.MaxValue);

			int currentSeed = 0;

			for (int sectionY = 0; sectionY < sectionsY; sectionY++) {
				for (int sectionX = 0; sectionX < sectionsX; sectionX++) {
					for (int seedIndex = 0; seedIndex < seedsPerSection; seedIndex++) {
						BaseSeed<byte> seed = seeds[currentSeed++];
						tempMaps[0].Data[(seed.x + (sectionX * sectionSizeX)) + (seed.y + sectionY * sectionSizeY) * sizeX] = seed.data;
					}
				}
			}

			tempMaps[1] = tempMaps[0].Clone();

			int[] neighbourParents = new int[seeds.Count];
			int xCarry, yCarry;
			byte plateID;
			int currentMapIndex = 0;
			bool mapFilled = false;

			while (!mapFilled) {
				mapFilled = true;
				tempMaps[currentMapIndex] = tempMaps[Flip(currentMapIndex)].Clone();

				for (int i = 0; i < sizeY; i++) {
					for (int j = 0; j < sizeX; j++) {
						if (tempMaps[currentMapIndex].Data[j + i * sizeX] == byte.MaxValue) {
							mapFilled = false;

							for (int n = 0; n < neighbourParents.Length; n++)
								neighbourParents[n] = 0;

							for (int y = -radius; y <= radius; y++) {
								for (int x = -radius; x <= radius; x++) {
									if (Math.Sqrt(x * x + y * y) > radius)
										continue;

									xCarry = 0;
									yCarry = 0;

									FixRange(j + x, i + y, sizeX, sizeY, ref xCarry, ref yCarry, true);

									plateID = tempMaps[currentMapIndex].Data[(j + xCarry + x) + (i + yCarry + y) * sizeX];

									if (plateID < byte.MaxValue)
										neighbourParents[plateID]++;
								}
							}

							// Find the most common neighbouring plate type
							// There might be more than one top plate (for example, 3 neighbours of type "2" and another 3 of type "5")
							int highestCount = -1;
							List<byte> choices = new List<byte>();

							for (int index = 0; index < neighbourParents.Length; index++) {
								if (neighbourParents[index] > 0) {
									if (neighbourParents[index] > highestCount) {
										choices.Clear();
										choices.Add((byte)index);
										highestCount = neighbourParents[index];
									}
									else if (neighbourParents[index] == highestCount) {
										choices.Add((byte)index);
									}
								}
							}

							// Select the plate type from neighbours
							if (choices.Count() > 0)
								tempMaps[Flip(currentMapIndex)].Data[j + i * sizeX] = choices[random.Next(choices.Count())];
						}
					}
				}
			}

			FunctionMaps[WorldMap.MAP_TECTONIC_PLATES] = tempMaps[currentMapIndex];
		}*/
	}
}
