namespace MapMaker {
	// This struct lets us pass commands to threads. This way, we don't need to run different threads for different functions
	// (i.e., 8 land spreading threads, 8 biome threads, 8 water spreading threads, 8 erotion thread, etc)
	// Instead, we run just the number of threads we'll need and tell each thread what it has to work on per iteration
	public struct ThreadInstructions {
		public const int FUNC_KILL_THREAD = -1;
		public const int FUNC_WAIT = 0;
		public const int FUNC_LAND_SPREAD_STD = 1;
		public const int FUNC_OCEAN_SPREAD_STD = 2;
		public const int FUNC_BIOME_SPREAD_STD = 3;
		public const int FUNC_BIOME_AVERAGE_STD = 4;
		public const int FUNC_ERODE = 5;

		public ThreadInstructions(int inFunciton, int inSpreadCount, int inSeed, int inStartY, int inEndY, int inStartX, int inEndX, byte inBiomeID) {
			function = inFunciton;
			spreadIterations = inSpreadCount;
			seed = inSeed;
			startY = inStartY;
			endY = inEndY;
			startX = inStartX;
			endX = inEndX;
			biomeID = inBiomeID;
			maxDistance = 0;
			pickupRange = 0;
			depositRange = 0;
		}

		public int function;
		public int spreadIterations;
		public int seed;
		public int startY;
		public int endY;
		public int startX;
		public int endX;
		public byte biomeID;
		public int maxDistance;
		public int pickupRange;
		public int depositRange;
	}
}
