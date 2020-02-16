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
    public partial class MainWindow : Window {
        Random random;

        [ThreadStatic]
        public static Bitmap mapImage;

		MapRenderer renderer;
		Thread flatThread;
		bool closed;

		GenMap genMap;
		bool genMapCreated;

		public MainWindow() {
            random = new Random();
			closed = false;
            InitializeComponent();

			renderer = new MapRenderer(this);
			renderer.Start();
		}

		// The generator is intended to be very flexible in how it's used to generate worlds
		// Therefore, it works by creating a map, and then performing whichever functions the user needs in whichever order the user needs
		// This is probably the clunkiest part of the program, but it's a necessary downside for the extreme modularity and flexibility of the generator
		public void GenerateAdvancedMap() {
			Console.WriteLine("Starting Generation");
			int mapHeight = 135;
			int mapWidth = 240;

			Console.WriteLine("Starting Renderer");
			int time = (int)(DateTime.Now.ToBinary() % int.MaxValue);
			GeneratorAdvanced generator = new GeneratorAdvanced(mapWidth, mapHeight, 12, time);
			renderer.AttachGenerator(ref generator);

			generator.WorkOn(0);

			// Generate the hardness map
			// This dictates the bedrock hardness - combined with soil hardness (generated per turn from existing land height) it helps dictate how easy it is for land to spread
			Console.WriteLine("Generating Functional Maps");
			generator.GenerateGroundHardness(3, 3, 6, 6, 3, 0.3f, 0.1f, 0.2f);

			// Generate the background noise map
			// It isn't currently used, but it just was used for critical functions and I have plans to use it again
			//generator.GenerateTectonicPlates(2, 1, 7, 3);
			generator.GenerateNoise(1.0f * 2.0f, 0.5625f * 2.0f, 2, 1.0f);
			// Debug code - I want to see what the noise map looks like
			DisplayImage(ImageCreator.CreateImageFromMap(((BaseMap<float>)generator.FunctionMaps[GenMap.MAP_NOISE_0]).Data, generator.sizeX, generator.sizeY, 255.0f));

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
			Console.WriteLine("Generating Oceans oceans");
			generator.FillMapWater();

			Console.WriteLine("Generating mountain seeds");
			generator.GenerateSeedBranches(new Range(quarterMountain, 255), new Range(Config.LandCutoff, 255), -1, 3, 4, new Range(3, 6),  new Range(0, 1), new vec2i(4, 6), mountainGlobalSeedRange, mountainLocalSeedRange, ref generator.CurrentSubmap.branchQueueMnt, 36, 4);
			generator.SpreadMap(1, 28, 0, 4);
			generator.SpreadMap(1, 0, 8, 4);
			generator.Curve();
			generator.SpreadMap(1, 0, 4, 4);

			generator.AddSubmap(ref generator.CurrentSubmap);

			// Generate land on separate maps
			// Allows new continents without creating huge blobs, as these spawns are uninfluenced by the original spawns
			generator.WorkOn(1);
			generator.FillMapWater();
			generator.GenerateSeedBranches(new Range(quarterMountain, 255), new Range(Config.LandCutoff, 255), -1, 3, 4, new Range(0, 0), new Range(3, 6), new vec2i(2, 6), mountainGlobalSeedRange, mountainLocalSeedRange, ref generator.CurrentSubmap.branchQueueMnt, 32, 6);
			generator.SpreadMap(1, 26, 0, 4);
			generator.SpreadMap(1, 0, 8, 4);
			generator.Curve();
			generator.WorkOn(0);
			generator.MergeLand(0, 1, false);

			// Same as above. For each of these spawns, we generate slighly less land. Smaller and smaller islands/continent outlines.
			// This smallest spawn isn't to create continents; it's to add minute details.
			generator.WorkOn(1);
			generator.FillMapWater();
			generator.GenerateSeedBranches(new Range(quarterMountain, 255), new Range(Config.LandCutoff, 255), -1, 3, 3, new Range(0, 0),  new Range(2, 4), new vec2i(2, 4), mountainGlobalSeedRange, mountainLocalSeedRange, ref generator.CurrentSubmap.branchQueueMnt, 24, 8);
			generator.SpreadMap(1, 16, 0, 4);
			generator.SpreadMap(1, 0, 4, 4);
			generator.WorkOn(0);
			generator.MergeLand(0, 1, false);

			generator.SpreadMap(1, 8, 0, 4);
			generator.SpreadMap(1, 0, 2, 4);
			
			// From here on out, we try to get a higher resolution map
			// Increase map size, refine so it's not blocky, repeat
			// This allows great-looking fractal terrain because we use the same spreading formula
			// i.e, we spread huge tendrils, then smaller ones from that, then smaller ones from that.
			// Each iteration we both add and remove land, but only removing a bit
			// This way, we retain the realistic aspects of fractal terrain, but withhout it being overly obvious to the human eye

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
			// We no longer need the second submap
			generator.RemoveSubmap();

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

			//renderer.Kill();
			Console.WriteLine("Ending timer...");
			renderer.DetachGeneratorAndWait();
			//renderer.Join();

			//generator.GenerateBiomes();
			//DisplayImage(generator.CurrentMap, true);

			Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
			genMap = generator.CurrentSubmap;
			genMapCreated = true;

			watch.Stop();
			long elapsedMs = watch.ElapsedMilliseconds;
			Console.WriteLine("Ended Timer!");
		}

		public void DisplayImage(Bitmap image) {
			mapImage = image;
			BitmapImage bmpImage = ImageCreator.ToBitmapImage(image);

			if (!closed) {
				mapDisplay.Dispatcher.Invoke((Action)delegate {
					mapDisplay.Source = bmpImage;
				});
			}
		}

		public void SaveImage(String name) {
            mapImage.Save("C:\\Maps\\" + name + ".png");
		}

		public void SaveImage() {
			SaveImage("tmp");
		}

        private void ButtonRegenerate(object sender, RoutedEventArgs e) {
            mapImage = null;
            random = new Random();

            flatThread = new Thread(() => GenerateAdvancedMap());
            flatThread.Start();
        }

        private void ButtonSave(object sender, RoutedEventArgs e) {
            SaveImage();
		}

		private void Window_Closed(object sender, EventArgs e) {
			closed = true;
			renderer.Kill();
			renderer.Join();
		}

		private void ButtonShowHeightmap(object sender, RoutedEventArgs e) {
			if (genMap != null) {
				Bitmap image = ImageCreator.CreateImageFromMap(genMap.CurrentMap);
				DisplayImage(image);
			}
		}

		private void ButtonShowBedrockHardness(object sender, RoutedEventArgs e) {
			if (genMap != null) {
				float[] hardnessMap = ((BaseMap<float>)genMap.function_maps[GenMap.MAP_BEDROCK_HARDNESS]).Data;
				Bitmap image = ImageCreator.CreateImageFromMap(hardnessMap, genMap.SizeX, genMap.SizeY, 255.0f);
				DisplayImage(image);
			}
		}

		private void ButtonShowSoilHardness(object sender, RoutedEventArgs e) {
			if (genMap != null) {
				float[] hardnessMap = ((BaseMap<float>)genMap.function_maps[GenMap.MAP_SOIL_HARDNESS]).Data;
				Bitmap image = ImageCreator.CreateImageFromMap(hardnessMap, genMap.SizeX, genMap.SizeY, 255.0f);
				DisplayImage(image);
			}
		}

		private void ButtonShowSpreadDirection(object sender, RoutedEventArgs e) {

		}

		private void ButtonShowNoise(object sender, RoutedEventArgs e) {
			if (genMap != null) {
				float[] noiseMap = ((BaseMap<float>)genMap.function_maps[GenMap.MAP_NOISE_0]).Data;
				Bitmap image = ImageCreator.CreateImageFromMap(noiseMap, genMap.SizeX, genMap.SizeY, 255.0f);
				DisplayImage(image);
			}
		}

		private void ButtonShowBiomes(object sender, RoutedEventArgs e) {

		}
	}
}