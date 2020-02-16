using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Interop;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Threading;

namespace MapMaker {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow_Legacy : Window {
        Random random;

        [ThreadStatic]
        public static Bitmap mapImage;

        Thread flatThread;
        Thread displayThread;
		
		bool displayPaused;
		bool proceed;
		bool display_run;
		bool display_stopNextTurn = false;
		Perlin perlin;
		GeneratorSimple generator;

		double[,] perlin_heat_latitude;
		double[,] perlin_heat_noise;
		double[,] perlin_heat_merged;
		double[,] perlin_heat;

		double[,] perlin_humidity_global_variance;
		double[,] perlin_humidity_noise;
		double[,] perlin_humidity_merged;
		double[,] perlin_humidity;

		double[,] perlin_height_global_variance;
		double[,] perlin_height_base;
		double[,] perlin_height_noise;
		double[,] perlin_height_merged;
		double[,] perlin_height;

		double[,] perlin_average;

		bool biomesGenerated;
		bool closed;

		public MainWindow_Legacy() {
            random = new Random();
			closed = false;
            InitializeComponent();
			biomesGenerated = false;
			int time = (int)(DateTime.Now.ToBinary() % int.MaxValue);

			int sizeX = 1920;
			int sizeY = 1080;
			perlin = new Perlin(time);

			float size = 1.0f;
			//perlin_average = GeneratePerlin(sizeX, sizeY, 5, size * 1, size * 0.5625, 1, 1);
			//DisplayImage(perlin_average);

			/*
			perlin_average = GeneratePerlin(sizeX, sizeY, 5, 1, 0.5625, 4, 0.3);

			perlin_heat = GenerateHeatMap(480, 270);
			perlin_humidity = GenerateHumidityMap(480, 270);

			Console.WriteLine("Scaling Perlin...");
			perlin_heat = Enlarge(perlin_heat, 2);
			perlin_humidity = Enlarge(perlin_humidity, 2);

			perlin_heat = Average(perlin_heat, 8, perlin_average);
			perlin_humidity = Average(perlin_humidity, 8, perlin_average);

			perlin_heat = Enlarge(perlin_heat, 2);
			perlin_humidity = Enlarge(perlin_humidity, 2);

			perlin_heat = Average(perlin_heat, 8, perlin_average);
			perlin_humidity = Average(perlin_humidity, 8, perlin_average);
			Console.WriteLine("Done!");

			perlin_height = GenerateHeightMap(sizeX / 1, sizeY / 1);
			Generator generator = new Generator(sizeX / 1, sizeY / 1, 8);

			DisplayImage(perlin_height);
			SaveImage("erosion_0");

			generator.Erode(4000, ref perlin_height, 0.001, 40, 4, 16);

			DisplayImage(perlin_height);
			SaveImage("erosion_1");

			generator.Erode(4000, ref perlin_height, 0.001, 40, 2, 8);

			DisplayImage(perlin_height);
			SaveImage("erosion_2");

			generator.Erode(5000, ref perlin_height, 0.001, 40, 1, 4);

			DisplayImage(perlin_height);
			SaveImage("erosion_3");

			generator.End();
			*/
		}
		
		public double[,] GenerateHeightMap(int sizeX, int sizeY) {
			perlin_height_global_variance = GeneratePerlin(sizeX, sizeY, 5, 16.0 * 0.03, 9.0 * 0.03, 4, 0.15);
			perlin_height_base = GeneratePerlin(sizeX, sizeY, 5, 1.0, 2.0, 1, 0.5);
			perlin_height_noise = GeneratePerlin(sizeX, sizeY, 5, 2.0, 1.125, 4, 1.0);

			perlin_height_merged = MergePerlin(perlin_height_base, perlin_height_noise, 0.6);

			double[,] heightMap = new double[sizeX, sizeY];

			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {

					double intensity = 0.0;

					double chosenIntensity = perlin_height_merged[x, y];
					chosenIntensity *= 2.0;
					chosenIntensity = Math.Pow(chosenIntensity, 2.25);
					chosenIntensity /= 2.0;
					chosenIntensity -= 0.75;
					chosenIntensity *= 1.5;

					perlin_height_global_variance[x, y] = Math.Min(Math.Max(2.0 * (perlin_height_global_variance[x, y] - 0.5), 0.0), 1.0);
					perlin_height_global_variance[x, y] = (1.0 - perlin_height_global_variance[x, y]);

					intensity = chosenIntensity * perlin_height_global_variance[x, y];

					intensity = Math.Min(Math.Max(intensity, 0.0), 1.0);

					heightMap[x, y] = Math.Min(Math.Max(intensity, 0.0), 1.0);
				}
			}

			return heightMap;
		}

		public double[,] Enlarge(double[,] noise, int scale) {
			int sizeX = noise.GetLength(0);
			int sizeY = noise.GetLength(1);

			double[,] output = new double[sizeX * scale, sizeY * scale];

			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {
					for (int i = 0; i < scale; i++) {
						for (int j = 0; j < scale; j++) {
							output[(x * scale) + j, (y * scale) + i] = noise[x, y];
						}
					}
				}
			}

