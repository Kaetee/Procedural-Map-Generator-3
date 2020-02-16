using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MapMaker {
	// A renderer thread. Runs and automatically updates an image with the contents of a generator instance.
	// One renderer per window.
	class MapRenderer : AThread {
		bool kill;
		bool pause;
		bool isPaused;

		bool hasGenerator;
		bool clearGenerator;

		GeneratorAdvanced generatorInstance;
		MainWindow target;
		BiomeMap map;

		public MapRenderer(MainWindow inTarget) {
			target = inTarget;
			Initialise();
		}

		void Initialise() {
			kill = false;
			pause = false;
			isPaused = false;
			hasGenerator = false;
			clearGenerator = false;
		}

		// The main run class. Handles anything run-related like pausing or shutting itself down
		// Delegates updating/rendering to appropriate methods
		public override void Run() {
			Console.WriteLine("MapRenderer.Run");

			while (!kill) {
				// Wait a bit before trying to spawn the next frame
				Thread.Sleep(50);

				if (!hasGenerator)
					continue;

				// Check for pause after update
				// We could check before, but then if the thread is killed without being unpaused,
				// A new update would be forced after the kill is issued.
				Update();

				if (pause) {
					isPaused = true;
					while (pause && !kill)
						Thread.Sleep(20);

					isPaused = pause;
				}

				if (clearGenerator)
					ClearGenerator();
			}
		}

		void LoadMap() {
			map = new BiomeMap(generatorInstance.sizeY, generatorInstance.sizeX);
		}

		// Attaches a generator instance to the renderer
		// Tells the Run function that it can now update with it
		public void AttachGenerator(ref GeneratorAdvanced generator) {
			generatorInstance = generator;
			hasGenerator = true;
		}

		// Removes the generator instance whenever the renderer is free to do so safely
		// i.e., now if the renderer is paused, or sets a flag to do so next iteration
		public void DetachGenerator() {
			if (kill || pause)
				ClearGenerator();
			else
				clearGenerator = true;
		}

		// Removes the generator class and updates flags so it doesn't attempt to be rendered from
		private void ClearGenerator() {
			generatorInstance = null;
			hasGenerator = false;
			clearGenerator = false;
		}

		// Detaches the generator, and then waits for it to get detached.
		// Used for thread closing and as a safety precaution when the user tries to generate a new map too quickly.
		public void DetachGeneratorAndWait() {
			DetachGenerator();

			while (hasGenerator)
				Thread.Sleep(20);
		}

		// Update anything that needs to be updated before rendering
		void Update() {
			if (generatorInstance != null) {
				if (generatorInstance.CurrentMap.CanRead()) {
					generatorInstance.CurrentMap.Lock();

					Render();

					generatorInstance.CurrentMap.Unlock();
				}
			}
		}

		// Render the map to the display image
		void Render() {
			map = generatorInstance.CurrentMap.Clone();
			target.DisplayImage(ImageCreator.CreateImageFromMap(map));
		}

		// Kills the renderer thread when it's free to do so
		public void Kill() => kill = true;

		// Pauses rendering
		public void Pause() => pause = true;

		// Resumes rendering
		public void Resume() => pause = false;
	}
}
