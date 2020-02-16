using System;

namespace MapMaker {
	public class BiomeMap : BaseMap<byte> {
		protected byte[,] biomeIntensities;

		public BiomeMap(int inSizeX, int inSizeY) : base(inSizeX, inSizeY) {
			ClearBiomes();
		}

		public BiomeMap(int inSizeX, int inSizeY, byte startingValue) : base(inSizeX, inSizeY, startingValue) {
			ClearBiomes();
		}

		public BiomeMap(int inSizeX, int inSizeY, byte[] inMetaData) : base(inSizeX, inSizeY, inMetaData) {
			ClearBiomes();
		}

		public void ClearBiomes() {
			biomeIntensities = new byte[sizeX * sizeY, BiomeRegistry.BIOME_COUNT];
		}

		public byte[,] Biomes {
			get { return biomeIntensities; }
			set { biomeIntensities = value; }
		}

		public void CopyBiomes(BiomeMap other) {
			byte[] biomesToCopy = new byte[] { BiomeRegistry.BIOME_HEAT, BiomeRegistry.BIOME_HUMIDITY, BiomeRegistry.BIOME_ICE_STRONG, BiomeRegistry.BIOME_ICE_WEAK, BiomeRegistry.BIOME_MOUNTAIN };

			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {
					for (int b = 0; b < biomesToCopy.Length; b++) {
						biomeIntensities[x + y * sizeX, b] = other.biomeIntensities[x + y * sizeX, b];
					}
				}
			}
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

			biomeIntensities[index, biomeID] = biomeIntensity;
		}

		public bool CanBecomeBiome(int index, byte biomeID) {
			return BiomeRegistry.CanBecomeBiome(data[index], biomeID);
		}

		public void InitialiseBiomes(ref double[,] heat, ref double[,] humidity, ref double[,] perlinHeight) {
			BiomeRegistry.GenerateDefaultBiomesFromHeight(ref biomeIntensities, ref data, ref heat, ref humidity, ref perlinHeight, sizeX, sizeY);
		}

		public void InitialiseBiomes() {
			BiomeRegistry.GenerateDefaultBiomesFromHeight(ref biomeIntensities, ref data, sizeX, sizeY);
		}

		new public void Enlarge(int scale) {
			locked_1 = true;
			tempData = (byte[])data.Clone();

			byte[] newData = new byte[(sizeX * scale) * (sizeY * scale)];
			//byte[,,] newLandSpread = new byte[(sizeX * scale) * (sizeY * scale), 3, 3];
			//byte[,,] newWaterSpread = new byte[(sizeX * scale) * (sizeY * scale), 3, 3];
			byte[,] newBiomeIntensities = new byte[(sizeX * scale) * (sizeY * scale), BiomeRegistry.BIOME_COUNT];

			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {
					for (int sx = 0; sx < scale; sx++) {
						for (int sy = 0; sy < scale; sy++) {
							newData[((x * scale) + sx) + (((y * scale) + sy) * (sizeX * scale))] = data[x + y * sizeX];

							for (int b = 0; b < BiomeRegistry.BIOME_COUNT; b++)
								newBiomeIntensities[((x * scale) + sx) + (((y * scale) + sy) * (sizeX * scale)), b] = biomeIntensities[x + y * sizeX, b];

							/*for (int i = 0; i < 2; i++) {
								for (int j = 0; j < 2; j++) {
									newLandSpread[((x * scale) + sx) + (((y * scale) + sy) * (sizeX * scale)), i, j] = landSpread[x + y * sizeX, i, j];
									newWaterSpread[((x * scale) + sx) + (((y * scale) + sy) * (sizeX * scale)), i, j] = waterSpread[x + y * sizeX, i, j];
								}
							}*/
						}
					}
				}
			}

			data = newData;
			biomeIntensities = newBiomeIntensities;
			//landSpread = newLandSpread;
			//waterSpread = newWaterSpread;

			sizeX *= scale;
			sizeY *= scale;

			locked_1 = false;
		}

		new public void Clear() {
			base.Clear();
			ClearBiomes();
		}

		new public BiomeMap Clone() {
			return new BiomeMap(sizeX, sizeY, data);
		}
	}
}
