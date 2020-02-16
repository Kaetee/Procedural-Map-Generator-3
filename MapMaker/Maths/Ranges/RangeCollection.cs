using System;
using System.Collections.Generic;

namespace MapMaker {
	public struct RangeCollection {
		public Range range;
		public List<RangeCollectionElement> ranges;

		public Range GetFirstRangeFor(int value) {
			int totalThreshold = 0;

			for (int i = 0; i < ranges.Count; i++) {
				totalThreshold += ranges[i].deltaThreshold;

				if (value >= totalThreshold)
					return ranges[i].range;
			}

			return new Range { min = 0, max = 0 };
		}

		public int RandomValue(Random random) {
			Range selectedRange = GetFirstRangeFor(random.Next(range.min, range.max));
			return random.Next(selectedRange.min, selectedRange.max);
		}
	}
}
