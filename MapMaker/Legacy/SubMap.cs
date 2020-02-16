using System.Collections.Generic;

namespace MapMaker
{
	public struct SubMap {
		public Map[] maps;
		public int currentMapIndex;
		public List<List<Seed>> seedQueueLand;
		public List<List<Seed>> seedQueueWater;

		public SubMap(int sizeY, int sizeX) {
			maps = new Map[] { new Map(sizeY, sizeX), new Map(sizeY, sizeX) };
			currentMapIndex = 0;
			seedQueueLand = new List<List<Seed>>();
			seedQueueWater = new List<List<Seed>>();
		}

		public int SizeX {
			get { return maps[0].sizeX; }
		}

		public int SizeY {
			get { return maps[0].sizeY; }
		}

		public void Set(Map map) {
			maps[0].Set(map);
			maps[1].Set(map);
		}

		public int Flip(int index) {
			return (index == 0) ? 1 : 0;
		}

		public void FlipMap() {
			currentMapIndex = Flip(currentMapIndex);
		}

		public ref Map CurrentMap {
			get { return ref maps[currentMapIndex]; }
		}

		public ref Map OtherMap {
			get { return ref maps[Flip(currentMapIndex)]; }
		}

		public void Enlarge(int scale) {
			maps[0].Enlarge(scale);
			maps[1].Enlarge(scale);
		}

		public Map this[int index] {
			get { return maps[index]; }
		}
	}
}
