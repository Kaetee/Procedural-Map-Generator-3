using System;
using System.Collections.Generic;

namespace MapMaker {
	public class GenMap {
		public const int MAP_BEDROCK_HARDNESS = 0;
		public const int MAP_SOIL_HARDNESS = 1;
		public const int MAP_SPREAD_DIRS = 2;
		public const int MAP_NOISE_0 = 3;

		public const int MAP_TECTONIC_PLATES = 4;

		public const int MAP_COUNT = 4;

		public int currentMapID;

		public List<ABasicMap> function_maps;
		public List<List<SeedBranch<SpreadDirs>>> branchQueueMnt;
		public List<List<SeedBranch<SpreadDirs>>> branchQueueOcean;

		// Writing to the same map we're reading can cause two major problems:
		//    1: When "spreading" land/water, the first spreads affect consecutive coordinates.
		//       Therefore, the map naturally becomes thicker in the bottom-right corner
		//    2: Multithreading becomes a huge problem; One thread can be reading array values
		//       that another thread is currently writing to.#
		// Therefore, we use two maps. We use one to determine what to write to the other, then switch
		public BiomeMap[] maps = new BiomeMap[2];

		private GenMap() {
			currentMapID = 0;

			function_maps = new List<ABasicMap>();
			for (int i = 0; i < MAP_COUNT; i++)
				function_maps.Add(new BaseMap<byte>());
		}

		public GenMap(int inSizeX, int inSizeY) : this() {
			function_maps[MAP_BEDROCK_HARDNESS] = new BaseMap<float>(inSizeX, inSizeY);
			function_maps[MAP_SOIL_HARDNESS] = new BaseMap<float>(inSizeX, inSizeY);
			function_maps[MAP_SPREAD_DIRS] = new BaseMap<SpreadDirs>(inSizeX, inSizeY, new SpreadDirs());
			function_maps[MAP_NOISE_0] = new BaseMap<float>(inSizeX, inSizeY);

			//function_maps[MAP_TECTONIC_PLATES] = new BaseMap<byte>(inSizeX, inSizeY);

			maps[0] = new BiomeMap(inSizeX, inSizeY, 0);
			maps[1] = new BiomeMap(inSizeX, inSizeY, 0);

			for (int i = 0; i < inSizeX * inSizeY; i++) {
				maps[0][i] = 255;
				maps[1][i] = 255;
			}

			branchQueueMnt = new List<List<SeedBranch<SpreadDirs>>>();
			branchQueueOcean = new List<List<SeedBranch<SpreadDirs>>>();
		}

		public GenMap(GenMap other) : this() {
			function_maps[MAP_BEDROCK_HARDNESS] = ((BaseMap<float>)other.function_maps[MAP_BEDROCK_HARDNESS]).Clone();
			function_maps[MAP_SOIL_HARDNESS] = ((BaseMap<float>)other.function_maps[MAP_SOIL_HARDNESS]).Clone();
			//function_maps[MAP_TECTONIC_PLATES] = ((BaseMap<byte>)other.function_maps[MAP_TECTONIC_PLATES]).Clone();
			function_maps[MAP_SPREAD_DIRS] = ((BaseMap<SpreadDirs>)other.function_maps[MAP_SPREAD_DIRS]).Clone();
			function_maps[MAP_NOISE_0] = ((BaseMap<float>)other.function_maps[MAP_NOISE_0]).Clone();

			maps[0] = other.maps[0].Clone();
			maps[1] = other.maps[1].Clone();

			branchQueueMnt = new List<List<SeedBranch<SpreadDirs>>>();
			branchQueueOcean = new List<List<SeedBranch<SpreadDirs>>>();
		}

		public ref BiomeMap CurrentMap {
			get => ref maps[currentMapID];
		}

		public ref BiomeMap OtherMap {
			get => ref maps[Flip(currentMapID)];
		}

		public int SizeX {
			get => maps[currentMapID].SizeX;
		}