			return output;
		}

		public double[,] GenerateHumidityMap(int sizeX, int sizeY) {
			perlin_humidity_global_variance = GeneratePerlin(sizeX, sizeY, 5, 0.5, 0.5, 1, 0.2);
			perlin_humidity_noise = GeneratePerlin(sizeX, sizeY, 5, 2, 2, 4, 0.5);
			perlin_humidity_merged = MergePerlin(perlin_humidity_global_variance, perlin_humidity_noise, 0.5);

			double[,] humidityMap = new double[sizeX, sizeY];

			double[] intensities;
			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {
					intensities = GenerateIntensities(perlin_humidity_merged, x, y, sizeX, sizeY, 2);

					double intensity = 0.5;

					if (intensities[0] > 0.5)
						intensity += 0.5;

					if (intensities[1] > 0.5)
						intensity -= 0.5;

					humidityMap[x, y] = intensity;
				}
			}

			return humidityMap;
		}

		public double[,] GenerateHeatMap(int sizeX, int sizeY) {
			perlin_heat_latitude = GenerateHeatMapY(sizeX, sizeY);
			perlin_heat_noise = GeneratePerlin(sizeX, sizeY, 5, 2, 1.125, 4, 0.6);
			perlin_heat_merged = MergePerlin(perlin_heat_latitude, perlin_heat_noise, 0.7);

			double[,] heatMap = new double[sizeX, sizeY];

			double[] intensities;
			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {
					intensities = GenerateIntensities(perlin_heat_merged, x, y, sizeX, sizeY, 3);

					double intensity = 0.5;

					if (intensities[0] > 0.5)
						intensity += 0.5;

					if (intensities[2] > 0.5)
						intensity -= 0.5;

					heatMap[x, y] = intensity;
				}
			}

			return heatMap;
		}

		public double[,] GenerateHeatMapY(int sizeX, int sizeY) {
			double[,] intensity = new double[sizeX, sizeY];

			int iceCutoff = (int)((sizeY) * Config.IceCutoff);
			int EquatorCutoff = (int)((sizeY) * Config.EquatorCutoff);
			
			double secondaryIntensity;

			for (int y = 0; y < sizeY; y++) {
				for (int x = 0;x < sizeX; x++) {
					intensity[x, y] = 0.5;
					
					if ((sizeY / 2) - Math.Abs(y - (sizeY / 2)) <= iceCutoff) {
						secondaryIntensity = iceCutoff;
						secondaryIntensity -= ((sizeY / 2) - Math.Abs(y - (sizeY / 2)));
						secondaryIntensity /= iceCutoff;
						secondaryIntensity = Math.Min(Math.Max(secondaryIntensity * 1, 0.0), 1.0);
						intensity[x, y] += secondaryIntensity / 2.5;
					}

					if (Math.Abs(y - (sizeY / 2)) <= EquatorCutoff) {
						secondaryIntensity = EquatorCutoff;
						secondaryIntensity -= Math.Abs(y - (sizeY / 2));
						secondaryIntensity /= EquatorCutoff;
						secondaryIntensity = Math.Min(Math.Max(secondaryIntensity * 1, 0.0), 1.0);
						intensity[x, y] -= secondaryIntensity / 2.5;
					}
				}
			}

			return intensity;
		}

		public double[,] Average(double[,] noise, int distance) {
			int xCarry, yCarry, count;
			int sizeX = noise.GetLength(0);
			int sizeY = noise.GetLength(1);
			double total;
			double[,] output = new double[sizeX, sizeY];

			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {
					total = 0.0;
					count = 0;

					for (int i = -distance; i < distance + 1; i++) {
						for (int j = -distance; j < distance + 1; j++) {
							xCarry = 0;
							yCarry = 0;

							FixRange(x + j, y + i, sizeX, sizeY, ref xCarry, ref yCarry, true);

							total += noise[x + xCarry + j, y + yCarry + i];
							count++;
						}
					}

					output[x, y] = total / count;
				}
			}

			return output;
		}

		public double[,] Average(double[,] noise, int distance, double[,] distanceMap) {
			int xCarry, yCarry, count;
			int sizeX = noise.GetLength(0);
			int sizeY = noise.GetLength(1);
			double total;
			double[,] output = new double[sizeX, sizeY];
			int currentDistance;

			double averageScale = distanceMap.GetLength(0) / noise.GetLength(0);

			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {
					total = 0.0;
					count = 0;
					
					currentDistance = (int)(distance * ((distanceMap[(int)(x * averageScale), (int)(y * averageScale)] + 0.01) / 1.01));

					for (int i = -currentDistance; i < currentDistance + 1; i++) {
						for (int j = -currentDistance; j < currentDistance + 1; j++) {
							xCarry = 0;
							yCarry = 0;

							FixRange(x + j, y + i, sizeX, sizeY, ref xCarry, ref yCarry, true);

							total += noise[x + xCarry + j, y + yCarry + i];
							count++;
						}
					}

					output[x, y] = total / count;
				}
			}

			return output;
		}

		public double[,] GeneratePerlin(int sizeX, int sizeY, int z, double scaleX, double scaleY, int octaves, double persistance) {
			double[,] output = new double[sizeX, sizeY];
			double indexX;
			double indexY;
			double min = double.MaxValue;
			double max = double.MinValue;

			for (int i = 0; i < sizeY; i++) {
				for (int j = 0; j < sizeX; j++) {
					indexY = ((double)i) / ((double)sizeY) * 8.0;
					indexX = ((double)j) / ((double)sizeX) * 8.0;
					output[j, i] = Math.Min(Math.Max(perlin.Noise(indexX * scaleX, indexY * scaleY, z, octaves, persistance) + 0.5, 0.0), 1.0);

					if (output[j, i] > max)
						max = output[j, i];
					else if (output[j, i] < min)
						min = output[j, i];
				}
			}


			return output;
		}

		public double[,] MergePerlinMax(double[,] perlin_0, double[,] perlin_1, double scale) {
			int sizeX = perlin_0.GetLength(0);
			int sizeY = perlin_0.GetLength(1);
			double[,] output = new double[sizeX, sizeY];

			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {
					output[x, y] = Math.Max(perlin_0[x, y], perlin_1[x, y]);
				}
			}

			return output;
		}

		public double[,] MergePerlin(double[,] perlin_0, double[,] perlin_1, double scale) {
			int sizeX = perlin_0.GetLength(0);
			int sizeY = perlin_0.GetLength(1);
			double[,] output = new double[sizeX, sizeY];
			
			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {
					output[x, y] = (scale * perlin_0[x, y]) + ((1.0 - scale) * perlin_1[x, y]);
				}
			}

			return output;
		}

		public double[,] GeneratePerlin(int sizeX, int sizeY, int z, double scaleX, double scaleY) {
			double[,] output = new double[sizeX, sizeY];
			double indexX;
			double indexY;
			double min = double.MaxValue;
			double max = double.MinValue;

			for (int i = 0; i < sizeY; i++) {
				for (int j = 0; j < sizeX; j++) {
					indexY = ((double)i) / ((double)sizeY) * 8.0;
					indexX = ((double)j) / ((double)sizeX) * 8.0;
					//output[j, i] = Perlin.OctavePerlin(indexX * scale, indexY * scale, z, 2, 0.5);
					output[j, i] = Math.Min(Math.Max(perlin.Noise(indexX * scaleX, indexY * scaleY, z, 4, 0.5) + 0.5, 0.0), 1.0);

					if (output[j, i] > max)
						max = output[j, i];
					else if (output[j, i] < min)
						min = output[j, i];
				}

				//Console.WriteLine("Generating Perlin... [" + ((int)(((double)i + 1.0) / (double)sizeY * 100.0)) + " / " + 100);
			}


			return output;
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

		double DegreesToRadians(double degrees) {
			return degrees * Math.PI / 180.0;
		}

		public double[,] Mutate(double[,] perlin0, double[,] perlin1, double intensity) {
			double maxDisplacementY = (intensity / 100.0) * perlin0.GetLength(1);
			double maxDisplacementX = (intensity / 100.0) * perlin0.GetLength(0);

			double[,] output = new double[1024, 1024];

			double min = Double.MaxValue;
			double max = Double.MinValue;

			for (int i = 0; i < 1024; i++) {
				for (int j = 0; j < 1024; j++) {
					double intensity_1_y = Math.Cos(DegreesToRadians(perlin1[j, i]));
					double intensity_1_x = Math.Sin(DegreesToRadians(perlin1[j, i]));

					if (intensity_1_y + intensity_1_x > max)
						max = intensity_1_y;

					if (intensity_1_y + intensity_1_x < min)
						min = intensity_1_x;

					double displacementY = (maxDisplacementY * intensity_1_y) - (maxDisplacementY);
					double displacementX = (maxDisplacementX * intensity_1_x) - (maxDisplacementX);

					displacementY *= 2.0;
					displacementX *= 2.0;


					double x = j + displacementX;
					double y = i + displacementY;

					if (y < 0)
						y = 0;
					else if (y >= 1024.0)
						y = 1023.0;

					if (x < 0)
						x = 0;
					else if (x >= 1024.0)
						y = 1023.0;

					double intensity_0 = perlin0[(int)(x), (int)(y)];

					output[j, i] = intensity_0;
				}
			}

			Console.WriteLine("Min:: " + min);
			Console.WriteLine("Max:: " + max);

			return output;
		}

		private float angleDistance(float a, float b) {
			float x1, x2, x3;

			x1 = Math.Abs(a - b);
			x2 = Math.Abs((a - 360.0f) - b);
			x3 = Math.Abs(a - (b - 360.0f));

			return Math.Min(Math.Min(x1, x2), x3);
		}

		private float addAngle(float a, float b) {
			float c = a + b;

			while (c < 0.0f)
				c += 360.0f;

			while (c >= 360.0f)
				c -= 360.0f;

			return c;
		}

		public float[] GenerateDistortedCircle(vec2 origin, float radius, int pointCount, float maxIntensity) {
			// { min, max } amount of distortions at every levels
			int distortionLevels = 3;
			int[,] distortionLimits = new int[,] {
				{ 6, 10 },
				{ 12, 24 },
				{ 32, 64 }
			};
			
			float minDistortion = 0.25f;

			int[] distortionCount = new int[distortionLimits.GetLength(0)];
			for (int i = 0; i < distortionLimits.GetLength(0); i++)
				distortionCount[i] = random.Next(distortionLimits[i, 0], distortionLimits[i, 1] + 1);

			float[] distortionLevelPointDistances = new float[distortionLevels];
			for (int i = 0; i < distortionLevels; i++)
				distortionLevelPointDistances[i] = 360.0f / distortionCount[i];

			float[][] distortionPoints = new float[distortionLevels][];

			for (int i = 0; i < distortionLevels; i++) {
				distortionPoints[i] = new float[distortionCount[i]];

				float step = 360.0f / distortionCount[i];
				
				for (int point = 0; point < distortionCount[i]; point++) {
					float displacementIntensity = (((float)random.NextDouble() * 2.0f) - 1.0f);
					if (displacementIntensity < minDistortion) {
						//displacementIntensity = minDistortion + (minDistortion - displacementIntensity);
					}

					float displacement = displacementIntensity * (0.4f * distortionLevelPointDistances[i]);
					distortionPoints[i][point] = step * point;
				}
			}

			float[][] distortions = new float[distortionLevels][];
			for (int i = 0; i < distortionLevels; i++) {
				distortions[i] = new float[distortionCount[i]];

				for (int point = 0; point < distortionCount[i]; point++) {
					distortions[i][point] = maxIntensity * (((float)random.NextDouble() * 2.0f) - 1.0f);
				}
			}

			// Generate the core circle
			float[] output = new float[pointCount];
			for (int i = 0; i < pointCount; i++) {
				float step = 360.0f / pointCount;
				float theta = step * i;

				float totalDistortion = 0.0f;
				float distortionStrength = 1.0f;
				float totalDistortionStrength = 0.0f;

				for (int level = 0; level < distortionLevels; level++) {
					distortionStrength = 1.0f / (float)Math.Pow(2.0, level * (0.75 * level));
					totalDistortionStrength += distortionStrength;

					float distortionLevelStep = distortionLevelPointDistances[level];
					int lastIndex = (int)(theta / distortionLevelStep);
					int nextIndex = lastIndex + 1;

					if (nextIndex >= distortionCount[level])
						nextIndex -= distortionCount[level];

					float lastPoint = distortionPoints[level][lastIndex];
					float nextPoint = distortionPoints[level][nextIndex];

					float lastDistance = angleDistance(theta, lastPoint);
					float nextDistance = angleDistance(theta, nextPoint);
					float lastIntensity = 1.0f - (lastDistance / distortionLevelStep);
					float nextIntensity = 1.0f - (nextDistance / distortionLevelStep);

					lastIntensity = (float)Math.Pow(lastIntensity, 2.0);
					nextIntensity = (float)Math.Pow(nextIntensity, 2.0);

					float totalIntensity = lastIntensity + nextIntensity;
					lastIntensity = lastIntensity / totalIntensity;
					nextIntensity = nextIntensity / totalIntensity;

					float lastDistortion = distortions[level][lastIndex];
					float nextDistortion = distortions[level][nextIndex];

					float currentDistortion = (lastDistortion * lastIntensity) + (nextDistortion * nextIntensity);
					totalDistortion += currentDistortion * distortionStrength;
				}
				
				float currentRadius = radius + radius * (totalDistortion / totalDistortionStrength);

				vec2 position = new vec2(currentRadius, currentRadius);
				//vec2 position = new vec2(origin.x + currentRadius * Math.Cos(DegreesToRadians(theta)), origin.y + currentRadius * Math.Sin(DegreesToRadians(theta)));
				output[i] = currentRadius;
			}

			return output;
		}

		// Generation zones
		struct gzone {
			// Position of the zone along the area (0.0 - 1.0, or 0.0 - 360.0)
			public float pos;
			// Size of the zone around the area.
			// In a line, 1.0 would mean the zone takes up the entire area
			// In a circle, 360.0 would mean the zone takes up the entire area
			public float size;
			// How the zone impacts generation.
			// In circle generation, how "wide" the buildable area will be at the centre of this zone
			public float intensity;

			public gzone(float p, float s, float i) {
				pos = p;
				size = s;
				intensity = i;
			}
		}

		private vec2[] GenerateLakeMap(vec2 origin, float radius, int pointCount) {
			float coreRadius = radius * 0.5f;
			float innerRadius = radius * 0.4f;
			float outerRadius = radius * 0.3f;

			float[] core = GenerateDistortedCircle(origin, coreRadius, pointCount, 0.5f);
			float[] inner = GenerateDistortedCircle(origin, innerRadius, pointCount, 0.5f);
			float[] outer = GenerateDistortedCircle(origin, outerRadius, pointCount, 1.0f);

			float maxCore = 0.0f;
			float maxInner = 0.0f;
			float minOuter = float.MaxValue;

			for (int i = 0; i < pointCount; i++) {
				if (core[i] > maxCore)
					maxCore = core[i];

				float dInner = (inner[i] < innerRadius) ? core[i] : (core[i] + (inner[i] - innerRadius));
				if (dInner > maxInner)
					maxInner = dInner;

				if (outer[i] < minOuter)
					minOuter = outer[i];
			}

			//vec2[] output = new vec2[pointCount * 2];

			float pos0 = 360.0f * (float)random.NextDouble();
			float pos1 = addAngle(pos0, 180.0f);
			pos1 = addAngle(pos1, 90.0f * (((float)random.NextDouble() * 2.0f) - 1.0f));

			float endDistance = 25.0f + 105.0f * (float)random.NextDouble();
			List<vec2> list = new List<vec2>();

			int mapType = 1;

			switch (mapType) {
				case 0:
					for (int i = 0; i < pointCount; i++) {
						float step = 360.0f / pointCount;
						float theta = step * i;

						vec2 corePosition = new vec2(origin.x + core[i] * Math.Cos(DegreesToRadians(theta)), origin.y + core[i] * Math.Sin(DegreesToRadians(theta)));

						float dInner = (inner[i] < innerRadius) ? core[i] : (core[i] + (inner[i] - innerRadius));
						float dOuter = (maxInner + (radius * 0.1f) + (outer[i] - minOuter));

						vec2 innerPosition = new vec2(origin.x + dInner * Math.Cos(DegreesToRadians(theta)), origin.y + dInner * Math.Sin(DegreesToRadians(theta)));
						vec2 outerPosition = new vec2(origin.x + dOuter * Math.Cos(DegreesToRadians(theta)), origin.y + dOuter * Math.Sin(DegreesToRadians(theta)));

						list.Add(innerPosition);
						list.Add(outerPosition);
					}
					break;
				case 1:
					float totalAreaSize  = 240.0f;
					float totalAreaPoint = 0.0f;


					for (int i = 0; i < pointCount; i++) {
						float step = 360.0f / pointCount;
						float theta = step * i;

						vec2 corePosition = new vec2(origin.x + core[i] * Math.Cos(DegreesToRadians(theta)), origin.y + core[i] * Math.Sin(DegreesToRadians(theta)));
						
						float dInner = (inner[i] < innerRadius) ? core[i] : (core[i] + (inner[i] - innerRadius));
						float dOuter = (dInner + (radius * 0.3f) + (outer[i] - minOuter));

						vec2 innerPosition = new vec2(origin.x + dInner * Math.Cos(DegreesToRadians(theta)), origin.y + dInner * Math.Sin(DegreesToRadians(theta)));
						vec2 outerPosition = new vec2(origin.x + dOuter * Math.Cos(DegreesToRadians(theta)), origin.y + dOuter * Math.Sin(DegreesToRadians(theta)));

						float distanceToMid = angleDistance(totalAreaPoint, theta);
						
						if (distanceToMid <= (totalAreaSize / 2.0f)) {
							list.Add(innerPosition);
							list.Add(outerPosition);
						}
					}
					break;
				default:
					break;
			}

			/*
			for (int i = 0; i < pointCount; i++) {
				float step = 360.0f / pointCount;
				float theta = step * i;

				vec2 corePosition = new vec2(origin.x + core[i] * Math.Cos(DegreesToRadians(theta)), origin.y + core[i] * Math.Sin(DegreesToRadians(theta)));

				float distance = angleDistance(theta, pos0);
				float outerMultiplier = 1.0f;

				if (distance < endDistance * 0.5f) {
					outerMultiplier = 0.0f;
				}
				else if (distance < endDistance) {
					outerMultiplier = (distance - (endDistance * 0.5f)) / (endDistance * 0.5f);
				}

				//vec2 outerPosition = new vec2(
				//	origin.x + (outerMultiplier * (0.5f * (core[i] + maxInner) + (radius * 0.2f) + (outer[i] - minOuter))) * Math.Cos(DegreesToRadians(theta)) + ((1.0f - outerMultiplier) * core[i]) * Math.Cos(DegreesToRadians(theta)),
				//	origin.y + (outerMultiplier * (0.5f * (core[i] + maxInner) + (radius * 0.2f) + (outer[i] - minOuter))) * Math.Sin(DegreesToRadians(theta)) + ((1.0f - outerMultiplier) * core[i]) * Math.Sin(DegreesToRadians(theta)));

				vec2 outerPosition = new vec2(
					origin.x + (maxInner + (radius * 0.1f) + (outer[i] - minOuter)) * Math.Cos(DegreesToRadians(theta)),
					origin.y + (maxInner + (radius * 0.1f) + (outer[i] - minOuter)) * Math.Sin(DegreesToRadians(theta)));

				output[i] = corePosition;
				output[pointCount + i] = outerPosition;
			}*/

			return list.ToArray();
		}

		private void DisplayUpdater(GeneratorAdvanced generator, ref bool pauseDisplay, ref bool canProceed, ref bool displayWater) {
			BiomeMap map = new BiomeMap(generator.sizeY, generator.sizeX);
			display_stopNextTurn = false;

			while (!display_stopNextTurn) {
				if (!display_run)
					display_stopNextTurn = true;

				if (pauseDisplay) {
					canProceed = true;
					while (pauseDisplay)
						Thread.Sleep(20);
				}
				
				Thread.Sleep(50);

				if (generator.CurrentMap.CanRead()) {
					generator.CurrentMap.Lock();
					map = generator.CurrentMap.Clone();
					generator.CurrentMap.Unlock();
				}

				DisplayImage(map, false);
			}
		}

		private void DisplayUpdater(GeneratorSimple generator, ref bool pauseDisplay, ref bool canProceed, ref bool renderBiomes) {
            Map map = new Map(generator.sizeY, generator.sizeX);
			display_stopNextTurn = false;

            while(!display_stopNextTurn) {
                if (!display_run)
					display_stopNextTurn = true;

				if (pauseDisplay) {
					canProceed = true;
					while (pauseDisplay)
						Thread.Sleep(20);
				}


				Thread.Sleep(50);

				if (generator.subMaps[0][0].CanRead())
					map.Set(generator.subMaps[0][0]);

				if (generator.subMaps[0][1].CanRead())
					map.MergeFlat(generator.subMaps[0][1]);

				int submapCount = generator.subMaps.Length;

				for (int i = 1; i < submapCount; i++) {
					if (generator.subMaps[i][0].CanRead())
						map.MergeFlat(generator.subMaps[i][0]);

					if (generator.subMaps[i][1].CanRead())
						map.MergeFlat(generator.subMaps[i][1]);
				}

				DisplayImage(map, biomesGenerated);
            }
        }

		public void GenerateCircleMap() {
			int mapHeight = 135;
			int mapWidth = 240;
			display_run = true;

			Map map = new Map(mapHeight, mapWidth);
			GeneratorSimple generator = new GeneratorSimple(mapHeight, mapWidth, 12);
			generator.AddSubmap();
			generator.AddSubmap();
			generator.AddSubmap();
			generator.WorkOn(0);

			double rangeMin = 0.04;
			double rangeMax = 0.08;
			generator.SeedMapSpread(0, 40, new Range(0, 40), generator.CurrentSubmap.seedQueueWater, 1, 4, new RangeD(rangeMin, rangeMax), new RangeD(rangeMin, rangeMax), 20);
			generator.SeedMapSpread(20, 100, new Range(0, 100), generator.CurrentSubmap.seedQueueWater, 1, 4, new RangeD(rangeMin, rangeMax), new RangeD(rangeMin, rangeMax), 30);
			generator.SeedMapSpread(40, 150, new Range(0, 150), generator.CurrentSubmap.seedQueueWater, 1, 4, new RangeD(rangeMin, rangeMax), new RangeD(rangeMin, rangeMax), 30);
			generator.SeedMapSpread(80, 150, new Range(0, 150), generator.CurrentSubmap.seedQueueWater, 1, 4, new RangeD(rangeMin, rangeMax), new RangeD(rangeMin, rangeMax), 30);
			generator.SeedMapSpread(100, 150, new Range(0, 150), generator.CurrentSubmap.seedQueueWater, 1, 4, new RangeD(rangeMin, rangeMax), new RangeD(rangeMin, rangeMax), 30);
			generator.FillMapOcean();

			generator.ResetSeedCache();

			bool renderBiomes = false;

			displayThread = new Thread(() => DisplayUpdater(generator, ref displayPaused, ref proceed, ref renderBiomes));
			displayThread.Start();

			int seed = new Random().Next(0, int.MaxValue / 2);
			Console.WriteLine("seed :: " + seed);

			Random random = new Random(seed);

			vec2 start = new vec2(random.Next(map.sizeX / 8, map.sizeX / 4), random.Next(map.sizeY / 8, map.sizeY / 4));
			vec2 end = new vec2(map.sizeX, map.sizeY) - new vec2(random.Next(map.sizeX / 8, map.sizeX / 4), random.Next(map.sizeY / 8, map.sizeY / 4));

			Console.WriteLine("(" + start.x + "," + start.y + ")");
			Console.WriteLine("(" + end.x + "," + end.y + ")");

			Console.WriteLine("\n\n");

			double maxDisplacement = 64.0;
			vec2[] circlePoints = GenerateLakeMap(generator.Centre, generator.CurrentSubmap.SizeY / 4.0f, 360);

			// Generate Seeds

			generator.SeedMap(circlePoints);
			//generator.SeedMapSpread(Config.LandCutoff, 16, new Range(0, 1), generator.CurrentSubmap.seedQueueLand, 4, 4, new RangeD(0.05, 0.2), new RangeD(0.001, 0.002), 24);


			// Generate islands

			// Connect islands by splines
			// TODO: curve splines into/out of island centre

			// Grow islands

			generator.End();

			display_run = false;
			Console.WriteLine("Ending timer...");
			displayThread.Join();
		}

		public void GenerateSplineMap() {
			int mapHeight = 135;
			int mapWidth = 240;
			display_run = true;

			Map map = new Map(mapHeight, mapWidth);
			GeneratorSimple generator = new GeneratorSimple(mapHeight, mapWidth, 12);
			generator.AddSubmap();
			generator.AddSubmap();
			generator.AddSubmap();
			generator.WorkOn(0);

			double rangeMin = 0.04;
			double rangeMax = 0.08;
			generator.SeedMapSpread(0, 40, new Range(0, 40), generator.CurrentSubmap.seedQueueWater, 1, 4, new RangeD(rangeMin, rangeMax), new RangeD(rangeMin, rangeMax), 20);
			generator.SeedMapSpread(20, 100, new Range(0, 100), generator.CurrentSubmap.seedQueueWater, 1, 4, new RangeD(rangeMin, rangeMax), new RangeD(rangeMin, rangeMax), 30);
			generator.SeedMapSpread(40, 150, new Range(0, 150), generator.CurrentSubmap.seedQueueWater, 1, 4, new RangeD(rangeMin, rangeMax), new RangeD(rangeMin, rangeMax), 30);
			generator.SeedMapSpread(80, 150, new Range(0, 150), generator.CurrentSubmap.seedQueueWater, 1, 4, new RangeD(rangeMin, rangeMax), new RangeD(rangeMin, rangeMax), 30);
			generator.SeedMapSpread(100, 150, new Range(0, 150), generator.CurrentSubmap.seedQueueWater, 1, 4, new RangeD(rangeMin, rangeMax), new RangeD(rangeMin, rangeMax), 30);
			generator.FillMapOcean();

			generator.ResetSeedCache();

			bool renderBiomes = false;

			displayThread = new Thread(() => DisplayUpdater(generator, ref displayPaused, ref proceed, ref renderBiomes));
			displayThread.Start();

			int seed = new Random().Next(0, int.MaxValue / 2);
			Console.WriteLine("seed :: " + seed);

			Random random = new Random(seed);

			vec2 start = new vec2(random.Next(map.sizeX / 8, map.sizeX / 4), random.Next(map.sizeY / 8, map.sizeY / 4));
			vec2 end = new vec2(map.sizeX, map.sizeY) - new vec2(random.Next(map.sizeX / 8, map.sizeX / 4), random.Next(map.sizeY / 8, map.sizeY / 4));

			Console.WriteLine("(" + start.x + "," + start.y + ")");
			Console.WriteLine("(" + end.x + "," + end.y + ")");

			Console.WriteLine("\n\n");

			double maxDisplacement = 64.0;
			SplineBezier spline = SplineBezier.Generate(start, end, maxDisplacement, 2, seed);
			Perlin perlin = new Perlin(seed);
			Console.WriteLine("Spline ::");
			Console.WriteLine("(" + spline.Points[0].x + "," + spline.Points[0].y + ")");
			Console.WriteLine("(" + spline.Points[1].x + "," + spline.Points[1].y + ")");
			Console.WriteLine("(" + spline.Points[2].x + "," + spline.Points[2].y + ")");
			Console.WriteLine("(" + spline.Points[3].x + "," + spline.Points[3].y + ")");

			// Generate Seeds

			generator.SeedMap(spline, 100);
			//generator.SeedMapSpread(Config.LandCutoff, 16, new Range(0, 1), generator.CurrentSubmap.seedQueueLand, 4, 4, new RangeD(0.05, 0.2), new RangeD(0.001, 0.002), 24);


			// Generate islands

			// Connect islands by splines
			// TODO: curve splines into/out of island centre

			// Grow islands

			generator.End();

			display_run = false;
			Console.WriteLine("Ending timer...");
			displayThread.Join();
		}

		float[] ByteArrayToFloatArray(byte[] bytes) {
			float[] output = new float[bytes.Length];

			for (int i = 0; i < bytes.Length; i++)
				output[i] = (int)bytes[i];

			return output;
		}

		float[] IntArrayToFloatArray(int[] ints) {
			float[] output = new float[ints.Length];

			for (int i = 0; i < ints.Length; i++)
				output[i] = ints[i];

			return output;
		}

		public void GenerateAdvancedMap() {
			int mapHeight = 135;
			int mapWidth = 240;
			display_run = true;
			bool renderBiomes = false;

			if (displayThread != null) {
				if (displayThread.IsAlive) {
					displayThread.Abort();
					displayThread.Join();
				}
			}

			int time = (int)(DateTime.Now.ToBinary() % int.MaxValue);
			GeneratorAdvanced generator = new GeneratorAdvanced(mapWidth, mapHeight, 12, time);
			generator.WorkOn(0);

			bool displayWater = true;
			displayThread = new Thread(() => DisplayUpdater(generator, ref displayPaused, ref proceed, ref displayWater));
			displayThread.Start();

			generator.GenerateGroundHardness(3, 3, 6, 6, 3, 0.3f, 0.1f, 0.2f);
			DisplayImage(((BaseMap<float>)generator.FunctionMaps[GenMap.MAP_BEDROCK_HARDNESS]).Data, generator.sizeX, generator.sizeY, 255.0f);

			//generator.GenerateTectonicPlates(2, 1, 7, 3);
			generator.GenerateNoise(1.0f * 2.0f, 0.5625f * 2.0f, 2, 1.0f);
			
			double rangeMin = 0.04;
			double rangeMax = 0.08;

			int quarterMountain = (int)(Math.Ceiling(Maths.Mix(Config.MountainCutoff, 255, 0.25)));
			int quarterOcean = (int)(Math.Floor(Maths.Mix(Config.LandCutoff, 0, 0.25)));

			ivec2<RangeD> oceanGlobalSeedRange = new ivec2<RangeD>(new RangeD(0.0, 0.2), new RangeD(0.0, 0.5));
			ivec2<RangeD> oceanLocalSeedRange = new ivec2<RangeD>(new RangeD(0.0, 0.3), new RangeD(0.05, 0.5));

			ivec2<RangeD> mountainGlobalSeedRange = new ivec2<RangeD>(new RangeD(rangeMin, rangeMax), new RangeD(0.0, 0.3));
			ivec2<RangeD> mountainLocalSeedRange = new ivec2<RangeD>(new RangeD(rangeMin, rangeMax), new RangeD(0.01, 0.025));

			Console.WriteLine("Generating ocean seeds");
			// Generate Oceans
			generator.GenerateSeedBranches(new Range(0, quarterOcean), new Range(0, Config.LandCutoff - 1), 1, 16, 16, new Range(0, 400), new Range(0, 40), new vec2i(3, 2), oceanGlobalSeedRange, oceanLocalSeedRange, ref generator.CurrentSubmap.branchQueueOcean, 24, 1.0);
			//generator.SpawnAllSeedBranches(generator.branchQueueOcean);
			Console.WriteLine("Spawning ocean seeds");
			generator.FillMapWater();

			Console.WriteLine("Generating mountain seeds");
			generator.GenerateSeedBranches(new Range(quarterMountain, 255), new Range(Config.LandCutoff, 255), -1, 3, 4, new Range(3, 6),  new Range(0, 1), new vec2i(4, 6), mountainGlobalSeedRange, mountainLocalSeedRange, ref generator.CurrentSubmap.branchQueueMnt, 36, 4);
			generator.SpreadMap(1, 28, 0, 4);
			generator.SpreadMap(1, 0, 8, 4);
			generator.Curve();
			generator.SpreadMap(1, 0, 4, 4);

			generator.AddSubmap(ref generator.CurrentSubmap);

			generator.WorkOn(1);
			generator.FillMapWater();
			generator.GenerateSeedBranches(new Range(quarterMountain, 255), new Range(Config.LandCutoff, 255), -1, 3, 4, new Range(0, 0), new Range(3, 6), new vec2i(2, 6), mountainGlobalSeedRange, mountainLocalSeedRange, ref generator.CurrentSubmap.branchQueueMnt, 32, 6);
			generator.SpreadMap(1, 26, 0, 4);
			generator.SpreadMap(1, 0, 8, 4);
			generator.Curve();
			generator.WorkOn(0);
			generator.MergeLand(0, 1, false);

			generator.WorkOn(1);
			generator.FillMapWater();
			generator.GenerateSeedBranches(new Range(quarterMountain, 255), new Range(Config.LandCutoff, 255), -1, 3, 3, new Range(0, 0),  new Range(2, 4), new vec2i(2, 4), mountainGlobalSeedRange, mountainLocalSeedRange, ref generator.CurrentSubmap.branchQueueMnt, 24, 8);
			generator.SpreadMap(1, 16, 0, 4);
			generator.SpreadMap(1, 0, 4, 4);
			generator.WorkOn(0);
			generator.MergeLand(0, 1, false);

			generator.SpreadMap(1, 8, 0, 4);
			generator.SpreadMap(1, 0, 2, 4);

			Console.WriteLine("Spawned mountain seeds");
			displayWater = false;
			
			// Map Size 2 START
			mapHeight *= 2;
			mapWidth *= 2;
			generator.Enlarge(2);
			Console.WriteLine("Generating Map :: [Size 2 - Start]");

			generator.SpreadMap(1, 0, 4, 8);

			generator.WorkOn(1);
			generator.FillMapWater();
			generator.GenerateSeedBranches(new Range(quarterMountain, 255), new Range(Config.LandCutoff, 255), -1, 8, 0, new Range(1, 4), new Range(2, 4), new vec2i(3, 4), mountainGlobalSeedRange, mountainLocalSeedRange, ref generator.CurrentSubmap.branchQueueMnt, 36, 4);
			//generator.GenerateOrbitSeedBranches(new Range(quarterMountain, 255), new Range(Config.LandCutoff, 255), -1, 12, 0, new Range(1, 4), new Range(2, 4), new vec2i(3, 4), mountainGlobalSeedRange, mountainLocalSeedRange, new Range(5, 16), ref generator.CurrentSubmap.branchQueueMnt, 36, 4);
			generator.SpreadMap(1, 16, 0, 8);
			//generator.SpreadMap(1, 0, 2, 8);
			generator.WorkOn(0);
			generator.MergeLand(0, 1, false);
			generator.SpreadMap(1, 0, 4, 8);
			Console.WriteLine("Generating Map :: [Size 2 - End]");
			// Map Size 2 END

			// Map Size 3 START
			mapHeight *= 2;
			mapWidth *= 2;
			generator.Enlarge(2);
			Console.WriteLine("Generating Map :: [Size 3 - Start]");

			generator.SpreadMap(1, 0, 4, 10);
			Console.WriteLine("Generating Map :: [Size 3 - End]");
			// Map Size 3 END

			// Map Size 4 START
			mapHeight *= 2;
			mapWidth *= 2;
			generator.Enlarge(2);
			Console.WriteLine("Generating Map :: [Size 4 - Start]");

			generator.SpreadMap(1, 0, 8, 12);
			generator.SpreadMap(1, 8, 0, 12);

			generator.SpreadMap(1, 0, 4, 12);
			Console.WriteLine("Generating Map :: [Size 4 - End]");
			// Map Size 4 END

			display_run = false;
			Console.WriteLine("Ending timer...");
			displayThread.Join();

			//generator.GenerateBiomes();
			//DisplayImage(generator.CurrentMap, true);

			/*
			byte[] map = new byte[generator.CurrentMap.Data.Length];

			for (int i = 0; i < generator.CurrentMap.Data.Length; i++) {
				byte height = generator.CurrentMap[i];

				if (height < Config.LandCutoff)
					height = 0;

				map[i] = height;
			}

			DisplayImage(map, generator.CurrentMap.SizeX, generator.CurrentMap.SizeY, false);*/
			Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

			watch.Stop();
			long elapsedMs = watch.ElapsedMilliseconds;
			Console.WriteLine("Ended Timer!");
		}

		public void GenerateMapFlat() {
            int mapHeight = 135;
            int mapWidth = 240;
			display_run = true;
			bool renderBiomes = false;

            //SubMap map = new SubMap(mapHeight, mapWidth);
            Map mapIslands = new Map(mapHeight, mapWidth);
            Map mapIslands2 = new Map(mapHeight, mapWidth);
            Map mapIslands3 = new Map(mapHeight, mapWidth);

            Map[] tempMaps = new Map[2];

            //tempMaps[0] = new Map(map.sizeY, map.sizeX);
            //tempMaps[1] = new Map(map.sizeY, map.sizeX);

            if (displayThread != null) {
                if (displayThread.IsAlive) {
                    displayThread.Abort();
                    displayThread.Join();
                }
			}

			generator = new GeneratorSimple(mapHeight, mapWidth, 12);
			generator.AddSubmap();
			generator.AddSubmap();
			generator.AddSubmap();
			generator.WorkOn(0);

			displayThread = new Thread(() => DisplayUpdater(generator, ref displayPaused, ref proceed, ref renderBiomes));
            displayThread.Start();

			//Console.WriteLine("Height :: " + generator.CurrentSubmap[0][60]);


			//SeedOceans(map, 16);
			Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
			//SpreadMap(mapConnectors, 1, 5, 0, tempMaps);

			Console.WriteLine("Generating Oceans...");

			double rangeMin = 0.04;
			double rangeMax = 0.08;

			generator.WorkOn(0);
			generator.SeedMapSpread(0, 40, new Range(0, 40), generator.CurrentSubmap.seedQueueWater, 1, 4, new RangeD(rangeMin, rangeMax), new RangeD(rangeMin, rangeMax), 20);
			generator.SeedMapSpread(20, 100, new Range(0, 100), generator.CurrentSubmap.seedQueueWater, 1, 4, new RangeD(rangeMin, rangeMax), new RangeD(rangeMin, rangeMax), 30);
			generator.SeedMapSpread(40, 150, new Range(0, 150), generator.CurrentSubmap.seedQueueWater, 1, 4, new RangeD(rangeMin, rangeMax), new RangeD(rangeMin, rangeMax), 30);
			generator.SeedMapSpread(80, 150, new Range(0, 150), generator.CurrentSubmap.seedQueueWater, 1, 4, new RangeD(rangeMin, rangeMax), new RangeD(rangeMin, rangeMax), 30);
			generator.SeedMapSpread(100, 150, new Range(0, 150), generator.CurrentSubmap.seedQueueWater, 1, 4, new RangeD(rangeMin, rangeMax), new RangeD(rangeMin, rangeMax), 30);
			generator.FillMapOcean();

			generator.ResetSeedCache();

			generator.SeedMapSpread(Config.LandCutoff, 16, new Range(0, 1), generator.CurrentSubmap.seedQueueLand, 4, 4, new RangeD(0.05, 0.2), new RangeD(0.001, 0.002), 24);

			//	------------------------------ LEVEL 1 ------------------------------
			generator.WorkOn(0);

			Console.WriteLine("Oceans Generated");

			Console.WriteLine(">> 1 [" + generator.CurrentSubmap.SizeX + " x " + generator.CurrentSubmap.SizeY + "]");
			generator.WorkOn(0);
			//generator.SpreadMap(1, 60, 0, 4);

			generator.SpreadMap(1, 40, 0, 4);
			generator.SpreadMap(1, 0, 24, 4);
			generator.SpreadMap(1, 48, 36, 4);
			
			generator.WorkOn(1);
			generator.SeedMapSpread(180, 16, new Range(6, 12), generator.CurrentSubmap.seedQueueLand, 2, 2, new RangeD(0.01, 0.05), new RangeD(0.001, 0.002), 24);
			generator.SpreadMap(1, 36, 16, 4);
			generator.MergeFlat(0, 1, true);
			generator.WorkOn(0);
			generator.SpreadMap(1, 16, 24, 4);
			/*
			generator.WorkOn(1);
			generator.SeedMapSpread(180, 12, new Range(6, 12), generator.CurrentSubmap.seedQueueLand, 2, 2, new RangeD(0.01, 0.05), new RangeD(0.001, 0.002), 16);
			generator.SpreadMap(1, 48, 16, 4);
			generator.MergeFlat(0, 1, true);
			generator.WorkOn(0);
			*/
			generator.CurveTop();
			
			generator.WorkOn(0);


			Console.WriteLine("<< 1");
			generator.WorkOn(0);
			generator.SpreadMap(1, 0, 16, 4);

			generator.WorkOn(0);

			generator.WorkOn(1);
			generator.SeedMapSpread(180, 8, new Range(0, 16), generator.CurrentSubmap.seedQueueLand, 2, 2, new RangeD(0.01, 0.1), new RangeD(0.01, 0.05), 16);
			generator.SpreadMap(1, 48, 8, 4);
			generator.MergeFlat(0, 1, true);
			generator.WorkOn(0);

			mapHeight *= 2;
            mapWidth *= 2;
			generator.Enlarge(2);
			generator.SpreadMap(1, 0, 8, 8);
			Console.WriteLine(">> 2 [" + generator.CurrentSubmap.SizeX + " x " + generator.CurrentSubmap.SizeY + "]");

			//	------------------------------ LEVEL 2 ------------------------------

			generator.WorkOn(1);
			//generator.SeedMapSpread(200, 16, new Range(2, 10), generator.CurrentSubmap.seedQueueLand, 2, 2, new RangeD(0.01, 0.1), new RangeD(0.05, 0.1), 24);
			//generator.SpreadMap(1, 40, 16, 4);
			generator.MergeFlat(0, 1, true);
			generator.WorkOn(0);

			Console.WriteLine("<< 2");
			generator.SpreadMap(1, 0, 24, 8);
			generator.SpreadMap(1, 24, 16, 8);
			mapHeight *= 2;
			mapWidth *= 2;
			generator.Enlarge(2);
			//generator.SpreadMap(1, 0, 36, 8);
			Console.WriteLine(">> 3 [" + generator.CurrentSubmap.SizeX + " x " + generator.CurrentSubmap.SizeY + "]");
			
			//generator.WorkOn(1);
			//generator.SeedMapSpread(200, 16, new Range(2, 10), generator.CurrentSubmap.seedQueueLand, 2, 2, new RangeD(0.02, 0.1), new RangeD(0.02, 0.05), 24);
			//generator.SpreadMap(1, 40, 16, 4);
			//generator.MergeFlat(0, 1, true);
			generator.WorkOn(0);

			generator.SpreadMap(1, 0, 24, 8);
			generator.SpreadMap(1, 48, 16, 8);
			//generator.SpreadMap(1, 0, 16, 8);
			//generator.SpreadMap(1, 16, 0, 8);

			//	------------------------------ LEVEL 3 ------------------------------
			//generator.SpreadMap(1, 48, 0, 12);
			Console.WriteLine("<< 3");
			generator.WorkOn(0);
			mapHeight *= 2;
			mapWidth *= 2;
			generator.Enlarge(2);
			generator.SpreadMap(1, 24, 8, 12);
			Console.WriteLine(">> 4 [" + generator.CurrentSubmap.SizeX + " x " + generator.CurrentSubmap.SizeY + "]");


			//	------------------------------ LEVEL 4 ------------------------------
			//generator.SpreadMap(1, 30, 0, 12);
			Console.WriteLine("<< 4");

			//generator.SpreadMap(1, 0, 24, 12);


			Console.WriteLine(">> 5");

			//generator.SpreadBiomesAll(4, 16, 8);

			generator.GenerateBiomes(ref perlin_heat, ref perlin_humidity, ref perlin_height);
			biomesGenerated = true;
			
			//generator.DistortBiomes(perlin_3_smooth_0, perlin_3_smooth_1, 0.05, 1);
			//generator.DistortBiomes(perlin_3_smooth_0, perlin_3_smooth_1, 0.05, 1);
			//generator.DistortBiomes(perlin_3_smooth_0, perlin_3_smooth_1, 0.05, 1);

			display_run = false;
			Console.WriteLine("Ending timer...");
			displayThread.Join();

			DisplayImage(generator.CurrentSubmap.CurrentMap, true);
			
			Console.WriteLine("<< 5");

			watch.Stop();
            long elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine("Ended Timer!");

        }

		public double mix(double value, double thresholdLow, double thresholdHigh, double outputLow, double outputHigh) {
			double output = 0;
			
			// Scale current to the range 0.0 - 1.0
			double current = (value - thresholdLow) / (thresholdHigh - thresholdLow);

			output += current * outputHigh;
			output += outputLow * (1.0 - current);

			return output;
		}

		public byte mix(byte value, byte thresholdLow, byte thresholdHigh, byte outputLow, byte outputHigh) {
			return (byte)mix((double)value, (double)thresholdLow, (double)thresholdHigh, (double)outputLow, (double)outputHigh);
		}

		public double[] GenerateIntensities(double[,] perlin, int x, int y, int sizeX, int sizeY, int sectionCount) {
			double perlinStrength = perlin[x, y];
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

		public double GetLightIntensity(Map map, int x, int y, double scale) {
			double heightTopLeft = 0.0;
			double heightBottomRight = 0.0;
			double heightCentre = 0.0;
			int xCarry = 0;
			int yCarry = 0;

			int count = 0;
			for (int i = -1; i < 2; i++) {
				for (int j = -1; j < 2; j++) {
					FixRange(x + j, y + i, map.sizeX, map.sizeY, ref xCarry, ref yCarry, true);

					if (count == 0 || count == 1 || count == 3) {
						heightTopLeft += map.Biomes[(x + xCarry + j) + ((y + yCarry + i) * map.sizeY), BiomeRegistry.BIOME_MOUNTAIN] / byte.MaxValue;
					}
					else if (count == 5 || count == 7 || count == 8) {
						heightBottomRight += map.Biomes[(x + xCarry + j) + ((y + yCarry + i) * map.sizeY), BiomeRegistry.BIOME_MOUNTAIN] / byte.MaxValue;
					}
					else {
						heightCentre += map.Biomes[(x + xCarry + j) + ((y + yCarry + i) * map.sizeY), BiomeRegistry.BIOME_MOUNTAIN] / byte.MaxValue;
					}

					count++;
				}
			}

			//heightTopLeft += heightCentre;
			//heightBottomRight += heightCentre;

			heightTopLeft /= 4.0;
			heightBottomRight /= 4.0;

			double difference =  heightBottomRight - heightTopLeft;
			difference = Math.Min(Math.Max(difference, -0.001), 0.001);
			difference += 0.001;
			difference *= 1.0 / 0.001;

			return difference;
		}

		public double GetLightIntensity(BiomeMap map, int x, int y, double scale) {
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

			//heightTopLeft += heightCentre;
			//heightBottomRight += heightCentre;

			heightTopLeft /= 4.0;
			heightBottomRight /= 4.0;

			double difference = heightBottomRight - heightTopLeft;
			difference = Math.Min(Math.Max(difference, -0.001), 0.001);
			difference += 0.001;
			difference *= 1.0 / 0.001;

			return difference;
		}

		byte[] depths = new byte[] { 84, 27, 21, 255 };
		byte[] water = new byte[] { 232, 82, 27, 255 };
		byte[] waterCoast = new byte[] { 221, 232, 27, 255 };
		byte[] beach = new byte[] { 78, 159, 165, 255 };
		byte[] grass = new byte[] { 18, 232, 57, 255 };
		byte[] forest = new byte[] { 32, 173, 58, 255 };
		byte[] peak = new byte[] { 233, 233, 233, 255 };

		byte[] rainforest = new byte[] { 12, 94, 21, 255 };
		//byte[] rainforest = new byte[] { 18, 232, 57, 255 };
		byte[] desert = new byte[] { 85, 215, 224, 255 };

		byte[] rock_bright = new byte[] { 70, 80, 75, 255 };
		byte[] rock_mid = new byte[] { 50, 55, 60, 255 };
		byte[] rock_dark = new byte[] { 45, 45, 45, 255 };

		byte[] dust = new byte[] { 25, 25, 25, 255 };

		byte[] snow = new byte[] { 255, 255, 255, 255 };

		byte[] iceWeak = new byte[] { 255, 255, 200, 255 };
		byte[] iceStrong = new byte[] { 255, 255, 255, 255 };

		byte levelDepths = 0;
		byte levelWater = (byte)(Config.LandCutoff - 1);
		//byte levelBeach = 80;
		byte levelGrass = Config.LandCutoff;
		byte levelPeak = 255;

		public void DisplayImage(BiomeMap map, bool useBiomes = false) {
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
			if (!closed) {
				mapDisplay.Dispatcher.Invoke((Action)delegate {
					mapImage = new Bitmap(sizeX, sizeY, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
					var bitmapData = mapImage.LockBits(new System.Drawing.Rectangle(0, 0, mapImage.Width, mapImage.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, mapImage.PixelFormat);
					Marshal.Copy(imageData, 0, bitmapData.Scan0, imageData.Length);
					mapImage.UnlockBits(bitmapData);

					BitmapImage bmpImage = ToBitmapImage(mapImage);
					mapDisplay.Source = bmpImage;
				});
			}
		}

		public void DisplayImage(Map map, bool useBiomes = false) {
            byte[] imageData = new byte[map.sizeY * map.sizeX * 4];

            for (int y = 0; y < map.sizeY; y++) {
                for (int x = 0; x < map.sizeX; x++) {
					int mapHeight = map[(x + (y * map.sizeX))];

					if (useBiomes) {
						double iGrass = map.Biomes[(x + (y * map.sizeX)), BiomeRegistry.BIOME_GRASS];

						double iDesert = map.Biomes[(x + (y * map.sizeX)), BiomeRegistry.BIOME_DESERT];
						double iRainforest = map.Biomes[(x + (y * map.sizeX)), BiomeRegistry.BIOME_RAINFOREST];

						double iIceStrong = map.Biomes[(x + (y * map.sizeX)), BiomeRegistry.BIOME_ICE_STRONG];
						double iIceWeak = map.Biomes[(x + (y * map.sizeX)), BiomeRegistry.BIOME_ICE_WEAK];

						double iOceanDeep = map.Biomes[(x + (y * map.sizeX)), BiomeRegistry.BIOME_OCEAN_DEEP];
						double iOceanShallow = map.Biomes[(x + (y * map.sizeX)), BiomeRegistry.BIOME_OCEAN_SHALLOW];

						double iMountain = map.Biomes[(x + (y * map.sizeX)), BiomeRegistry.BIOME_MOUNTAIN];

						iGrass /= byte.MaxValue;
						iMountain /= byte.MaxValue;

						iDesert /= byte.MaxValue;
						iRainforest /= byte.MaxValue;

						iIceStrong /= byte.MaxValue;
						iIceWeak /= byte.MaxValue;

						iOceanDeep /= byte.MaxValue;
						iOceanShallow /= byte.MaxValue;

						for (int i = 0; i < 4; i++)
							imageData[(x + (y * map.sizeX)) * 4 + i] = (byte)((iIceStrong * iceStrong[i]) + (iIceWeak * iceWeak[i]));

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
								imageData[(x + (y * map.sizeX)) * 4 + i] = (byte)((overallMountain * ((iMountainBright * rock_bright[i]) + (iMountainMid * rock_mid[i]) + (iMountainDark * rock_dark[i]) + (iMountainSnow * snow[i]) + (iMountainDust * dust[i]))) + (mountainLeftover * ((iGrass * forest[i]) + (iDesert * desert[i]) + (iRainforest * rainforest[i]) + (iIceStrong * iceStrong[i]) + (iIceWeak * iceWeak[i]))));
							
							//imageData[(x + (y * map.sizeX)) * 4 + 3] = 255;
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
								imageData[(x + (y * map.sizeX)) * 4 + i] += (byte)((iOceanShallow * waterCoast[i]) + (iOceanDeep * depths[i]));

							//imageData[(x + (y * map.sizeX)) * 4 + 3] = 255;
							//imageData[(x + (y * map.sizeX)) * 4 + i] = (byte)((intensity_shallow * water[i]) + (intensity_deep * depths[i]) + (intensity_ice_weak * iceWeak[i]) + (intensity_ice_strong * iceStrong[i]));
						}
						else if (iIceStrong == 0.0 && iIceWeak == 0.0) {

						}
					}
					else {
						if (mapHeight < levelGrass) {
							for (int i = 0; i < 4; i++)
								imageData[(x + (y * map.sizeX)) * 4 + i] = mix((byte)mapHeight, levelDepths, levelWater, depths[i], water[i]);
						}
						else {
							for (int i = 0; i < 4; i++)
								imageData[(x + (y * map.sizeX)) * 4 + i] = mix((byte)mapHeight, levelGrass, levelPeak, grass[i], peak[i]);
						}
					}
                }
            }
			if (!closed) {
				mapDisplay.Dispatcher.Invoke((Action)delegate {
					mapImage = new Bitmap(map.sizeX, map.sizeY, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
					var bitmapData = mapImage.LockBits(new System.Drawing.Rectangle(0, 0, mapImage.Width, mapImage.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, mapImage.PixelFormat);
					Marshal.Copy(imageData, 0, bitmapData.Scan0, imageData.Length);
					mapImage.UnlockBits(bitmapData);

					BitmapImage bmpImage = ToBitmapImage(mapImage);
					mapDisplay.Source = bmpImage;
				});
			}
		}

		public void DisplayImage(double[,] heat, double[,] humidity) {
			int sizeX = heat.GetLength(0);
			int sizeY = heat.GetLength(1);
			byte[] imageData = new byte[sizeY * sizeX * 4];

			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {
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

					iIceStrong = Math.Min(Math.Max(intensities_humidity[0] * iCold, 0.0), 1.0);
					iIceWeak = Math.Min(Math.Max(intensities_humidity[1] * iCold, 0.0), 1.0);

					iGrass = Math.Min(Math.Max(intensities_humidity[0] * iTemperate, 0.0), 1.0);
					iForest = Math.Min(Math.Max(intensities_humidity[1] * iTemperate, 0.0), 1.0);

					iDesert = Math.Min(Math.Max(intensities_humidity[0] * iHot, 0.0), 1.0);
					iRainforest = Math.Min(Math.Max(intensities_humidity[1] * iHot, 0.0), 1.0);

					double leftover = Math.Min(Math.Max((1.0 - iCold) - iHot, 0.0), 1.0);

					for (int i = 0; i < 4; i++) {
						imageData[(x + (y * sizeX)) * 4 + i] = (byte)((iTemperate * forest[i]) + (iIceStrong * iceStrong[i]) + (iIceWeak * iceWeak[i]) + (iDesert * desert[i]) + (iRainforest * rainforest[i]));
					}
				}
			}

			mapDisplay.Dispatcher.Invoke((Action)delegate {
				mapImage = new Bitmap(sizeX, sizeY, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				var bitmapData = mapImage.LockBits(new System.Drawing.Rectangle(0, 0, mapImage.Width, mapImage.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, mapImage.PixelFormat);
				Marshal.Copy(imageData, 0, bitmapData.Scan0, imageData.Length);
				mapImage.UnlockBits(bitmapData);

				BitmapImage bmpImage = ToBitmapImage(mapImage);
				mapDisplay.Source = bmpImage;
			});
		}

		public void DisplayImage(byte[] map, int sizeX, int sizeY, bool displayWater) {
			byte[] imageData = new byte[sizeY * sizeX * 4];

			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {
					byte mapHeight = map[x + y * sizeX];

					if (!displayWater) {
						if (mapHeight < Config.LandCutoff)
							mapHeight = 0;
					}

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
						imageData[(x + (y * sizeX)) * 4 + i] = (byte)(mapHeight);
					}

					imageData[(x + (y * sizeX)) * 4 + 3] = 255;
				}
			}

			mapDisplay.Dispatcher.Invoke((Action)delegate {
				mapImage = new Bitmap(sizeX, sizeY, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				var bitmapData = mapImage.LockBits(new System.Drawing.Rectangle(0, 0, mapImage.Width, mapImage.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, mapImage.PixelFormat);
				Marshal.Copy(imageData, 0, bitmapData.Scan0, imageData.Length);
				mapImage.UnlockBits(bitmapData);

				BitmapImage bmpImage = ToBitmapImage(mapImage);
				mapDisplay.Source = bmpImage;
			});
		}

		public void DisplayImage(float[] map, int sizeX, int sizeY, float multiplier) {
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

			mapDisplay.Dispatcher.Invoke((Action)delegate {
				mapImage = new Bitmap(sizeX, sizeY, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				var bitmapData = mapImage.LockBits(new System.Drawing.Rectangle(0, 0, mapImage.Width, mapImage.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, mapImage.PixelFormat);
				Marshal.Copy(imageData, 0, bitmapData.Scan0, imageData.Length);
				mapImage.UnlockBits(bitmapData);

				BitmapImage bmpImage = ToBitmapImage(mapImage);
				mapDisplay.Source = bmpImage;
			});
		}

		public void DisplayImage(double[,] noise) {
			int sizeX = noise.GetLength(0);
			int sizeY = noise.GetLength(1);
			byte[] imageData = new byte[sizeY * sizeX * 4];

			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {
					double mapHeight = noise[x, y];

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
						imageData[(x + (y * sizeX)) * 4 + i] = (byte)(mapHeight * 255.0);
					}

					imageData[(x + (y * sizeX)) * 4 + 3] = 255;
				}
			}

			mapDisplay.Dispatcher.Invoke((Action)delegate {
				mapImage = new Bitmap(sizeX, sizeY, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				var bitmapData = mapImage.LockBits(new System.Drawing.Rectangle(0, 0, mapImage.Width, mapImage.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, mapImage.PixelFormat);
				Marshal.Copy(imageData, 0, bitmapData.Scan0, imageData.Length);
				mapImage.UnlockBits(bitmapData);

				BitmapImage bmpImage = ToBitmapImage(mapImage);
				mapDisplay.Source = bmpImage;
			});
		}

		public void SaveImage(String name) {
            mapImage.Save("C:\\Maps\\" + name + ".png");
		}

		public void SaveImage() {
			SaveImage("tmp");
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

        private void ButtonRegenerate(object sender, RoutedEventArgs e) {
            mapImage = null;
			biomesGenerated = false;
            random = new Random();

            flatThread = new Thread(() => GenerateAdvancedMap());
            flatThread.Start();
        }

        private void ButtonSave(object sender, RoutedEventArgs e) {
            SaveImage();
		}

		// ------------------------- Landscape -------------------------

		private void ButtonShowLandscape(object sender, RoutedEventArgs e) {
			DisplayImage(generator.CurrentSubmap.CurrentMap, false);
		}

		private void ButtonShowBiomes(object sender, RoutedEventArgs e) {
			DisplayImage(generator.CurrentSubmap.CurrentMap, true);
		}

		// ------------------------- Height -------------------------

		private void ButtonShowHeightGlobalVariance(object sender, RoutedEventArgs e) {
			DisplayImage(perlin_height_global_variance);
		}

		private void ButtonShowHeightBase(object sender, RoutedEventArgs e) {
			DisplayImage(perlin_height_base);
		}

		private void ButtonShowHeightNoise(object sender, RoutedEventArgs e) {
			DisplayImage(perlin_height_noise);
		}

		private void ButtonShowHeightMerged(object sender, RoutedEventArgs e) {
			DisplayImage(perlin_height_merged);
		}

		private void ButtonShowHeightEroded(object sender, RoutedEventArgs e) {
			DisplayImage(perlin_height);
		}

		// ------------------------- Heat -------------------------

		private void ButtonShowHeatLatitude(object sender, RoutedEventArgs e) {
			DisplayImage(perlin_heat_latitude);
		}

		private void ButtonShowHeatNoise(object sender, RoutedEventArgs e) {
			DisplayImage(perlin_heat_noise);
		}

		private void ButtonShowHeatMerged(object sender, RoutedEventArgs e) {
			DisplayImage(perlin_heat_merged);
		}

		private void ButtonShowHeatSectioned(object sender, RoutedEventArgs e) {
			DisplayImage(perlin_heat);
		}

		// ------------------------- Humidity -------------------------

		private void ButtonShowHumidityGlobalVariance(object sender, RoutedEventArgs e) {
			DisplayImage(perlin_humidity_global_variance);
		}

		private void ButtonShowHumidityNoise(object sender, RoutedEventArgs e) {
			DisplayImage(perlin_humidity_noise);
		}

		private void ButtonShowHumidityMerged(object sender, RoutedEventArgs e) {
			DisplayImage(perlin_humidity_merged);
		}

		private void ButtonShowHumiditySectioned(object sender, RoutedEventArgs e) {
			DisplayImage(perlin_humidity);
		}

		private void Window_Closed(object sender, EventArgs e) {
			closed = true;
			display_run = false;
		}
	}
}