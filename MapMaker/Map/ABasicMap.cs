using System;

namespace MapMaker {
	// Interface for BasicMap<T>
	// Allows the BasicMap<T> to be used in lists without knowing the type
	public abstract class ABasicMap {
		public abstract Type Type { get; }
		public abstract void Enlarge(int scale);
	}
}
