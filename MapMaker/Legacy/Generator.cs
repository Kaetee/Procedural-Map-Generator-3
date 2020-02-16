using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MapMaker {
	public class GeneratorSimple : GeneratorBase {
		protected int currentSubmapIndex;
		protected int submapCount;
		public SubMap[] subMaps;
		public double[,] height;

		protected List<int> seedSpawnsX;
		protected List<int> seedSpawnsY;

		public GeneratorSimple() { }

		public GeneratorSimple(int inSizeY, int inSizeX, int threadCount) {
			random = new Random();
			sizeX = inSizeX;
			sizeY = inSizeY;
			submapCount = 0;
			subMaps = new SubMap[0];

			seedSpawnsX = new List<int>();
			seedSpawnsY = new List<int>();

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

		public void GenerateBiomes(ref double[,] heat, ref double[,] humidity, ref double[,] height) {
			subMaps[0][0].InitialiseBiomes(ref heat, ref humidity, ref height);

			subMaps[0][1].InitialiseBiomes(ref heat, ref humidity, ref height);
		}

		public void GenerateBiomes() {
			subMaps[0][0].InitialiseBiomes();
			subMaps[0][1].InitialiseBiomes();
		}

		public vec2 Centre {
			get { return new vec2(CurrentSubmap.SizeX / 2.0, CurrentSubmap.SizeY / 2.0); }
		}

		public void DistortBiomes(double[,] noise_0, double[,] noise_1, double intensityPercentage, int spreadCount) {
			int xCarry;
			int yCarry;
			/*
			int spreadCount = threadInstructions[threadID].spreadIterations;

			int startY = threadInstructions[threadID].startY;
			int startX = threadInstructions[threadID].startX;
			int endY = threadInstructions[threadID].endY;
			int endX = threadInstructions[threadID].endX;
			int randomSeed = threadInstructions[threadID].seed;
			*/

			ref SubMap currentSubmap = ref CurrentSubmap;

			double intensityX = intensityPercentage * CurrentSubmap.SizeX;
			double intensityY = intensityPercentage * CurrentSubmap.SizeY;

			int startX = 0;
			int endX = CurrentSubmap.SizeX - 1;
			int startY = 0;
			int endY = CurrentSubmap.SizeY - 1;

			
			Random random = new Random();

			for (int it = 0; it < spreadCount; it++) {
				//Console.WriteLine("Iterations :: [" + (it + 1) + " / " + spreadCount + "]");
				// -----
				// Make Thread Caller Flip Current Map
				// -----

				currentSubmap.OtherMap.Set(currentSubmap.CurrentMap);

				for (int y = startY; y < endY + 1; y++) {
					for (int x = startX; x < endX + 1; x++) {
						
						int i = (int)(2 * (noise_0[x, y] - 0.5) * intensityY);
						int j = (int)(2 * (noise_1[x, y] - 0.5) * intensityX);

						// Check surrounding square
						xCarry = 0;
						yCarry = 0;

						FixRange(x + j, y + i, currentSubmap.SizeX, currentSubmap.SizeY, ref xCarry, ref yCarry, true);

						for (int b = 0; b < BiomeRegistry.BIOME_COUNT; b++) {
							//currentSubmap.OtherMap.Biomes[x + y * currentSubmap.SizeX, b] = currentSubmap.CurrentMap.Biomes[enlarg), b];
						}
					}
				}

				currentSubmap.FlipMap();
			}
		}

		public void ResetSeedCache() {
			seedSpawnsX = new List<int>();
			seedSpawnsY = new List<int>();
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

		private void SpreadBiome(int threadID) {
			int xCarry;
			int yCarry;
			int spreadCount = threadInstructions[threadID].spreadIterations;

			int startY = threadInstructions[threadID].startY;
			int startX = threadInstructions[threadID].startX;
			int endY = threadInstructions[threadID].endY;
			int endX = threadInstructions[threadID].endX;
			int randomSeed = threadInstructions[threadID].seed;
			byte biomeID = threadInstructions[threadID].biomeID;

			ref SubMap currentSubmap = ref CurrentSubmap;

			double biomeStrengthRange = 0.2 * byte.MaxValue;
			
			Random random = new Random(randomSeed);
			byte spreaderBiomeStrength;
			byte otherBiomeStrength;
			int outputBiomeStrength;
			double spreadChance;

			for (int it = 0; it < spreadCount; it++) {
				//Console.WriteLine("Iterations :: [" + (it + 1) + " / " + spreadCount + "]");
				// -----
				// Make Thread Caller Flip Current Map
				// -----

				while (waitThread[threadID])
					Thread.Sleep(20);

				for (int y = startY; y < endY + 1; y++) {
					for (int x = startX; x < endX + 1; x++) {
						// Check surrounding square
						for (int i = -1; i < 2; i++) {
							for (int j = -1; j < 2; j++) {
								if (i == 0 && j == 0)
									continue;

								xCarry = 0;
								yCarry = 0;

								FixRange(x + j, y + i, currentSubmap.SizeX, currentSubmap.SizeY, ref xCarry, ref yCarry, true);

								if (currentSubmap.OtherMap.CanBecomeBiome((x + xCarry + j) + ((y + yCarry + i) * currentSubmap.SizeX), biomeID)) {
									spreaderBiomeStrength = currentSubmap.CurrentMap.Biomes[(x + xCarry + j) + ((y + yCarry + i) * currentSubmap.SizeX), biomeID];
									otherBiomeStrength = currentSubmap.CurrentMap.Biomes[x + y * currentSubmap.SizeX, biomeID];

									if (spreaderBiomeStrength > 0 && spreaderBiomeStrength > otherBiomeStrength) {
										spreadChance = spreaderBiomeStrength;
										spreadChance /= byte.MaxValue;
										spreadChance *= spreadChance;
										// to-do :: multiply by distance to ideal distance (0.0 - 1.0)
										spreadChance *= 64;

										int outcome = random.Next((int)spreadChance, 100);

										if (outcome > 98) {
											outputBiomeStrength = (byte)Math.Min(Math.Max(spreaderBiomeStrength + (((2.0 * random.NextDouble()) - 1.0) * biomeStrengthRange), otherBiomeStrength), 255);

											currentSubmap.OtherMap.SetBiome(x + y * currentSubmap.SizeX, biomeID, (byte)outputBiomeStrength);
										}
									}
								}
							}
						}
					}

				}

				waitThread[threadID] = true;
			}
		}

		public void SpreadBiomesAll(int iterations, int spreads, int inThreadCount) {
			int threadCount = Math.Min(inThreadCount, threads.Length);
			ref SubMap currentSubmap = ref CurrentSubmap;

			for (int it = 0; it < iterations; it++) {
				for (int b = 0; b < BiomeRegistry.BIOME_COUNT; b++) {
					if (b == BiomeRegistry.BIOME_NULL)
						continue;

					if (b == BiomeRegistry.BIOME_OCEAN_DEEP)
						continue;

					if (b == BiomeRegistry.BIOME_OCEAN_SHALLOW)
						continue;

					if (b == BiomeRegistry.BIOME_GRASS)
						continue;

					if (b == BiomeRegistry.BIOME_DESERT)
						continue;

					if (b == BiomeRegistry.BIOME_RAINFOREST)
						continue;


					for (int t = 0; t < threadCount; t++) {
						int threadID = t;
						int xStep = currentSubmap.SizeX / threadCount;
						int xStart = xStep * t;
						int xEnd = (t == threadCount - 1) ? currentSubmap.SizeX - 1 : xStep * (t + 1) - 1;
						int spreadIterations = spreads;
						byte biomeID = (byte)b;

						threadInstructions[t].function = ThreadInstructions.FUNC_BIOME_SPREAD_STD;
						threadInstructions[t].seed = random.Next();
						threadInstructions[t].spreadIterations = spreadIterations;
						threadInstructions[t].startY = 0;
						threadInstructions[t].endY = currentSubmap.SizeY - 1;
						threadInstructions[t].startX = xStart;
						threadInstructions[t].endX = xEnd;
						threadInstructions[t].biomeID = biomeID;

						waitThread[t] = false;
					}

					for (int t = 0; t < threadCount; t++)
						while (!waitThread[t])
							Thread.Sleep(20);

					for (int ls = 0; ls < spreads; ls++) {
						// Allow all threads to proceed
						for (int t = 0; t < threadCount; t++)
							waitThread[t] = false;

						// Wait for threads to finish processing
						for (int t = 0; t < threadCount; t++)
							while (!waitThread[t])
								Thread.Sleep(20);

						currentSubmap.FlipMap();
					}

					for (int t = 0; t < threadCount; t++)
						while (!waitThread[t])
							Thread.Sleep(20);
				}
			}
		}

		private void AverageBiome(int threadID) {
			int xCarry;
			int yCarry;
			int spreadCount = threadInstructions[threadID].spreadIterations;

			int startY = threadInstructions[threadID].startY;
			int startX = threadInstructions[threadID].startX;
			int endY = threadInstructions[threadID].endY;
			int endX = threadInstructions[threadID].endX;
			int randomSeed = threadInstructions[threadID].seed;
			byte biomeID = threadInstructions[threadID].biomeID;

			ref SubMap currentSubmap = ref CurrentSubmap;

			double biomeStrengthRange = 0.2 * byte.MaxValue;

			Random random = new Random(randomSeed);
			double biomeStrengthTotal;
			int surroundingCount;

			for (int it = 0; it < spreadCount; it++) {
				//Console.WriteLine("Iterations :: [" + (it + 1) + " / " + spreadCount + "]");
				// -----
				// Make Thread Caller Flip Current Map
				// -----

				while (waitThread[threadID])
					Thread.Sleep(20);

				for (int y = startY; y < endY + 1; y++) {
					for (int x = startX; x < endX + 1; x++) {
						biomeStrengthTotal = 0.0;
						surroundingCount = 0;

						biomeStrengthTotal += currentSubmap.CurrentMap.Biomes[x + y * currentSubmap.SizeX, biomeID];
						surroundingCount++;

						if (biomeStrengthTotal <= 0)
							continue;

						// Check surrounding square
						for (int i = -1; i < 2; i++) {
							for (int j = -1; j < 2; j++) {
								if (j == 0 & i == 0)
									continue;

								xCarry = 0;
								yCarry = 0;

								FixRange(x + j, y + i, currentSubmap.SizeX, currentSubmap.SizeY, ref xCarry, ref yCarry, true);

								biomeStrengthTotal += currentSubmap.CurrentMap.Biomes[(x + xCarry + j) + ((y + yCarry + i) * currentSubmap.SizeX), biomeID];
								surroundingCount++;
							}
						}

						biomeStrengthTotal /= surroundingCount;

						currentSubmap.OtherMap.SetBiome(x + y * currentSubmap.SizeX, biomeID, (byte)biomeStrengthTotal);
					}

				}

				waitThread[threadID] = true;
			}
		}

		public void AverageBiomesAll(int iterations, int inThreadCount) {
			int threadCount = Math.Min(inThreadCount, threads.Length);
			ref SubMap currentSubmap = ref CurrentSubmap;

			for (int b = 0; b < BiomeRegistry.BIOME_COUNT; b++) {
				if (b == BiomeRegistry.BIOME_NULL)
					continue;

				if (b == BiomeRegistry.BIOME_OCEAN_DEEP)
					continue;

				if (b == BiomeRegistry.BIOME_OCEAN_SHALLOW)
					continue;

				if (b == BiomeRegistry.BIOME_GRASS)
					continue;

				for (int t = 0; t < threadCount; t++) {
					int threadID = t;
					int xStep = currentSubmap.SizeX / threadCount;
					int xStart = xStep * t;
					int xEnd = (t == threadCount - 1) ? currentSubmap.SizeX - 1 : xStep * (t + 1) - 1;
					int spreadIterations = iterations;
					byte biomeID = (byte)b;

					threadInstructions[t].function = ThreadInstructions.FUNC_BIOME_AVERAGE_STD;
					threadInstructions[t].seed = random.Next();
					threadInstructions[t].spreadIterations = spreadIterations;
					threadInstructions[t].startY = 0;
					threadInstructions[t].endY = currentSubmap.SizeY - 1;
					threadInstructions[t].startX = xStart;
					threadInstructions[t].endX = xEnd;
					threadInstructions[t].biomeID = biomeID;

					waitThread[t] = false;
				}

				for (int t = 0; t < threadCount; t++)
					while (!waitThread[t])
						Thread.Sleep(20);

				for (int ls = 0; ls < iterations; ls++) {
					// Allow all threads to proceed
					for (int t = 0; t < threadCount; t++)
						waitThread[t] = false;

					// Wait for threads to finish processing
					for (int t = 0; t < threadCount; t++)
						while (!waitThread[t])
							Thread.Sleep(20);

					currentSubmap.FlipMap();
				}

				for (int t = 0; t < threadCount; t++)
					while (!waitThread[t])
						Thread.Sleep(20);
			}
		}

		public double Distance(int ox, int oy, int dx, int dy) {
			return Distance((double) ox, (double)oy, (double)dx, (double)dy);
		}

		public double Distance(double ox, double oy, double dx, double dy) {
			return Math.Sqrt(((dx - ox) * (dx - ox)) + ((dy - oy) * (dy - oy)));
		}

		public double PickUpSediment(ref double[,] inMap, ref double[,] outMap, int x, int y, int range, double pickupStrength, double velocity) {
			double distanceScaledStrength;
			double maxDistanceStrength = Distance(0, 0, 4, 4);
			double totalSediment = 0.0;
			int xCarry = 0;
			int yCarry = 0;

			int sizeX = inMap.GetLength(0);
			int sizeY = inMap.GetLength(1);

			double currentPickup = 0.0;
			double decreasedSediment = 0.0;
			double distance;

			
			for (int i = -range; i < range + 1; i++) {
				for (int j = -range; j < range + 1; j++) {
					distance = Distance(0, 0, j, i) / 1.0;

					if (distance > range)
						continue;

					FixRange(x + j, y + i, sizeX, sizeY, ref xCarry, ref yCarry, true);
					distanceScaledStrength = (maxDistanceStrength - Distance(0, 0, j, i)) / maxDistanceStrength;

					currentPickup = distanceScaledStrength * pickupStrength * velocity;
					totalSediment += currentPickup;
					
					decreasedSediment = outMap[x + xCarry + j, y + yCarry + i] - currentPickup;
					outMap[x + xCarry + j, y + yCarry + i] = Math.Min(Math.Max(decreasedSediment, 0.0), 1.0);
				}
			}
			/*
			currentPickup = pickupStrength * velocity;
			decreasedSediment = outMap[x, y] - currentPickup;
			outMap[x, y] = Math.Min(Math.Max(decreasedSediment, 0.0), 1.0);
			*/
			totalSediment = currentPickup;

			return totalSediment;
		}

		public double DepositSediment(ref double[,] inMap, ref double[,] outMap, int x, int y, int range, double totalSediment, double strength, double depositPercentage) {
			double distanceScaledStrength;
			double maxDistanceStrength = Distance(0, 0, range, range);
			double[,] depositIntensity = new double[(range * 2) + 1, (range * 2 ) + 1];
			double totalDeposited = 0.0;

			double min = double.MaxValue;

			double distance;

			int sizeX = inMap.GetLength(0);
			int sizeY = inMap.GetLength(1);

			for (int i = -range; i < range + 1; i++) {
				for (int j = -range; j < range + 1; j++) {
					if (i == 0 && j == 0)
						continue;
					
					distance = Distance(0, 0, j, i) / 1.0;

					if (distance > range)
						continue;

					distanceScaledStrength = maxDistanceStrength - (distance / (maxDistanceStrength / 2.0));

					if (distanceScaledStrength > 1.0)
						distanceScaledStrength = 1.0 - (distanceScaledStrength - 1.0);

					distanceScaledStrength = Math.Pow(distanceScaledStrength, 3.0);

					if (distanceScaledStrength < min)
						min = distanceScaledStrength;

					//distanceScaledStrength = Math.Pow(distanceScaledStrength, 2.0);
					depositIntensity[j + range, i + range] = distanceScaledStrength * strength;
					totalDeposited += depositIntensity[j + range, i + range];
				}
			}

			//if (min < 0.0)

			// Scales the sediment deposits so that extra half of the total sediment is deposited
			double multiplier = totalSediment / totalDeposited;
			int xCarry = 0;
			int yCarry = 0;

			//Console.WriteLine("totalDeposited :: " + totalDeposited);
			//Console.WriteLine("totalSediment :: " + totalSediment);
			//Console.WriteLine("multiplier :: " + multiplier);
			//Console.WriteLine("remainingSediment :: " + (totalSediment - (depositPercentage * multiplier * totalDeposited)));

			double newTotalDeposited = 0.0;

			for (int i = -range; i < range + 1; i++) {
				for (int j = -range; j < range + 1; j++) {

					FixRange(x + j, y + i, sizeX, sizeY, ref xCarry, ref yCarry, true);

					depositIntensity[j + range, i + range] = depositPercentage * (depositIntensity[j + range, i + range] * multiplier);
					newTotalDeposited += depositIntensity[j + range, i + range];
					outMap[x + xCarry + j, y + yCarry + i] += depositIntensity[j + range, i + range];
				}
			}

			return newTotalDeposited;
		}

		public double Angle(int ox, int oy, int d1x, int d1y, int d2x, int d2y) {
			double o_to_d1 = Distance(ox, oy, d1x, d1y);
			double o_to_d2 = Distance(ox, oy, d2x, d2y);

			double d1_to_d2 = Distance(d1x, d1y, d2x, d2y);
			
			return ToDegrees(Math.Acos((Math.Pow(o_to_d1, 2.0) + Math.Pow(o_to_d2, 2.0) - Math.Pow(d1_to_d2, 2.0)) / (2 * o_to_d1 * o_to_d2)));
		}

		public void Erode(int iterations, ref double[,] map, double minHeight, int maxDistance, int pickupRange, int depositRange) {
			int sizeX = map.GetLength(0);
			int sizeY = map.GetLength(1);

			double[,] outMap = (double[,])map.Clone();

			double currentHoldings = 0.0;
			int lastX = 0;
			int lastY = 0;
			double currentVelocity = 1.0;
			double pickupStrength = 0.03;
			double depositStrength = 0.03;
			int maxAttempts = 1000;
			int currentAttempts;
			bool foundValidPosition;
			int xCarry, yCarry;
			double nextPositionHeight;
			vec2i chosenNeightbour;

			List<vec2i> neighbourDirections = new List<vec2i>();
			vec2i lastDirection = new vec2i(0, 0);

			double pickedUpSediment;
			double depositedSediment;
			int neighbourCount;

			double[][,] heightMaps = new double[][,] { (double[,])map.Clone(), (double[,])map.Clone() };

			int currentIndex = 0;

			double maxDirectionDistance = Distance(-1, -1, 1, 1);
			double neighbourDirectionDistance = Distance(0, 0, 1, 1);
			double angleToLastDirection;

			double currentMinimum;
			List<vec2i> lowestNeighbours = new List<vec2i>();

			for (int it = 0; it < iterations; it++) {
				foundValidPosition = false;
				currentVelocity = 0.75;
				currentAttempts = 0;
				currentAttempts = 0;
				lastX = 0;
				lastY = 0;

				while (currentAttempts < maxAttempts && !foundValidPosition) {
					currentAttempts++;

					lastX = random.Next(sizeX);
					lastY = random.Next(sizeY);

					if (map[lastX, lastY] > minHeight)
						foundValidPosition = true;
				}


				// Move Droplet
				if (foundValidPosition) {
					for (int step = 0; step < maxDistance; step++) {
						currentMinimum = map[lastX, lastY];
						lowestNeighbours.Clear();
						xCarry = 0;
						yCarry = 0;
						neighbourCount = 0;

						for (int i = -1; i < 2; i++) {
							for (int j = -1; j < 2; j++) {
								// Skip neighbour travelled from
								if (i == 0 && j == 0)
									continue;

								FixRange(lastX + j, lastY + i, sizeX, sizeY, ref xCarry, ref yCarry, false);

								nextPositionHeight = map[lastX + xCarry + j, lastY + yCarry + i];
								if (nextPositionHeight < 0.99) {
									if (nextPositionHeight < currentMinimum) {
										lowestNeighbours.Clear();
										neighbourDirections.Clear();
										currentMinimum = nextPositionHeight;

										lowestNeighbours.Add(new vec2i(lastX + xCarry + j, lastY + yCarry + i));
										neighbourDirections.Add(new vec2i(j, i));
									}
									else if (nextPositionHeight <= currentMinimum && currentMinimum < map[lastX, lastY]) {
										neighbourDirections.Add(new vec2i(j, i));
										lowestNeighbours.Add(new vec2i(lastX + xCarry + j, lastY + yCarry + i));
									}
								}

								neighbourCount++;
							}
						}

						// If no possible cells have been found, try breaking through flat land with momentum
						if (lowestNeighbours.Count == 0) {
							neighbourCount = 0;

							for (int i = -1; i < 2; i++) {
								for (int j = -1; j < 2; j++) {
									// Skip neighbour travelled from
									if (i == 0 && j == 0)
										continue;

									angleToLastDirection = Angle(0, 0, j, i, lastDirection.x, lastDirection.y);

									FixRange(lastX + j, lastY + i, sizeX, sizeY, ref xCarry, ref yCarry, false);

									nextPositionHeight = map[lastX + xCarry + j, lastY + yCarry + i];
									if (nextPositionHeight <= currentMinimum && angleToLastDirection < 135.0) {
										neighbourDirections.Add(new vec2i(j, i));
										lowestNeighbours.Add(new vec2i(lastX + xCarry + j, lastY + yCarry + i));
									}
								}

								neighbourCount++;
							}

							if (lowestNeighbours.Count > 0)
								currentVelocity -= 0.01;
						}

						// If no possible cells have been found, try breaking through hills with momentum
						if (lowestNeighbours.Count == 0) {
							neighbourCount = 0;

							for (int i = -1; i < 2; i++) {
								for (int j = -1; j < 2; j++) {
									// Skip neighbour travelled from
									if (i == 0 && j == 0)
										continue;

									angleToLastDirection = Angle(0, 0, j, i, lastDirection.x, lastDirection.y);
									angleToLastDirection /= 180.0;
									angleToLastDirection *= 2.0;

									FixRange(lastX + j, lastY + i, sizeX, sizeY, ref xCarry, ref yCarry, false);

									nextPositionHeight = map[lastX + xCarry + j, lastY + yCarry + i];
									if (nextPositionHeight < (currentMinimum + (angleToLastDirection * (0.002 * (currentVelocity - 0.75))))) {
										neighbourDirections.Add(new vec2i(j, i));
										lowestNeighbours.Add(new vec2i(lastX + xCarry + j, lastY + yCarry + i));
									}
								}

								neighbourCount++;
							}

							if (lowestNeighbours.Count > 0)
								currentVelocity -= 0.015;
						}

						if (lowestNeighbours.Count > 0) {
							// Choose neighbour (in case of multiple at equal height)
							int neighbourIndex = random.Next(0, lowestNeighbours.Count);
							chosenNeightbour = lowestNeighbours[neighbourIndex];
							lastDirection = neighbourDirections[neighbourIndex];

							// Pick up sediment
							///pickedUpSediment = (pickupStrength * currentVelocity);
							///map[chosenNeightbour.x, chosenNeightbour.y] -= pickedUpSediment;
							///currentHoldings += pickedUpSediment;
							pickedUpSediment = PickUpSediment(ref heightMaps[currentIndex], ref heightMaps[Flip(currentIndex)], lastX, lastY, pickupRange, pickupStrength, currentVelocity);
							currentHoldings += pickedUpSediment;

							//if (currentHoldings < 0.0)
							//	Console.WriteLine("pickedUpSediment :: " + currentHoldings);

							// Adjust velocity now that the droplet moved
							///currentVelocity += (map[lastX, lastY]) - (map[chosenNeightbour.x, chosenNeightbour.y] + pickedUpSediment);

							// Deposit half the sediment to the surrounding neighbours
							depositedSediment = DepositSediment(ref heightMaps[currentIndex], ref heightMaps[Flip(currentIndex)], chosenNeightbour.x, chosenNeightbour.y, depositRange, currentHoldings, depositStrength, 0.7);
							currentHoldings -= depositedSediment;
							
							// For the next iteration, this is now the old origin
							lastX = chosenNeightbour.x;
							lastY = chosenNeightbour.y;
							currentVelocity += 0.01;
						}
						else if (step > 0) {
							DepositSediment(ref heightMaps[currentIndex], ref heightMaps[Flip(currentIndex)], lastX, lastY, depositRange, currentHoldings, depositStrength, 1.0);

							// If no valid neighbours exist, the droplet has reached the end of its journey
							step = maxDistance;
						}
					}
				}

				currentIndex = Flip(currentIndex);
			}

			map = heightMaps[currentIndex];
		}

		public int Flip(int i) {
			return (i == 0) ? 1 : 0;
		}

		private void SpreadMapLand(int threadID) {
			int xCarry;
			int yCarry;
			int spreadCount = threadInstructions[threadID].spreadIterations;

			int startY = threadInstructions[threadID].startY;
			int startX = threadInstructions[threadID].startX;
			int endY = threadInstructions[threadID].endY;
			int endX = threadInstructions[threadID].endX;
			int randomSeed = threadInstructions[threadID].seed;

			ref SubMap currentSubmap = ref CurrentSubmap;

			int spreaderHeight = 0;
			int otherHeight = 0;
			int newHeight = 0;
			Random random = new Random(randomSeed);
			int max = 0;
			int spreadChance;

			int x, y, j, i;

			for (int it = 0; it < spreadCount; it++) {
				//Console.WriteLine("Iterations :: [" + (it + 1) + " / " + spreadCount + "]");
				// -----
				// Make Thread Caller Flip Current Map
				// -----

				while (waitThread[threadID])
					Thread.Sleep(20);

				for (y = startY; y < endY + 1; y++) {
					for (x = startX; x < endX + 1; x++) {
						// currentSubmap.CurrentMap.Height[x + y * currentSubmap.CurrentMap.sizeX] >= 80
						if (false) { // was > 80
							currentSubmap.OtherMap.Copy(currentSubmap.CurrentMap, x + y * currentSubmap.CurrentMap.sizeX, x + y * currentSubmap.CurrentMap.sizeX);
							continue;
						}

						// Check surrounding square
						for (i = -1; i < 2; i++) {
							for (j = -1; j < 2; j++) {
								if (i == 0 && j == 0)
									continue;

								xCarry = 0;
								yCarry = 0;

								FixRange(x + j, y + i, currentSubmap.SizeX, currentSubmap.SizeY, ref xCarry, ref yCarry, true);

								if (currentSubmap.CurrentMap.Height[(x + xCarry + j) + ((y + yCarry + i) * currentSubmap.SizeX)] >= Config.LandCutoff) { // was > 80
									spreadChance = currentSubmap.CurrentMap.LandSpread[(x + xCarry + j) + ((y + yCarry + i) * currentSubmap.SizeX),
									   (i + 1),
									   (j + 1)];
									

									int outcome = random.Next(spreadChance, 164);

									if (outcome > 154) {
										spreaderHeight = currentSubmap.CurrentMap.Height[(x + xCarry + j) + ((y + yCarry + i) * currentSubmap.SizeX)];
										otherHeight = currentSubmap.OtherMap.Height[x + y * currentSubmap.SizeX];
										newHeight = Math.Min(Math.Max(Config.LandCutoff, Average(otherHeight, spreaderHeight)), 255);

										if (newHeight < otherHeight)
											newHeight = Math.Min(Math.Max(Config.LandCutoff, Average(otherHeight, newHeight)), 255);

										currentSubmap.OtherMap.Height[x + y * currentSubmap.SizeX] = (byte)newHeight;
										currentSubmap.OtherMap.SetLandSpread(currentSubmap.CurrentMap, (x + xCarry + j) + ((y + yCarry + i) * currentSubmap.SizeX), x + (y * currentSubmap.SizeX));
									}
								}
							}
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
			int endY = threadInstructions[threadID].endY;
			int endX = threadInstructions[threadID].endX;
			int randomSeed = threadInstructions[threadID].seed;
			int spreaderHeight = 0;
			int otherHeight = 0;
			int newHeight = 0;

			ref SubMap currentSubmap = ref CurrentSubmap;
			Random random = new Random(randomSeed);

			for (int it = 0; it < spreadCount; it++) {
				//Console.WriteLine("Iterations :: [" + (it + 1) + " / " + spreadCount + "]");
				// -----
				// Make Thread Caller Flip Current Map
				// -----

				while (waitThread[threadID])
					Thread.Sleep(20);

				for (int y = startY; y < endY + 1; y++) {
					for (int x = startX; x < endX + 1; x++) {
						//currentSubmap.CurrentMap.Height[x + y * currentSubmap.SizeX] < 80
						if (false) { // was < 50
							currentSubmap.OtherMap.Copy(currentSubmap.CurrentMap, x + y * currentSubmap.CurrentMap.sizeX, x + y * currentSubmap.CurrentMap.sizeX);
							continue;
						}

						// Check surrounding square
						for (int i = -1; i < 2; i++) {
							for (int j = -1; j < 2; j++) {
								if (i == 0 && j == 0)
									continue;

								xCarry = 0;
								yCarry = 0;

								FixRange(x + j, y + i, currentSubmap.SizeX, currentSubmap.SizeY, ref xCarry, ref yCarry, true);

								// currentSubmap.CurrentMap.Height[(x + xCarry + j) + ((y + yCarry + i) * currentSubmap.CurrentMap.sizeX)] < 80
								if (currentSubmap.CurrentMap.Height[(x + xCarry + j) + ((y + yCarry + i) * currentSubmap.CurrentMap.sizeX)] < Config.LandCutoff) { // was < 50
									int spreadChance = currentSubmap.CurrentMap.WaterSpread[(x + xCarry + j) + ((y + yCarry + i) * currentSubmap.CurrentMap.sizeX),
									   (i + 1),
									   (j + 1)];

									int outcome = random.Next(spreadChance, 164);
									if (outcome > 154) {
										spreaderHeight = currentSubmap.CurrentMap.Height[(x + xCarry + j) + ((y + yCarry + i) * currentSubmap.SizeX)];
										otherHeight = currentSubmap.OtherMap.Height[x + y * currentSubmap.SizeX];
										newHeight = Math.Max(Math.Min(79, Average(otherHeight, spreaderHeight)), 0);

										currentSubmap.OtherMap.Height[x + y * currentSubmap.CurrentMap.sizeX] = (byte)newHeight;
										currentSubmap.OtherMap.SetWaterSpread(currentSubmap.CurrentMap, (x + xCarry + j) + ((y + yCarry + i) * currentSubmap.SizeX), x + (y * currentSubmap.SizeX));
									}
								}
							}
						}
					}
				}

				waitThread[threadID] = true;
			}
		}

		public void Paint(byte height) {
			ref SubMap currentSubmap = ref CurrentSubmap;

			Console.WriteLine("Size X :: " + currentSubmap.SizeX);
			Console.WriteLine("Size Y :: " + currentSubmap.SizeY);

			int k = 0;

			for (int i = 0; i < currentSubmap.SizeY; i++) {
				for (int j = 0; j < currentSubmap.SizeX; j++) {
					currentSubmap.OtherMap[j + (i * currentSubmap.SizeX)] = height;
					k++;
				}
			}

			Console.WriteLine("Count Expected :: " + currentSubmap.SizeX * currentSubmap.SizeY);
			Console.WriteLine("Count Found    :: " + k);

			currentSubmap.FlipMap();
		}

		public void FillMapOcean() {
			int xCarry;
			int yCarry;

			int unfilledCells = 1;
			int spreaderHeight = 0;
			int otherHeight = 0;
			int newHeight = 0;

			ref SubMap currentSubmap = ref CurrentSubmap;

			Console.WriteLine("Starting Ocean Value Fill...");
			while (unfilledCells > 0) {
				unfilledCells = 0;

				PlaceAllSeeds(currentSubmap.seedQueueWater);

				for (int y = 0; y < currentSubmap.SizeY; y++) {
					for (int x = 0; x < currentSubmap.SizeX; x++) {
						//currentSubmap.CurrentMap.Height[x + y * currentSubmap.SizeX] < 80
						if (currentSubmap.CurrentMap[x + y * currentSubmap.SizeX] > Config.LandCutoff - 1)
							unfilledCells++;

						// Check surrounding square
						for (int i = -1; i < 2; i++) {
							for (int j = -1; j < 2; j++) {
								if (i == 0 && j == 0)
									continue;

								xCarry = 0;
								yCarry = 0;

								FixRange(x + j, y + i, currentSubmap.SizeX, currentSubmap.SizeY, ref xCarry, ref yCarry, true);

								// currentSubmap.CurrentMap.Height[(x + xCarry + j) + ((y + yCarry + i) * currentSubmap.CurrentMap.sizeX)] < 80
								if (currentSubmap.CurrentMap.Height[(x + xCarry + j) + ((y + yCarry + i) * currentSubmap.CurrentMap.sizeX)] < Config.LandCutoff) { // was < 50
									int spreadChance = currentSubmap.CurrentMap.WaterSpread[(x + xCarry + j) + ((y + yCarry + i) * currentSubmap.CurrentMap.sizeX),
									   (i + 1),
									   (j + 1)];

									// Determines the upper threshold of the spread chance. Changes based on how far from the equator y is
									double spreadChanceBalance = 4;
									double mult = Math.Pow((Math.Abs(((double)y / (double)currentSubmap.CurrentMap.sizeY) - 0.5) * 2.0), 2.0);
									spreadChanceBalance *= mult;

									int outcome = random.Next(spreadChance, 256);
									if (outcome > 245) {
										spreaderHeight = currentSubmap.CurrentMap.Height[(x + xCarry + j) + ((y + yCarry + i) * currentSubmap.SizeX)];
										otherHeight = currentSubmap.OtherMap.Height[x + y * currentSubmap.SizeX];
										newHeight = Math.Max(Math.Min(Config.LandCutoff - 1, Average(otherHeight, spreaderHeight)), 0);

										currentSubmap.OtherMap.Height[x + y * currentSubmap.CurrentMap.sizeX] = (byte)newHeight;
										currentSubmap.OtherMap.SetWaterSpread(currentSubmap.CurrentMap, (x + xCarry + j) + ((y + yCarry + i) * currentSubmap.SizeX), x + (y * currentSubmap.SizeX));
									}
								}
							}
						}
					}
				}

				currentSubmap.FlipMap();
			}
			
			Console.WriteLine("Ocean Fill Complete!");
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
						break;
					case ThreadInstructions.FUNC_BIOME_SPREAD_STD:
						//Console.WriteLine("[" + threadID + "] :: Spreading Water");
						waitThread[threadID] = true;
						SpreadBiome(threadID);
						break;
					case ThreadInstructions.FUNC_BIOME_AVERAGE_STD:
						//Console.WriteLine("[" + threadID + "] :: Spreading Water");
						waitThread[threadID] = true;
						while (waitThread[threadID])
							Thread.Sleep(50);

						AverageBiome(threadID);
						waitThread[threadID] = true;
						break;
					case ThreadInstructions.FUNC_ERODE:
						//Console.WriteLine("[" + threadID + "] :: Spreading Water");
						waitThread[threadID] = true;
						Erode(threadInstructions[threadID].spreadIterations, ref height, 0.001, threadInstructions[threadID].maxDistance, threadInstructions[threadID].pickupRange, threadInstructions[threadID].depositRange);
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

		public void SpreadMap(int iterations, int landSpreads, int waterSpreads, int inThreadCount) {
			int seedIndex;
			int seedSpawnDivider = 1;
			
			int threadCount = Math.Min(inThreadCount, threads.Length);
			ref SubMap currentSubmap = ref CurrentSubmap;

			for (int it = 0; it < iterations; it++) {
				// --------------------------- Land Spreading ---------------------------

				for (int t = 0; t < threadCount; t++) {
					int threadID = t;
					int xStep = currentSubmap.SizeX / threadCount;
					int xStart = xStep * t;
					int xEnd = (t == threadCount - 1) ? currentSubmap.SizeX - 1 : xStep * (t + 1) - 1;
					int spreadIterations = landSpreads;

					threadInstructions[t].function = ThreadInstructions.FUNC_LAND_SPREAD_STD;
					threadInstructions[t].seed = random.Next();
					threadInstructions[t].spreadIterations = spreadIterations;
					threadInstructions[t].startY = 0;
					threadInstructions[t].endY = currentSubmap.SizeY - 1;
					threadInstructions[t].startX = xStart;
					threadInstructions[t].endX = xEnd;

					waitThread[t] = false;
				}

				for (int t = 0; t < threadCount; t++)
					while (!waitThread[t])
						Thread.Sleep(20);

				for (int ls = 0; ls < landSpreads; ls++) {
					// Wait for all threads to complete

					for (int s = 0; s < currentSubmap.seedQueueLand.Count; s++) {
						if (currentSubmap.seedQueueLand[s].Count > 0) {
							if (random.Next(2) == 0) {
								int seedCount = (currentSubmap.seedQueueLand[s].Count / iterations) / seedSpawnDivider;
								for (int i = 0; i < seedCount; i++) {
									if (currentSubmap.seedQueueLand[s].Count > 0) {
										seedIndex = random.Next(0, currentSubmap.seedQueueLand[s].Count);
										PlaceSeed(currentSubmap.seedQueueLand[s][seedIndex]);
										currentSubmap.seedQueueLand[s].RemoveAt(seedIndex);
									}
								}
							}
						}
					}

					// Allow all threads to proceed
					for (int t = 0; t < threadCount; t++)
						waitThread[t] = false;

					// Wait for threads to finish processing
					for (int t = 0; t < threadCount; t++)
						while (!waitThread[t])
							Thread.Sleep(20);

					currentSubmap.FlipMap();
				}

				for (int t = 0; t < threadCount; t++)
					while (!waitThread[t])
						Thread.Sleep(20);

				//Console.WriteLine("End Sync");

				// --------------------------- Water Spreading ---------------------------

				for (int t = 0; t < threadCount; t++) {
					int threadID = t;
					int xStep = currentSubmap.SizeX / threadCount;
					int xStart = xStep * t;
					int xEnd = (t == threadCount - 1) ? currentSubmap.SizeX - 1 : xStep * (t + 1) - 1;
					int spreadIterations = waterSpreads;

					threadInstructions[t].function = ThreadInstructions.FUNC_OCEAN_SPREAD_STD;
					threadInstructions[t].spreadIterations = spreadIterations;
					threadInstructions[t].seed = random.Next();
					threadInstructions[t].startY = 0;
					threadInstructions[t].endY = currentSubmap.SizeY - 1;
					threadInstructions[t].startX = xStart;
					threadInstructions[t].endX = xEnd;

					waitThread[t] = false;
				}

				for (int t = 0; t < threadCount; t++)
					while (!waitThread[t])
						Thread.Sleep(20);

				for (int ws = 0; ws < waterSpreads; ws++) {
					// Allow all threads to proceed
					for (int t = 0; t < threadCount; t++)
						waitThread[t] = false;

					// Wait for threads to finish processing
					for (int t = 0; t < threadCount; t++)
						while (!waitThread[t])
							Thread.Sleep(20);

					currentSubmap.FlipMap();
				}

				for (int t = 0; t < threadCount; t++)
					while (!waitThread[t])
						Thread.Sleep(20);
			}
		}

		public void AddSubmap() {
			submapCount++;
			Array.Resize(ref subMaps, subMaps.Length + 1);

			subMaps[subMaps.Length - 1] = new SubMap(sizeY, sizeX);
		}

		public void ResetSubmap(int index) {
			subMaps[index] = new SubMap(sizeY, sizeX);
		}

		public void AddSubmap(Map map) {
			submapCount++;
			Array.Resize(ref subMaps, subMaps.Length + 1);

			subMaps[subMaps.Length - 1] = new SubMap(sizeY, sizeX);
			subMaps[subMaps.Length - 1][0].Set(map);
			subMaps[subMaps.Length - 1][1].Set(map);
		}

		public void WorkOn(int mapIndex) {
			currentSubmapIndex = mapIndex;
		}

		public void SetMap(int mapIndex, Map map) {
			subMaps[mapIndex][0].Set(map);
			subMaps[mapIndex][1].Set(map);
		}

		public ref SubMap CurrentSubmap {
			get { return ref subMaps[currentSubmapIndex]; }
		}

		public void Enlarge(int scale) {
			foreach (SubMap submap in subMaps) {
				submap.Enlarge(scale);
			}

			for (int i = 0; i < seedSpawnsX.Count; i++) {
				seedSpawnsX[i] *= scale;
				seedSpawnsY[i] *= scale;
			}

			sizeX *= scale;
			sizeY *= scale;
		}

		public void CurveTop() {
			CurrentSubmap[0].CurveTop();
			CurrentSubmap[1].CurveTop();
		}

		public void Curve() {
			subMaps[currentSubmapIndex][0].Curve();
			subMaps[currentSubmapIndex][1].Curve();
		}

		public void SeedBiomes() {

		}

		public void MergeLand(int map_0, int map_1, bool clearAfterMerge) {

			if (clearAfterMerge)
				ResetSubmap(map_1);
		}

		public void GenerateIce(int maxPoleDistance) {
			ref SubMap currentSubmap = ref CurrentSubmap;
			for (int y = 0; y < maxPoleDistance; y++) {
				for (int x = 0; x < CurrentSubmap.SizeX; x++) {

				}
			}
		}

		public void MergeFlat(int map_0, int map_1, bool clearAfterMerge) {
			subMaps[map_0][0].MergeFlat(subMaps[map_1][0]);
			subMaps[map_0][1].MergeFlat(subMaps[map_1][1]);

			if (clearAfterMerge)
				ResetSubmap(map_1);
		}

		public void MergeAdditive(int map_0, int map_1, bool clearAfterMerge) {
			subMaps[map_0][0].MergeAdditive(subMaps[map_1][0]);
			subMaps[map_0][1].MergeAdditive(subMaps[map_1][1]);

			if (clearAfterMerge)
				ResetSubmap(map_1);
		}

		public Seed GenerateSeed(byte inHeight, int posX, int posY, byte strengthBaseline, bool mutate) {
			Seed seed = new Seed {
				x = posX,
				y = posY,
				height = inHeight,
				spread = new byte[3, 3] { { 1, 1, 1 }, { 1, 1, 1 }, { 1, 1, 1 } },
			};


			int preferred = random.Next(0, 4);
			byte preferredChance = (byte)(random.Next(1, 5) * 4);

			if (preferred == 0) {
				seed.spread[0, 0] = preferredChance;
				seed.spread[2, 2] = preferredChance;
			}
			else if (preferred == 1) {
				seed.spread[0, 1] = preferredChance;
				seed.spread[2, 1] = preferredChance;
			}
			else if (preferred == 2) {
				seed.spread[0, 2] = preferredChance;
				seed.spread[2, 0] = preferredChance;
			}
			else if (preferred == 3) {
				seed.spread[1, 2] = preferredChance;
				seed.spread[1, 0] = preferredChance;
			}

			if (random.Next(0, 14) > 13 && mutate)
				seed.SetStrength((byte)(random.Next(1, 4) * 3));
			else
				seed.SetStrength(strengthBaseline);

			return seed;
		}

		public void PlaceSeed(Seed seed) {
			ref SubMap currentSubmap = ref CurrentSubmap;
			CurrentSubmap.CurrentMap.Height[seed.x + seed.y * CurrentSubmap.CurrentMap.sizeX] = seed.height;

			for (int i = 0; i < 3; i++) {
				for (int j = 0; j < 3; j++) {
					if (seed.height < Config.LandCutoff)
						currentSubmap.CurrentMap.WaterSpread[seed.x + seed.y * currentSubmap.CurrentMap.sizeX, i, j] = seed.spread[i, j];
					else
						currentSubmap.CurrentMap.LandSpread[seed.x + seed.y * currentSubmap.CurrentMap.sizeX, i, j] = seed.spread[i, j];
				}
			}
		}

		public void PlaceAllSeeds(List<List<Seed>> seedQueue) {
			ref SubMap currentSubmap = ref CurrentSubmap;

			for (int i = 0; i < seedQueue.Count; i++) {
				for (int seedID = 0; seedID < seedQueue[i].Count; seedID++) {
					PlaceSeed(seedQueue[i][seedID]);
				}
			}
		}

		public void SpawnSeeds(byte height, int parentX, int parentY, int seedCount, List<List<Seed>> seedQueueOutput, double lastDirectionAngle, List<vec2i> currentSeeds, bool diagonal) {
			List<Seed> seedQueue = new List<Seed>();

			bool allowLastDirection = (random.Next(0, 3) == 0);

			//int direction = random.Next(0, 3);
			// Picks between the 8 45 degree angles in a circle
			double directionAngle;
			double currentAngle;

			double directionCount = 16;

			directionAngle = random.Next((int)directionCount) * (360.0 / directionCount);

			if (!allowLastDirection && directionAngle == lastDirectionAngle)
				while (lastDirectionAngle == directionAngle)
					directionAngle = random.Next((int)directionCount) * (360.0 / directionCount);

			//if (diagonal)
			//	directionAngle += 45;

			int rangeMax = 7;
			int rangeMin = 1;
			//int rangeAverage = 2;
			int angleRange = 35;
			int deltaAngle = random.Next(5);

			int x = parentX;
			int y = parentY;
			double distance;

			int lastX = x;
			int lastY = y;
			int xCarry = 0;
			int yCarry = 0;
			
			Seed lastSeed = Seed.Default;
			ref SubMap currentSubmap = ref CurrentSubmap;

			for (int k = 0; k < seedCount; k++) {
				if (random.Next(0, 5) > 3)
					SpawnSeeds(height, lastX, lastY, random.Next((int)((seedCount - k) * 0.7), (int)((seedCount - k) * 1.0)), seedQueueOutput, directionAngle, currentSeeds, diagonal);

				xCarry = 0;
				yCarry = 0;

				currentAngle = directionAngle + (random.Next(-angleRange, angleRange));
				distance = random.Next(rangeMin, rangeMax);
				x = lastX + (int)(distance * Math.Cos(ToRadians(currentAngle)));
				y = lastY + (int)(distance * Math.Sin(ToRadians(currentAngle)));

				//x = random.Next(minX, maxX);
				//y = random.Next(minY, maxY);

				FixRange(x, y, currentSubmap.SizeX, currentSubmap.SizeY, ref xCarry, ref yCarry, true);

				lastSeed = GenerateSeed(height, x + xCarry, y + yCarry, lastSeed.strength, true);
				seedQueue.Add(lastSeed);

				lastX = x + xCarry;
				lastY = y + yCarry;

				seedSpawnsX.Add(lastX);
				seedSpawnsY.Add(lastY);

				directionAngle += deltaAngle;
			}

			seedQueueOutput.Add(seedQueue);
		}

		public void SeedMapIsland(byte height, int amount, Range centreRange, List<List<Seed>> seedQueueOutput) {
			int x, y;

			vec2i centre = new vec2i(sizeX / 2, sizeY / 2);
			float thetaDeg;
			float distance;

			for (int a = 0; a < amount; a++) {
				thetaDeg = random.Next(360);

				distance = random.Next(centreRange.min, centreRange.max);

				double displacementX = (int)(Math.Cos(ToRadians(thetaDeg)) * distance);
				double displacementY = (int)(Math.Sin(ToRadians(thetaDeg)) * distance);

				vec2i point = centre + new vec2i((int)displacementX, (int)displacementY);
			}
		}

		public void SeedMap(vec2[] points) {

			List<Seed> currentSeeds = new List<Seed>();
			//currentSeeds.Add(GenerateSeed(Config.LandCutoff, (int)spline.Points[0].x, (int)spline.Points[0].y, Config.DefaultSeedStrength, true));
			//currentSeeds.Add(GenerateSeed(Config.LandCutoff, (int)spline.Points[3].x, (int)spline.Points[3].y, Config.DefaultSeedStrength, true));
			ref SubMap currentSubmap = ref CurrentSubmap;

			for (int i = 0; i < points.Length; i++) {
				vec2 position = points[i];

				int xCarry = 0;
				int yCarry = 0;
				FixRange((int)position.x, (int)position.y, currentSubmap.SizeX, currentSubmap.SizeY, ref xCarry, ref yCarry, false);

				currentSeeds.Add(GenerateSeed(Config.LandCutoff, (int)position.x + xCarry, (int)position.y + yCarry, Config.DefaultSeedStrength, true));
			}

			foreach (Seed seed in currentSeeds) {
				PlaceSeed(seed);
			}
		}

		public void SeedMap(SplineBezier spline, int amount) {

			List<Seed> currentSeeds = new List<Seed>();
			//currentSeeds.Add(GenerateSeed(Config.LandCutoff, (int)spline.Points[0].x, (int)spline.Points[0].y, Config.DefaultSeedStrength, true));
			//currentSeeds.Add(GenerateSeed(Config.LandCutoff, (int)spline.Points[3].x, (int)spline.Points[3].y, Config.DefaultSeedStrength, true));

			
			double step = 1.0 / amount;
			for (double i = 0.0; i < amount; i += 1.0) {
				vec2 position = spline[i * step];

				currentSeeds.Add(GenerateSeed(Config.LandCutoff, (int)position.x, (int)position.y, Config.DefaultSeedStrength, true));
			}
			
			foreach (Seed seed in currentSeeds) {
				PlaceSeed(seed);
			}
		}

		public void SeedMapSpread(byte height, int amount, Range southSeedRange, List<List<Seed>> seedQueueOutput, int globalOriginCount, int localOriginCount, RangeD globalSeedRange, RangeD localSeedRange, int maxSeedLength, bool debug = false) {
			int x = 0;
			int y = 0;

			ref SubMap currentSubmap = ref CurrentSubmap;
			List<vec2i> currentSeeds = new List<vec2i>();

			ivec2<RangeD> globalRange = new ivec2<RangeD>(
				new RangeD(currentSubmap.SizeX * globalSeedRange.min, currentSubmap.SizeX * globalSeedRange.max),
				new RangeD(currentSubmap.SizeY * globalSeedRange.min, currentSubmap.SizeY * globalSeedRange.max));

			ivec2<RangeD> localRange = new ivec2<RangeD>(
				new RangeD(currentSubmap.SizeX * localSeedRange.min, currentSubmap.SizeX * localSeedRange.max),
				new RangeD(currentSubmap.SizeY * localSeedRange.min, currentSubmap.SizeY * localSeedRange.max));

			int parentX = 0;
			int parentY = 0;

			int xCarry = 0;
			int yCarry = 0;
			int southPositionMin = 2 * (currentSubmap.SizeY / 4);
			int southPositionMax = 3 * (currentSubmap.SizeY / 4);

			int southSeedCount = 0;
			int parentID;
			
			bool foundCorrectLatitude;

			for (int a = 0; a < amount; a++) {
				foundCorrectLatitude = false;

				while (!foundCorrectLatitude) {
					// In most cases, place the new seed in a set range around an old seed
					if (seedSpawnsX.Count > (globalOriginCount)) {
						xCarry = 0;
						yCarry = 0;

						double displacementDistanceX = globalRange.x.min + (random.NextDouble() * globalRange.x.max);
						double displacementDistanceY = globalRange.y.min + (random.NextDouble() * globalRange.y.max);

						double displacementAngle = random.NextDouble() * 360.0;

						double displacementX = (int)(Math.Cos(ToRadians(displacementAngle)) * displacementDistanceX);
						double displacementY = (int)(Math.Sin(ToRadians(displacementAngle)) * displacementDistanceY);

						if (currentSeeds.Count > localOriginCount) {
							parentID = random.Next(currentSeeds.Count);
							parentX = currentSeeds[parentID].x;
							parentY = currentSeeds[parentID].y;
						}
						else {
							parentID = random.Next(seedSpawnsX.Count);
							parentX = seedSpawnsX[parentID];
							parentY = seedSpawnsY[parentID];
						}

						int newX = (int)(parentX + displacementX);
						int newY = (int)(parentY + displacementY);

						x = newX;
						y = newY;

						FixRange(x, y, currentSubmap.SizeX, currentSubmap.SizeY, ref xCarry, ref yCarry, false);
					}
					// If there aren't enough seeds to compare to yet, spawn the seed randomly
					else {
						x = random.Next(Config.MapBorderWidth, currentSubmap.SizeX - Config.MapBorderWidth);
						y = random.Next(Config.MapBorderWidth, currentSubmap.SizeY - Config.MapBorderWidth);
					}

					// No seeds can spawn in the lowest 1/4 of the map
					if (y < southPositionMax) {
						// If we NEED more southern seeds
						if (southSeedCount < southSeedRange.min) {
							// If this is a southern seed
							if (y > southPositionMin) {
								southSeedCount++;
								foundCorrectLatitude = true;
							}
							// If this is a northern seed
							else {

							}
						}
						// If we CAN have more southern seeds
						else if (southSeedCount < southSeedRange.max) {
							// If this IS a southern seed
							if (y > southPositionMin) {
								southSeedCount++;
								foundCorrectLatitude = true;
							}
							// If this is a northern seed
							else {
								foundCorrectLatitude = true;
							}
						}
						// If we CANNOT have any more southern seeds
						else {
							// If this IS a southern seed
							if (y > southPositionMin) {

							}
							// If this is a northern seed
							else {
								southSeedCount++;
								foundCorrectLatitude = true;
							}
						}
					}
				}

				if (debug) {
					Console.WriteLine("Start X :: " + x);
					Console.WriteLine("Start Y :: " + y);
				}

				Seed seed = GenerateSeed(height, x + xCarry, y + yCarry, Config.DefaultSeedStrength, true);

				//Console.WriteLine("Placing Seed... [" + (x + xCarry) + ", " + (y + yCarry) + "]");
				PlaceSeed(seed);
				//Console.WriteLine("Placed Seed");

				if (random.Next(2) > -1) {

					int seedCount = 0;

					if (random.Next(6) > 1)
						seedCount = random.Next(6, maxSeedLength);
					else
						seedCount = random.Next(4, 12);

					int seedCount2 = Config.SeedSpreadRanges.RandomValue(random);

					int diagonalVal = random.Next(0, 2);
					bool diagonal = (diagonalVal == 0);

					SpawnSeeds(height, x + xCarry, y + yCarry, seedCount, seedQueueOutput, random.Next(0, 3), currentSeeds, diagonal);
				}
			}
		}

		public void SeedMap(byte height, int amount) {
			int x, y;
			int border = 25;
			ref SubMap currentSubmap = ref CurrentSubmap;

			for (int a = 0; a < amount; a++) {
				x = random.Next(border, currentSubmap.SizeX - border);

				if (random.Next(0, 5) > 1)
					y = random.Next(border, currentSubmap.SizeY / 3);
				else
					y = random.Next(border, currentSubmap.SizeY - border);

				PlaceSeed(GenerateSeed(height, x, y, Config.DefaultSeedStrength, true));
			}
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

		public double ToRadians(double angle) {
			return angle * (Math.PI / 180);
		}

		public double ToDegrees(double angle) {
			return angle * (180 / Math.PI);
		}

		public Seed GenerateSeed_OLD(int posX, int posY) {
			Seed seed = new Seed {
				x = posX,
				y = posY,
				height = 100,
				spread = new byte[3, 3] { { 1, 1, 1 }, { 1, 1, 1 }, { 1, 1, 1 } },
			};


			int preferred = random.Next(0, 3);
			byte preferredChance = 20;

			if (preferred == 0) {
				seed.spread[0, 0] = preferredChance;
				seed.spread[2, 2] = preferredChance;
			}
			else if (preferred == 1) {
				seed.spread[0, 1] = preferredChance;
				seed.spread[2, 1] = preferredChance;
			}
			else if (preferred == 2) {
				seed.spread[0, 2] = preferredChance;
				seed.spread[2, 0] = preferredChance;
			}
			else if (preferred == 3) {
				seed.spread[1, 2] = preferredChance;
				seed.spread[1, 0] = preferredChance;
			}

			return seed;
		}
		
		public void SpreadMap_OLD(int iterations, int landSpreads) {
			Console.WriteLine("Started");
			int seedIndex;
			int seedSpawnDivider = 1;
			int xCarry;
			int yCarry;

			ref SubMap currentSubmap = ref CurrentSubmap;

			//currentSubmap.OtherMap.Set(currentSubmap.CurrentMap);

			for (int it = 0; it < iterations; it++) {
				for (int ls = 0; ls < landSpreads; ls++) {
					// Spawn a few seeds from the backlog
					for (int s = 0; s < currentSubmap.seedQueueLand.Count; s++) {
						if (currentSubmap.seedQueueLand[s].Count > 0) {
							if (random.Next(2) == 0) {
								int seedCount = (currentSubmap.seedQueueLand[s].Count / iterations) / seedSpawnDivider;
								for (int i = 0; i < seedCount; i++) {
									if (currentSubmap.seedQueueLand[s].Count > 0) {
										seedIndex = random.Next(0, currentSubmap.seedQueueLand[s].Count - 1);
										PlaceSeed(currentSubmap.seedQueueLand[s][seedIndex]);
										currentSubmap.seedQueueLand[s].RemoveAt(seedIndex);
									}
								}
							}
						}
					}

					for (int y = 0; y < currentSubmap.CurrentMap.sizeY; y++) {
						for (int x = 0; x < currentSubmap.CurrentMap.sizeX; x++) {
							if (currentSubmap.CurrentMap[x + y * currentSubmap.CurrentMap.sizeX] > Config.LandCutoff) {
								currentSubmap.OtherMap.Copy(currentSubmap.CurrentMap, x + y * currentSubmap.CurrentMap.sizeX, x + y * currentSubmap.CurrentMap.sizeX);
								continue;
							}

							// Check surrounding square
							for (int i = -1; i < 2; i++) {
								for (int j = -1; j < 2; j++) {
									if (i == 0 && j == 0)
										continue;

									xCarry = 0;
									yCarry = 0;

									// If the square goes over the top of the map, move 50% to the right and read down
									if (y + i < 0) {
										xCarry += currentSubmap.CurrentMap.sizeX;
										yCarry += 2;
									}

									// If the square goes under the bottom of the map, move 50% to the right and read up
									if (y + i > currentSubmap.CurrentMap.sizeY - 1) {
										xCarry += currentSubmap.CurrentMap.sizeX;
										yCarry -= 2;
									}

									// If the square goes off the left side of the map, start reading from the right
									if (x + j < 0)
										xCarry += currentSubmap.CurrentMap.sizeX;

									// If the square goes off the right side of the map, start reading from the left
									if (x + j > currentSubmap.CurrentMap.sizeX - 1)
										xCarry += -currentSubmap.CurrentMap.sizeX;

									if (x + xCarry + j > currentSubmap.CurrentMap.sizeX - 1)
										xCarry -= currentSubmap.CurrentMap.sizeX;

									if (x + xCarry + j < 0)
										xCarry += currentSubmap.CurrentMap.sizeX;

									if (currentSubmap.CurrentMap[(x + xCarry + j) + ((y + yCarry + i) * currentSubmap.CurrentMap.sizeX)] > Config.LandCutoff) {
										int spreadChance = currentSubmap.CurrentMap.LandSpread[(x + xCarry + j) + ((y + yCarry + i) * currentSubmap.CurrentMap.sizeX),
										   (i + 1),
										   (j + 1)];

										// Determines the upper threshold of the spread chance. Changes based on how far from the equator y is
										double spreadChanceBalance = 4;
										double mult = Math.Pow((Math.Abs(((double)y / (double)currentSubmap.CurrentMap.sizeY) - 0.5) * 2.0), 2.0);
										spreadChanceBalance *= mult;

										int outcome = random.Next(spreadChance, 22);
										if (outcome > 20) {
											currentSubmap.OtherMap[x + y * currentSubmap.CurrentMap.sizeX] = 100;
											currentSubmap.OtherMap.SetLandSpread((x + xCarry + j) + ((y + yCarry + i) * currentSubmap.CurrentMap.sizeX), x + y * currentSubmap.CurrentMap.sizeX);
										}
									}
								}
							}
						}
					}

					currentSubmap.FlipMap();
				}

			}

			Console.WriteLine("Ended");
		}
	}
}
