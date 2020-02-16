using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapMaker {
	public class Seed {
		public int x;
		public int y;
		public byte height;
		public byte[,] spread;
		public byte strength = 1;

		public void SetStrength(byte newStrength) {
			for (int i = 0; i < 3; i++) {
				for (int j = 0; j < 3; j++) {
					spread[i, j] /= strength;
					spread[i, j] *= newStrength;
				}
			}

			strength = newStrength;
		}

		public static Seed Default {
			get {
				return new Seed {
					x = 0,
					y = 0,
					height = 0,
					spread = new byte[,] {
						{ 1, 1, 1 },
						{ 1, 1, 1 },
						{ 1, 1, 1 } },
					strength = 6
				};
			}
		}
	}

	public class Map {
		public int sizeY, sizeX;
		private byte[] tempHeight;

		private byte[] height;
		private byte[,,] waterSpread;
		private byte[,,] landSpread;
		private byte[,] biomeIntensities;
		bool locked;
		public byte[] temperature;

		public byte this[int index] {
			get {
				if (locked)
					return tempHeight[index];

				return height[index];
			}

			set {
				if (!locked)
					height[index] = value;
			}
		}

		public void CopyBiomes(Map other) {
			byte[] biomesToCopy = new byte[] { BiomeRegistry.BIOME_HEAT, BiomeRegistry.BIOME_HUMIDITY, BiomeRegistry.BIOME_ICE_STRONG, BiomeRegistry.BIOME_ICE_WEAK, BiomeRegistry.BIOME_MOUNTAIN };

			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {
					for (int b = 0; b < biomesToCopy.Length; b++) {
						biomeIntensities[x + y * sizeX, b] = other.biomeIntensities[x + y * sizeX, b];
					}
				}
			}
		}

		public byte[] Temperature {
			get { return temperature; }
			set { temperature = value; }
		}

		public byte[,] Biomes {
			get { return biomeIntensities; }
			set { biomeIntensities = value; }
		}

		public byte[] Height {
			get {
				return height;
			}

			set {
				height = value;
			}
		}

		public byte[,,] WaterSpread {
			get {
				return waterSpread;
			}

			set {
				waterSpread = value;
			}
		}

		public byte[,,] LandSpread {
			get {
				return landSpread;
			}

			set {
				landSpread = value;
			}
		}

		public byte[] Height2 {
			get {
				if (locked)
					return tempHeight;

				return height;
			}

			set {
				if (!locked)
					height = value;
			}
		}

		public Map(int height, int width) {
            this.height = new byte[height * width];
			waterSpread = new byte[height * width, 3, 3];
			landSpread = new byte[height * width, 3, 3];

			tempHeight = new byte[height * width];
			temperature = new byte[height * width];

			sizeY = height;
            sizeX = width;
			locked = false;

			biomeIntensities = new byte[height * width, BiomeRegistry.BIOME_COUNT];

			for (int i = 0; i < height; i++)
				this.height[i] = 0;
        }

		public bool CanRead() {
			return !locked;
		}

        public void Clear() {
            height = new byte[sizeY * sizeX];
			waterSpread = new byte[sizeY * sizeX, 3, 3];
			landSpread = new byte[sizeY * sizeX, 3, 3];
		}

		public void SetBiome(int index, byte biomeID, byte biomeIntensity) {
			int currentBiomeIntensity = biomeIntensities[index, biomeID];
			byte intensityChange = (byte)Math.Min(Math.Max(biomeIntensity - currentBiomeIntensity, 0), 255);

			if (biomeID == BiomeRegistry.BIOME_DESERT && biomeIntensity > 0) {
				//biomeIntensities[index, BiomeRegistry.BIOME_RAINFOREST] = 0;
			}

			if (biomeID == BiomeRegistry.BIOME_RAINFOREST && biomeIntensity > 0) {
				//biomeIntensities[index, BiomeRegistry.BIOME_DESERT] = 0;
			}

			/*
			// If this is a heat-type biome, make sure to drop the values of conflicting biomes
			if (false && biomeID >= BiomeRegistry.BIOMETYPE_HEAT_START && biomeID < (BiomeRegistry.BIOMETYPE_HEAT_START + BiomeRegistry.BIOMETYPE_HEAT_COUNT)) {

				// How much extra intensity to give to/take from every other heat biome
				int intensityToDistribute = biomeIntensity - currentBiomeIntensity;
				int distributionPerBiome = intensityToDistribute / (BiomeRegistry.BIOMETYPE_HEAT_COUNT - 1);

				// if dividing by a decimal, there will be some leftover values to distribute for the last value.
				int distrubutionForLastBiome = intensityToDistribute - (distributionPerBiome * (BiomeRegistry.BIOMETYPE_HEAT_COUNT - 1));
				byte newIntensity;

				for (int b = BiomeRegistry.BIOMETYPE_HEAT_START; b < BiomeRegistry.BIOMETYPE_HEAT_START + BiomeRegistry.BIOMETYPE_HEAT_COUNT; b++) {
					if (b == biomeID)
						continue;

					if (b == BiomeRegistry.BIOMETYPE_HEAT_START + BiomeRegistry.BIOMETYPE_HEAT_COUNT - 1)
						newIntensity = (byte)(biomeIntensities[index, b] + distrubutionForLastBiome);
					else
						newIntensity = (byte)(biomeIntensities[index, b] + distributionPerBiome);

					biomeIntensities[index, b] = newIntensity;
				}
			}
			*/

			biomeIntensities[index, biomeID] = biomeIntensity;
		}

		public bool CanBecomeBiome(int index, byte biomeID) {
			return BiomeRegistry.CanBecomeBiome(height[index], biomeID);
		}

		public void InitialiseBiomes(ref double[,] heat, ref double[,] humidity, ref double[,] perlinHeight) {
			BiomeRegistry.GenerateDefaultBiomesFromHeight(ref biomeIntensities, ref height, ref heat, ref humidity, ref perlinHeight, sizeX, sizeY);
		}

		public void InitialiseBiomes() {
			BiomeRegistry.GenerateDefaultBiomesFromHeight(ref biomeIntensities, ref height, sizeX, sizeY);
		}

		public void InitialiseTemperature() {
			double halfSize = sizeY / 2.0;

			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {
					temperature[x + y * sizeX] = (byte)(byte.MaxValue * (Math.Abs(y - halfSize)) / halfSize); 
				}
			}
		}

        public void SetLandSpread(int from, int to) {
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    landSpread[to, i, j] = landSpread[from, i, j];
					waterSpread[to, i, j] = 0;
				}
            }
		}

		public void SetWaterSpread(int from, int to) {
			for (int i = 0; i < 3; i++) {
				for (int j = 0; j < 3; j++) {
					waterSpread[to, i, j] = waterSpread[from, i, j];
					landSpread[to, i, j] = 0;
				}
			}
		}

		public void SetLandSpread(Map map, int from, int to) {
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
					landSpread[to, i, j] = map.landSpread[from, i, j];
					waterSpread[to, i, j] = 0;
				}
			}
		}

		public void SetWaterSpread(Map map, int from, int to) {
			for (int i = 0; i < 3; i++) {
				for (int j = 0; j < 3; j++) {
					waterSpread[to, i, j] = map.waterSpread[from, i, j];
					landSpread[to, i, j] = 0;
				}
			}
		}

		public void SetSpread(Map map, int from, int to) {
			int landStrength = 0;
			int waterStrength = 0;

			for (int i = 0; i < 3; i++) {
				for (int j = 0; j < 3; j++) {
					landStrength += map.landSpread[from, i, j];
					waterStrength += map.waterSpread[from, i, j];
				}
			}


			for (int i = 0; i < 3; i++) {
				for (int j = 0; j < 3; j++) {
					if (landStrength > waterStrength)
						landStrength += map.landSpread[from, i, j];
					else
						waterStrength += map.waterSpread[from, i, j];
				}
			}
		}

		public void Copy(Map map, int from, int to) {
            height[to] = map.height[from];
			SetSpread(map, from, to);
		}

        public void Set(Map map) {
            sizeY = map.sizeY;
            sizeX = map.sizeX;
            height = (byte[])map.height.Clone();
			biomeIntensities = (byte[,])map.biomeIntensities.Clone();
			landSpread = (byte[,,])map.landSpread.Clone();
			waterSpread = (byte[,,])map.waterSpread.Clone();
		}

        public void Curve() {
            byte[] newHeight = (byte[])height.Clone();
            for (int y = 0; y < sizeY; y++) {
                // 0 - 1 over quarter of map
                double poleDist = (Math.Abs(y - (sizeY / 2.0)) - (sizeY / 4.0)) / (sizeY / 4.0);

				if (poleDist > 0.0) {
                    int range = (int)(Math.Pow((sizeX / 60.0) * poleDist, 2.0));
                    
                    for (int x = 0; x < sizeX; x++) {
                        if (height[x + y * sizeX] > 80) {
                            for (int j = -range; j < range; j++) {
                                if (x + j < 0 || x + j > sizeX - 1)
                                    continue;

                                newHeight[(x + j) + y * sizeX] = height[x + y * sizeX];
                            }
                        }
                    }
                }
            }

            height = newHeight;
        }

		public void CurveTop() {
			byte[] newHeight = (byte[])height.Clone();
			for (int y = 0; y < sizeY; y++) {
				// 0 - 1 over quarter of map
				double poleDist = ((sizeY / 4.0) - y) / (sizeY / 4.0);
				//double poleDist = Math.Pow((Math.Abs(y - (sizeY / 2.0))) / (sizeY / 2.0), 2.0);

				if (poleDist > 0.0) {
					int range = (int)(Math.Pow((sizeX / Config.CurveDivider) * poleDist, 2.0));

					for (int x = 0; x < sizeX; x++) {
						if (height[x + y * sizeX] > 80) {
							for (int j = -range; j < range; j++) {
								if (x + j < 0 || x + j > sizeX - 1)
									continue;

								newHeight[(x + j) + y * sizeX] = height[x + y * sizeX];
							}
						}
					}
				}
			}

			height = newHeight;
		}

		public void MergeFlat(Map map) {
            if (sizeX != map.sizeX || sizeY != map.sizeY)
                return;

			for (int y = 0; y < sizeY; y++)
				for (int x = 0; x < sizeX; x++)
					height[x + y * sizeX] = Math.Max(height[x + y * sizeX], map.height[x + y * sizeX]);
        }

        public void MergeAdditive(Map map) {
            if (sizeX != map.sizeX || sizeY != map.sizeY)
                return;

            for (int y = 0; y < sizeY; y++)
                for (int x = 0; x < sizeX; x++)
                    height[x + y * sizeX] = (byte)(height[x + y * sizeX] + map.height[x + y * sizeX]);
        }

        public void MergeSubtractive(Map map) {
            if (sizeX != map.sizeX || sizeY != map.sizeY)
                return;

            for (int y = 0; y < sizeY; y++)
                for (int x = 0; x < sizeX; x++)
                    height[x + y * sizeX] = (byte)(height[x + y * sizeX] - map.height[x + y * sizeX]);
        }

        public void Enlarge(int scale) {
			locked = true;
			tempHeight = (byte[])height.Clone();

			byte[] newHeight = new byte[(sizeX * scale) * (sizeY * scale)];
			byte[,,] newLandSpread = new byte[(sizeX * scale) * (sizeY * scale), 3, 3];
			byte[,,] newWaterSpread = new byte[(sizeX * scale) * (sizeY * scale), 3, 3];
			byte[,] newBiomeIntensities = new byte[(sizeX * scale) * (sizeY * scale), BiomeRegistry.BIOME_COUNT];

			for (int y = 0; y < sizeY; y++) {
                for (int x = 0; x < sizeX; x++) {
                    for (int sx = 0; sx < scale; sx++) {
                        for (int sy = 0; sy < scale; sy++) {
							newHeight[((x * scale) + sx) + (((y * scale) + sy) * (sizeX * scale))] = height[x + y * sizeX];

							for (int i = 0; i < BiomeRegistry.BIOME_COUNT; i++)
								newBiomeIntensities[((x * scale) + sx) + (((y * scale) + sy) * (sizeX * scale)), i] = biomeIntensities[x + y * sizeX, i];

							for (int i = 0; i < 2; i++) {
                                for (int j = 0; j < 2; j++) {
									newLandSpread[((x * scale) + sx) + (((y * scale) + sy) * (sizeX * scale)), i, j] = landSpread[x + y * sizeX, i, j];
									newWaterSpread[((x * scale) + sx) + (((y * scale) + sy) * (sizeX * scale)), i, j] = waterSpread[x + y * sizeX, i, j];
								}
                            }
                        }
                    }
                }
            }

            height = newHeight;
			biomeIntensities = newBiomeIntensities;
			landSpread = newLandSpread;
			waterSpread = newWaterSpread;

			sizeX *= scale;
            sizeY *= scale;

			locked = false;
        }
    }
}