		public int SizeY {
			get => maps[currentMapID].SizeY;
		}

		public GenMap Clone() {
			return new GenMap(this);
		}

		public int Flip(int i) {
			return (i == 0) ? 1 : 0;
		}

		public void Enlarge(int scale) {
			maps[0].Enlarge(scale);
			maps[1].Enlarge(scale);

			function_maps[MAP_BEDROCK_HARDNESS].Enlarge(scale);
			function_maps[MAP_SOIL_HARDNESS].Enlarge(scale);
			//function_maps[MAP_TECTONIC_PLATES].Enlarge(scale);
			function_maps[MAP_SPREAD_DIRS].Enlarge(scale);
			function_maps[MAP_NOISE_0].Enlarge(scale);
		}


		public void InitialiseBiomes() {
			CurrentMap.InitialiseBiomes();
			OtherMap.InitialiseBiomes();
		}

		public void MergeFlat(ref GenMap other) {
			int mapLength = maps[0].Data.Length;

			GenMap[] alts = new GenMap[] { this, other };

			for (int i = 0; i < mapLength; i++) {
				int j = (maps[0][i] > other.maps[0][i]) ? 0 : 1;

				maps[0][i] = alts[j].maps[0][i];
				maps[1][i] = alts[j].maps[1][i];
				
				((BaseMap<float>)function_maps[MAP_BEDROCK_HARDNESS])[i] = ((BaseMap<float>)alts[j].function_maps[MAP_BEDROCK_HARDNESS])[i];
				((BaseMap<float>)function_maps[MAP_SOIL_HARDNESS])[i] = ((BaseMap<float>)alts[j].function_maps[MAP_SOIL_HARDNESS])[i];
				//((BaseMap<byte>)function_maps[MAP_TECTONIC_PLATES])[i] = ((BaseMap<byte>)alts[j].function_maps[MAP_TECTONIC_PLATES])[i];
				((BaseMap<SpreadDirs>)function_maps[MAP_SPREAD_DIRS])[i] = ((BaseMap<SpreadDirs>)alts[j].function_maps[MAP_SPREAD_DIRS])[i];
				((BaseMap<float>)function_maps[MAP_NOISE_0])[i] = ((BaseMap<float>)alts[j].function_maps[MAP_NOISE_0])[i];
			}
		}

		public void MergeLand(ref GenMap other) {
			int mapLength = maps[0].Data.Length;

			GenMap[] alts = new GenMap[] { this, other };

			for (int i = 0; i < mapLength; i++) {
				if (maps[0][i] < Config.LandCutoff && other.maps[0][i] >= Config.LandCutoff) {
					maps[0][i] = other.maps[0][i];
					maps[1][i] = other.maps[1][i];

					((BaseMap<float>)function_maps[MAP_BEDROCK_HARDNESS])[i] = ((BaseMap<float>)other.function_maps[MAP_BEDROCK_HARDNESS])[i];
					((BaseMap<float>)function_maps[MAP_SOIL_HARDNESS])[i] = ((BaseMap<float>)other.function_maps[MAP_SOIL_HARDNESS])[i];
					//((BaseMap<byte>)function_maps[MAP_TECTONIC_PLATES])[i] = ((BaseMap<byte>)other.function_maps[MAP_TECTONIC_PLATES])[i];
					((BaseMap<SpreadDirs>)function_maps[MAP_SPREAD_DIRS])[i] = ((BaseMap<SpreadDirs>)other.function_maps[MAP_SPREAD_DIRS])[i];
					((BaseMap<float>)function_maps[MAP_NOISE_0])[i] = ((BaseMap<float>)other.function_maps[MAP_NOISE_0])[i];
				}
			}
		}

