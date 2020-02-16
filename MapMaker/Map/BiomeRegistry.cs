using System;

namespace MapMaker {
	public class BiomeRegistry {
		public const int BIOME_NULL = 0;
		public const int BIOME_OCEAN_SHALLOW = 1;
		public const int BIOME_OCEAN_DEEP = 2;
		public const int BIOME_GRASS = 3;
		public const int BIOME_MOUNTAIN = 4;
		public const int BIOME_ICE_STRONG = 5;
		public const int BIOME_ICE_WEAK = 6;

		public const int BIOME_DESERT = 7;
		public const int BIOME_RAINFOREST = 8;

		public const int BIOME_HEAT = 9;
		public const int BIOME_HUMIDITY = 10;

		public const int BIOME_COUNT = 11;

		public const int BIOMETYPE_HEAT_START = BIOME_DESERT;
		public const int BIOMETYPE_HEAT_COUNT = 2;

		public static bool CanBecomeBiome(byte height, byte biomeID) {
			if (height < Config.LandCutoff) {
				if (biomeID == BIOME_OCEAN_SHALLOW)
					return true;

				if (biomeID == BIOME_OCEAN_DEEP)
					return true;

			}
			else {
				if (biomeID == BIOME_GRASS)
					return true;

				if (biomeID == BIOME_MOUNTAIN)
					return true;
			}

			if (biomeID == BIOME_ICE_STRONG)
				return true;

			if (biomeID == BIOME_ICE_WEAK)
				return true;

			if (biomeID == BIOME_HEAT)
				return true;

			if (biomeID == BIOME_HUMIDITY)
				return true;

			if (biomeID == BIOME_DESERT)
				return true;

			if (biomeID == BIOME_RAINFOREST)
				return true;

			return false;
		}

		public static void GenerateDefaultBiomesFromHeight(ref byte[,] biomes, ref byte[] height, int sizeX, int sizeY) {
			byte currentHeight;
			double intensity;

			int iceCutoff = (int)((sizeY) * Config.IceCutoff);
			int EquatorCutoff = (int)((sizeY) * Config.EquatorCutoff);

			Console.WriteLine("[biomes.Length] :: " + biomes.Length);
			Console.WriteLine("[sizeX] :: " + sizeX);
			Console.WriteLine("[sizeY] :: " + sizeY);
			Console.WriteLine("[size] :: " + (sizeX * sizeY));
			Console.WriteLine("[biomeSize] :: " + (sizeX * sizeY * BiomeRegistry.BIOME_COUNT));

			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {
					currentHeight = height[x + y * sizeX];

					if (currentHeight < Config.LandCutoff) {
						intensity = currentHeight; // 0 - 140
						intensity -= Config.DeepOceanCutoff; // 0 - 140  => -40 - 100
						intensity /= (Config.LandCutoff - Config.DeepOceanCutoff); // -40 - 100 => -0.4 - 1.0
						intensity = Math.Min(Math.Max(intensity, 0.0), 1.0); // -0.4 - 1.0 => 0.0 - 1.0

						biomes[x + y * sizeX, BIOME_OCEAN_SHALLOW] = (byte)(intensity * byte.MaxValue);

						intensity = 1.0 - intensity; // 0.0 - 1.0 => 1.0 - 0.0
						biomes[x + y * sizeX, BIOME_OCEAN_DEEP] = (byte)(intensity * byte.MaxValue);
					}
					else {
						biomes[x + y * sizeX, BIOME_OCEAN_SHALLOW] = 0;
						biomes[x + y * sizeX, BIOME_OCEAN_DEEP] = 0;
					}

					if (currentHeight >= Config.LandCutoff) {
						biomes[x + y * sizeX, BIOME_GRASS] = byte.MaxValue;
					}
					else {
						biomes[x + y * sizeX, BIOME_GRASS] = 0;
					}

					if (currentHeight >= Config.MountainCutoff) {
						intensity = currentHeight; // 145 - 255
						intensity -= Config.MountainCutoff; // 145 - 255 => 0 - 110
						intensity /= (byte.MaxValue - Config.MountainCutoff); // 0.0 - 1.0

						biomes[x + y * sizeX, BIOME_MOUNTAIN] = (byte)(intensity * byte.MaxValue);
					}
					else {
						biomes[x + y * sizeX, BIOME_MOUNTAIN] = 0;
					}

					if ((sizeY / 2) - Math.Abs(y - (sizeY / 2)) <= iceCutoff) {
						intensity = iceCutoff;
						intensity -= ((sizeY / 2) - Math.Abs(y - (sizeY / 2)));
						intensity /= iceCutoff;
						intensity *= byte.MaxValue;

						biomes[x + y * sizeX, BIOME_ICE_STRONG] = 255;
					}
					else {
						biomes[x + y * sizeX, BIOME_ICE_STRONG] = 0;
					}

					if (Math.Abs(y - (sizeY / 2)) <= EquatorCutoff) {
						intensity = EquatorCutoff;
						intensity -= Math.Abs(y - (sizeY / 2));
						intensity /= EquatorCutoff;
						intensity *= byte.MaxValue;

						biomes[x + y * sizeX, BIOME_HEAT] = 255;
					}
					else {
						biomes[x + y * sizeX, BIOME_HEAT] = 0;
					}
				}
			}
		}

