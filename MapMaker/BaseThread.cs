using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MapMaker {
	// C# doesn't seem to let me extend the Thread class (it's a sealed class)
	// This is a simple class that acts as a wrapper for it instead
	// Takes th functions I need from Thread and lets me use them as if it was Thread (by storing a thread instance and simply calling its equivalent functions)
	// But also lets me extend it for my renderer
	abstract class AThread {
		private Thread thread;

		protected AThread() {
			thread = new Thread(new ThreadStart(Run));
		}

		public abstract void Run();

		public void Start() => thread.Start();
		public void Join() => thread.Join();
	}
}
