using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MapMaker {
	class ImageCreator {
		static byte[] depths = new byte[] { 84, 27, 21, 255 };
		static byte[] water = new byte[] { 232, 82, 27, 255 };
		static byte[] waterCoast = new byte[] { 221, 232, 27, 255 };
		static byte[] beach = new byte[] { 78, 159, 165, 255 };
		static byte[] grass = new byte[] { 18, 232, 57, 255 };
		static byte[] forest = new byte[] { 32, 173, 58, 255 };
		static byte[] peak = new byte[] { 233, 233, 233, 255 };

		static byte[] rainforest = new byte[] { 12, 94, 21, 255 };
		//byte[] rainforest = new byte[] { 18, 232, 57, 255 };
		static byte[] desert = new byte[] { 85, 215, 224, 255 };

		static byte[] rock_bright = new byte[] { 70, 80, 75, 255 };
		static byte[] rock_mid = new byte[] { 50, 55, 60, 255 };
		static byte[] rock_dark = new byte[] { 45, 45, 45, 255 };

		static byte[] dust = new byte[] { 25, 25, 25, 255 };

		static byte[] snow = new byte[] { 255, 255, 255, 255 };

		static byte[] iceWeak = new byte[] { 255, 255, 200, 255 };
		static byte[] iceStrong = new byte[] { 255, 255, 255, 255 };

		static byte levelDepths = 0;
		static byte levelWater = (byte)(Config.LandCutoff - 1);
		//byte levelBeach = 80;
		static byte levelGrass = Config.LandCutoff;
		static byte levelPeak = 255;

		public static Bitmap CreateImageFromMap(BiomeMap map, bool useBiomes = false) {
			int sizeX = map.SizeX;
			int sizeY = map.SizeY;

			byte[] imageData = new byte[sizeY * sizeX * 4];

			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {
					int mapHeight = map[(x + (y * sizeX))];

					if (useBiomes) {
						double iGrass = map.Biomes[(x + (y * sizeX)), BiomeRegistry.BIOME_GRASS];

						double iDesert = map.Biomes[(x + (y * sizeX)), BiomeRegistry.BIOME_DESERT];
						double iRainforest = map.Biomes[(x + (y * sizeX)), BiomeRegistry.BIOME_RAINFOREST];

						double iIceStrong = map.Biomes[(x + (y * sizeX)), BiomeRegistry.BIOME_ICE_STRONG];
						double iIceWeak = map.Biomes[(x + (y * sizeX)), BiomeRegistry.BIOME_ICE_WEAK];

						double iOceanDeep = map.Biomes[(x + (y * sizeX)), BiomeRegistry.BIOME_OCEAN_DEEP];
						double iOceanShallow = map.Biomes[(x + (y * sizeX)), BiomeRegistry.BIOME_OCEAN_SHALLOW];

						double iMountain = map.Biomes[(x + (y * sizeX)), BiomeRegistry.BIOME_MOUNTAIN];

						iGrass /= byte.MaxValue;
						iMountain /= byte.MaxValue;

						iDesert /= byte.MaxValue;
						iRainforest /= byte.MaxValue;

						iIceStrong /= byte.MaxValue;
						iIceWeak /= byte.MaxValue;

						iOceanDeep /= byte.MaxValue;
						iOceanShallow /= byte.MaxValue;

						for (int i = 0; i < 4; i++)
							imageData[(x + (y * sizeX)) * 4 + i] = (byte)((iIceStrong * iceStrong[i]) + (iIceWeak * iceWeak[i]));

						if (iGrass > 0.0 || iDesert > 0.0 || iRainforest > 0.0 || iMountain > 0.0) {
							double mountainBrightness = GetLightIntensity(map, x, y, 1.0);

							// Bright mountain colour will peak within 1/6th of the mountain height
							double iMountainBright = Math.Min(Math.Max((iMountain * 6.0), 0.0), 1.0);

							// Regular mountain colour will peak within 1/2 (3/6) of the mountain height, but is displaced by 0.5/2 
							double iMountainMid = Math.Min(Math.Max(((iMountain * 6.0) - 1.0), 0.0), 1.0);

							double iMountainDark = Math.Min(Math.Max(((((iMountain * 6.0) - 2.0) / 6.0) * 3.0), 0.0), 1.0);
							double iCold = Math.Max(iIceStrong, iIceWeak);
							double iHot = Math.Max(iDesert, iRainforest);

							// Snow mountain colour will peak within 1/3 of the mountain height, but is displaced by 2/3rds.
							// Therefore, it'll peak at the same time as iMountain, but won't start rising until iMountain = 2/3
							//double iMountainSnow = Math.Min(Math.Max((iMountain * 6.0) - 4.0, 0.0), 1.0);
							double iMountainSnow = Math.Min(Math.Max(((iMountain * 6.0) - (4.0 - (2.0 * iCold))) / (1.0 + (2.0 * iCold)), 0.0), 1.0);

							double iMountainDust = Math.Min(Math.Max((iMountain * 6.0) - 4.0, 0.0), 1.0);

							//double iMountainSnow_Default = Math.Min(Math.Max(((iMountain * 6.0) - 4.0) / 2.0, 0.0), 1.0);
							double iMountainSnow_Cold = Math.Min(Math.Max(((iMountain * 6.0) - 3.0) / 3.0, 0.0), 1.0);

							double overallMountain = 0.9;

							iMountainBright -= iMountainMid;
							iMountainMid -= iMountainDark;
							iMountainDark -= iMountainSnow;

							double mountainLeftover = 1.0 - (overallMountain * (iMountainBright + iMountainMid + iMountainDark + iMountainSnow));

							iMountainSnow *= (1.0 - iHot);
							iMountainDust *= iHot;

							for (int i = 0; i < 4; i++)
								imageData[(x + (y * sizeX)) * 4 + i] = (byte)((overallMountain * ((iMountainBright * rock_bright[i]) + (iMountainMid * rock_mid[i]) + (iMountainDark * rock_dark[i]) + (iMountainSnow * snow[i]) + (iMountainDust * dust[i]))) + (mountainLeftover * ((iGrass * forest[i]) + (iDesert * desert[i]) + (iRainforest * rainforest[i]) + (iIceStrong * iceStrong[i]) + (iIceWeak * iceWeak[i]))));

							//imageData[(x + (y * sizeX)) * 4 + 3] = 255;
						}
						else if (iOceanDeep > 0.0 || iOceanShallow > 0.0) {
							double iceWeakIntensity = 0.0;
							double iceStrongIntensity = 0.0;

							iceWeakIntensity *= 0.95;
							iceStrongIntensity *= 0.95;

							double iceIntensity = iceStrongIntensity + iceWeakIntensity;

							double leftoverIntensity = 1.0 - Math.Min(Math.Max(iceIntensity, 0.0), 1.0);
							iceIntensity = Math.Max(iceIntensity, 1.0);

							double intensity_ice_weak = (iceWeakIntensity / iceIntensity);
							double intensity_ice_strong = (iceStrongIntensity / iceIntensity);

							for (int i = 0; i < 4; i++)
								imageData[(x + (y * sizeX)) * 4 + i] += (byte)((iOceanShallow * waterCoast[i]) + (iOceanDeep * depths[i]));

							//imageData[(x + (y * sizeX)) * 4 + 3] = 255;
							//imageData[(x + (y * sizeX)) * 4 + i] = (byte)((intensity_shallow * water[i]) + (intensity_deep * depths[i]) + (intensity_ice_weak * iceWeak[i]) + (intensity_ice_strong * iceStrong[i]));
						}
						else if (iIceStrong == 0.0 && iIceWeak == 0.0) {

						}
					}
					else {
						if (mapHeight < levelGrass) {
							for (int i = 0; i < 4; i++)
								imageData[(x + (y * sizeX)) * 4 + i] = mix((byte)mapHeight, levelDepths, levelWater, depths[i], water[i]);
						}
						else {
							for (int i = 0; i < 4; i++)
								imageData[(x + (y * sizeX)) * 4 + i] = mix((byte)mapHeight, levelGrass, levelPeak, grass[i], peak[i]);
						}
					}
				}
			}

			Bitmap mapImage = new Bitmap(sizeX, sizeY, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			var bitmapData = mapImage.LockBits(new System.Drawing.Rectangle(0, 0, mapImage.Width, mapImage.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, mapImage.PixelFormat);
			Marshal.Copy(imageData, 0, bitmapData.Scan0, imageData.Length);
			mapImage.UnlockBits(bitmapData);

			return mapImage;
		}

		public static Bitmap CreateImageFromMap(float[] map, int sizeX, int sizeY, float multiplier) {
			byte[] imageData = new byte[sizeY * sizeX * 4];

			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {
					double mapHeight = map[x + y * sizeX];

					int sections = 3;
					double sectionSize = 1.0 / sections;
					double intensity = 0;

					for (int s = 0; s < sections; s++) {
						double currentSection = s * sectionSize;

						double flipper = 1.0;
						if (currentSection < 0.5)
							flipper = -1.0;

						currentSection -= 0.5;
						currentSection = Math.Abs(currentSection);
						currentSection *= 2.0;

						currentSection = Math.Pow(currentSection, 2.5);

						currentSection /= 2.0;
						currentSection *= flipper;
						currentSection += 0.5;

						if (mapHeight <= (s * sectionSize))
							intensity += sectionSize;
					}

					for (int i = 0; i < 4; i++) {
						imageData[(x + (y * sizeX)) * 4 + i] = (byte)(mapHeight * multiplier);
					}

					imageData[(x + (y * sizeX)) * 4 + 3] = 255;
				}
			}

			Bitmap mapImage = new Bitmap(sizeX, sizeY, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			var bitmapData = mapImage.LockBits(new System.Drawing.Rectangle(0, 0, mapImage.Width, mapImage.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, mapImage.PixelFormat);
			Marshal.Copy(imageData, 0, bitmapData.Scan0, imageData.Length);
			mapImage.UnlockBits(bitmapData);

			return mapImage;
		}

		public static BitmapImage ToBitmapImage(Bitmap bitmap) {
			using (var memory = new System.IO.MemoryStream()) {
				bitmap.Save(memory, ImageFormat.Png);
				memory.Position = 0;

				var bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.StreamSource = memory;
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.EndInit();
				bitmapImage.Freeze();

				return bitmapImage;
			}
		}

		public static double mix(double value, double thresholdLow, double thresholdHigh, double outputLow, double outputHigh) {
			double output = 0;

			// Scale current to the range 0.0 - 1.0
			double current = (value - thresholdLow) / (thresholdHigh - thresholdLow);

			output += current * outputHigh;
			output += outputLow * (1.0 - current);

			return output;
		}

		public static byte mix(byte value, byte thresholdLow, byte thresholdHigh, byte outputLow, byte outputHigh) {
			return (byte)mix((double)value, (double)thresholdLow, (double)thresholdHigh, (double)outputLow, (double)outputHigh);
		}

		public static double GetLightIntensity(BiomeMap map, int x, int y, double scale) {
			int sizeX = map.SizeX;
			int sizeY = map.SizeY;

			double heightTopLeft = 0.0;
			double heightBottomRight = 0.0;
			double heightCentre = 0.0;
			int xCarry = 0;
			int yCarry = 0;

			int count = 0;
			for (int i = -1; i < 2; i++) {
				for (int j = -1; j < 2; j++) {
					FixRange(x + j, y + i, sizeX, sizeY, ref xCarry, ref yCarry, true);

					if (count == 0 || count == 1 || count == 3) {
						heightTopLeft += map.Biomes[(x + xCarry + j) + ((y + yCarry + i) * sizeX), BiomeRegistry.BIOME_MOUNTAIN] / byte.MaxValue;
					}
					else if (count == 5 || count == 7 || count == 8) {
						heightBottomRight += map.Biomes[(x + xCarry + j) + ((y + yCarry + i) * sizeX), BiomeRegistry.BIOME_MOUNTAIN] / byte.MaxValue;
					}
					else {
						heightCentre += map.Biomes[(x + xCarry + j) + ((y + yCarry + i) * sizeX), BiomeRegistry.BIOME_MOUNTAIN] / byte.MaxValue;
					}

					count++;
				}
			}

			heightTopLeft /= 4.0;
			heightBottomRight /= 4.0;

			double difference = heightBottomRight - heightTopLeft;
			difference = Math.Min(Math.Max(difference, -0.001), 0.001);
			difference += 0.001;
			difference *= 1.0 / 0.001;

			return difference;
		}
		
		public static void FixRange(int x, int y, int rangeX, int rangeY, ref int xCarry, ref int yCarry, bool rotatePoles) {
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