		public static void GenerateDefaultBiomesFromHeight(ref byte[,] biomes, ref byte[] heightmap, ref double[,] heat, ref double[,] humidity, ref double[,] height, int sizeX, int sizeY) {
			byte currentHeight;
			double intensity;
			double mountainDivider = Math.Abs(byte.MaxValue - Config.MountainCutoff);

			int iceCutoff = (int)((sizeY) * Config.IceCutoff);
			int EquatorCutoff = (int)((sizeY) * Config.EquatorCutoff);

			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {
					currentHeight = heightmap[x + y * sizeX];

					double heatValue = heat[x, y];
					double humidityValue = humidity[x, y];

					double[] intensities_heat = GenerateIntensities(heat, x, y, sizeX, sizeY, 3);
					double[] intensities_humidity = GenerateIntensities(humidity, x, y, sizeX, sizeY, 2);

					double iCold;
					double iHot;
					double iTemperate;

					double iIceStrong;
					double iIceWeak;

					double iGrass;
					double iForest;

					double iDesert;
					double iRainforest;

					iCold = Math.Min(Math.Max(intensities_heat[0], 0.0), 1.0);
					iTemperate = Math.Min(Math.Max(intensities_heat[1], 0.0), 1.0);
					iHot = Math.Min(Math.Max(intensities_heat[2], 0.0), 1.0);


					iGrass = Math.Min(Math.Max(intensities_humidity[0] * iTemperate, 0.0), 1.0);
					iForest = Math.Min(Math.Max(intensities_humidity[1] * iTemperate, 0.0), 1.0);

					iDesert = Math.Min(Math.Max(intensities_humidity[0] * iHot, 0.0), 1.0);
					iRainforest = Math.Min(Math.Max(intensities_humidity[1] * iHot, 0.0), 1.0);


					// -------------------- Ocean --------------------
					if (currentHeight < Config.LandCutoff) {
						intensity = currentHeight; // 0 - 140
						intensity -= Config.DeepOceanCutoff; // 0 - 140  => -40 - 100
						intensity /= (Config.LandCutoff - Config.DeepOceanCutoff); // -40 - 100 => -0.4 - 1.0
						intensity = Math.Min(Math.Max(intensity, 0.0), 1.0); // -0.4 - 1.0 => 0.0 - 1.0

						iCold *= 0.9;

						biomes[x + y * sizeX, BIOME_OCEAN_SHALLOW] = (byte)((intensity * (1.0 - iCold)) * byte.MaxValue);

						intensity = 1.0 - intensity; // 0.0 - 1.0 => 1.0 - 0.0
						biomes[x + y * sizeX, BIOME_OCEAN_DEEP] = (byte)((intensity * (1.0 - iCold)) * byte.MaxValue);
					}
					else {
						biomes[x + y * sizeX, BIOME_OCEAN_SHALLOW] = 0;
						biomes[x + y * sizeX, BIOME_OCEAN_DEEP] = 0;
					}

					// -------------------- Ice --------------------
					iIceStrong = Math.Min(Math.Max(intensities_humidity[0] * iCold, 0.0), 1.0);
					iIceWeak = Math.Min(Math.Max(intensities_humidity[1] * iCold, 0.0), 1.0);

					// -------------------- Basic Land Biomes --------------------
					if (currentHeight >= Config.LandCutoff) {
						biomes[x + y * sizeX, BIOME_GRASS] = (byte)(iTemperate * byte.MaxValue);

						biomes[x + y * sizeX, BIOME_RAINFOREST] = (byte)(iRainforest * byte.MaxValue);
						biomes[x + y * sizeX, BIOME_DESERT] = (byte)(iDesert * byte.MaxValue);

						iIceStrong += 2.0 * iIceWeak / 3.0;
						iIceWeak /= 3.0;

						// -------------------- Mountain --------------------
						if (true) {
							double terrainMountainHeight = (currentHeight - Config.MountainCutoff);
							terrainMountainHeight /= mountainDivider;
							double newHeight = (height[x, y]);

							if (terrainMountainHeight > 0.0)
								newHeight = Math.Max(newHeight, terrainMountainHeight);

							biomes[x + y * sizeX, BIOME_MOUNTAIN] = (byte)((height[x, y]) * byte.MaxValue);
							//heightmap[x + y * sizeX] = (byte)(newHeight * byte.MaxValue);
						}
					}

					// -------------------- Ice, Continued --------------------
					biomes[x + y * sizeX, BIOME_ICE_STRONG] = (byte)(iIceStrong * byte.MaxValue);
					biomes[x + y * sizeX, BIOME_ICE_WEAK] = (byte)(iIceWeak * byte.MaxValue);

					// -------------------- Heat and Humidity Maps --------------------
					biomes[x + y * sizeX, BIOME_HEAT] = (byte)(heatValue * byte.MaxValue);
					biomes[x + y * sizeX, BIOME_HUMIDITY] = (byte)(humidityValue * byte.MaxValue);
				}
			}
		}

		public static double[] GenerateIntensities(double[,] perlin, int x, int y, int sizeX, int sizeY, int sectionCount) {
			double perlinStrength = perlin[x, y];
			double[] intensities = new double[sectionCount];

			double sectionSize = 1.0 / sectionCount;
			double intensity = 0;
			double halfSectionSize = sectionSize / 2.0;

			double total = 0.0;

			for (int s = 0; s < sectionCount; s++) {
				double centre = (s * sectionSize) + halfSectionSize;
				double lower = ((s - 1) * sectionSize) + halfSectionSize;
				double upper = ((s + 1) * sectionSize) + halfSectionSize;

				double intensityLower = 0.0;
				double intensityUpper = 0.0;

				if (perlinStrength > lower && perlinStrength <= centre) {
					if (s == 0) {
						intensityLower = 1.0;
					}
					else {
						intensityLower = Math.Min(Math.Max(perlinStrength - lower, 0.0), 1.0);
						intensityLower *= (sectionCount);
					}
				}

				if (perlinStrength >= centre && perlinStrength < upper) {
					if (s == sectionCount - 1) {
						intensityUpper = 1.0;
					}
					else {
						intensityUpper = sectionSize - Math.Min(Math.Max(perlinStrength - centre, 0.0), 1.0);
						intensityUpper *= (sectionCount);
					}
				}

				intensities[s] += intensityLower;
				intensities[s] += intensityUpper;

				//total += perlinStrength;
			}

			return intensities;
		}

		public static double[] GenerateIntensities(double perlinStrength, int sectionCount) {
			double[] intensities = new double[sectionCount];

			double sectionSize = 1.0 / sectionCount;
			double halfSectionSize = sectionSize / 2.0;

			for (int s = 0; s < sectionCount; s++) {
				double centre = (s * sectionSize) + halfSectionSize;
				double lower = ((s - 1) * sectionSize) + halfSectionSize;
				double upper = ((s + 1) * sectionSize) + halfSectionSize;

				double intensityLower = 0.0;
				double intensityUpper = 0.0;

				if (perlinStrength > lower && perlinStrength <= centre) {
					if (s == 0) {
						intensityLower = 1.0;
					}
					else {
						intensityLower = Math.Min(Math.Max(perlinStrength - lower, 0.0), 1.0);
						intensityLower *= (sectionCount);
					}
				}

				if (perlinStrength >= centre && perlinStrength < upper) {
					if (s == sectionCount - 1) {
						intensityUpper = 1.0;
					}
					else {
						intensityUpper = sectionSize - Math.Min(Math.Max(perlinStrength - centre, 0.0), 1.0);
						intensityUpper *= (sectionCount);
					}
				}

				intensities[s] += intensityLower;
				intensities[s] += intensityUpper;
			}

			return intensities;
		}

		public BiomeRegistry(int biomeCount) {

		}
	}
}