		public void CurveTop() {
			ref BiomeMap currentMap = ref CurrentMap;
			ref BiomeMap otherMap = ref OtherMap;
			otherMap = currentMap.Clone();

			int spreadCount = 0;

			BaseMap<SpreadDirs> landDir = ((BaseMap<SpreadDirs>)function_maps[MAP_SPREAD_DIRS]);
			BaseMap<SpreadDirs> otherLandDir = ((BaseMap<SpreadDirs>)function_maps[MAP_SPREAD_DIRS]).Clone();

			for (int y = 0; y < SizeY; y++) {
				// 0 - 1 over quarter of map
				double poleDist = ((SizeY * Config.WorldCurveRange) - y) / (SizeY * Config.WorldCurveRange);

				if (poleDist > 0.0) {
					int range = (int)(Math.Pow((SizeX / Config.CurveDivider) * poleDist, 3.0));

					for (int r = 0; r < range; r++) {
						for (int x = 0; x < SizeX; x++) {
							byte height = currentMap[x + y * SizeX];

							byte maxHeight = height;
							SpreadDirs dirs = landDir[x + y * SizeX];

							for (int i = -1; i < 2; i++) {
								if (i == 0) continue;

								int xCarry = 0;
								int yCarry = 0;

								FixRange(x + i, y, SizeX, SizeY, ref xCarry, ref yCarry, false);

								byte newHeight = currentMap[(x + xCarry + i) + y * SizeX];

								if (newHeight >= Config.LandCutoff && newHeight > maxHeight) {
									maxHeight = newHeight;
									dirs = landDir[(x + xCarry + i) + y * SizeX];
								}
							}

							if (maxHeight > height) {
								otherMap[x + y * SizeX] = maxHeight;
								otherLandDir[x + y * SizeX] = dirs;
								spreadCount++;
							}
						}

						currentMap = otherMap.Clone();
						landDir = otherLandDir.Clone();
					}
				}
			}

			Console.WriteLine("Curve Spreads :: " + spreadCount);
		}

		public void Curve() {
			ref BiomeMap currentMap = ref CurrentMap;
			ref BiomeMap otherMap = ref OtherMap;
			otherMap = currentMap.Clone();

			int spreadCount = 0;

			BaseMap<SpreadDirs> landDir = ((BaseMap<SpreadDirs>)function_maps[MAP_SPREAD_DIRS]);
			BaseMap<SpreadDirs> otherLandDir = ((BaseMap<SpreadDirs>)function_maps[MAP_SPREAD_DIRS]).Clone();

			for (int y = 0; y < SizeY; y++) {
				// 0 - 1 over quarter of map
				int correctedY = (y < SizeY / 2) ? y : SizeY - y;
				double poleDist = ((SizeY * Config.WorldCurveRange) - correctedY) / (SizeY * Config.WorldCurveRange);

				if (poleDist > 0.0) {
					int range = (int)(Math.Pow((SizeX / Config.CurveDivider) * poleDist, 2.0));

					for (int r = 0; r < range; r++) {
						for (int x = 0; x < SizeX; x++) {
							byte height = currentMap[x + y * SizeX];

							byte maxHeight = height;
							SpreadDirs dirs = landDir[x + y * SizeX];

							for (int i = -1; i < 2; i++) {
								if (i == 0) continue;

								int xCarry = 0;
								int yCarry = 0;

								FixRange(x + i, y, SizeX, SizeY, ref xCarry, ref yCarry, false);

								byte newHeight = currentMap[(x + xCarry + i) + y * SizeX];

								if (newHeight >= Config.LandCutoff && newHeight > maxHeight) {
									maxHeight = newHeight;
									dirs = landDir[(x + xCarry + i) + y * SizeX];
								}
							}

							if (maxHeight > height) {
								otherMap[x + y * SizeX] = maxHeight;
								otherLandDir[x + y * SizeX] = dirs;
								spreadCount++;
							}
						}

						currentMap = otherMap.Clone();
						landDir = otherLandDir.Clone();
					}
				}
			}

			Console.WriteLine("Curve Spreads :: " + spreadCount);
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
	}
}
