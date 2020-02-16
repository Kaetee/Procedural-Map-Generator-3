namespace MapMaker {
	public struct RangeCollectionElement {
		public int deltaThreshold;
		public Range range;

		public RangeCollectionElement(int newDeltaThreshold, int min, int max) {
			deltaThreshold = newDeltaThreshold;
			range = new Range(min, max);
		}
	}
}
