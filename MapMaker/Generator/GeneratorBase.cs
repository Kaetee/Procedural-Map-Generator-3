using System;
using System.Threading;

namespace MapMaker {
	public class GeneratorBase {
		protected Random random;
		public int sizeX, sizeY;

		// The threads to be used in generation
		// One ThreadInstructions per thread
		// Each thread gets told what to do through the associated instructions. Each thread stores its ID
		// One waitThread per thread, too. Tells the thread whether it shhould proceed or pause
		protected Thread[] threads;
		protected ThreadInstructions[] threadInstructions;
		protected bool[] waitThread;
	}
}
