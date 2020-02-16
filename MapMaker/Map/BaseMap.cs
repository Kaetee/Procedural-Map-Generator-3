using System;
using System.Collections.Generic;

namespace MapMaker {
	public class BaseMap<T> : ABasicMap {
		protected int sizeX;
		protected int sizeY;
		protected T[] data;

		// When the map size is being increased, we need to lock the main array (especially for multithreading)
		// If there are any calls that just want to read the last version while we're updating, we store it in a temporary location
		protected T[] tempData;
		bool locked_0;
		protected bool locked_1;
		int lastIndex = 0;

		public BaseMap() {
			sizeX = 0;
			sizeY = 0;
		}

		public BaseMap(int width, int height) {
			sizeX = width;
			sizeY = height;

			data = new T[sizeX * sizeY];
			tempData = new T[sizeX * sizeY];
			locked_0 = false;
			locked_1 = false;
		}

		public bool Lock() {
			if (locked_0)
				return false;

			locked_0 = true;
			return true;
		}

		public bool Unlock() {
			if (!locked_0)
				return false;

			locked_0 = false;
			return true;
		}

		public BaseMap(int inSizeX, int inSizeY, T startingValue) : this(inSizeX, inSizeY) {
			Set(startingValue);
		}

		public BaseMap(int inSizeX, int inSizeY, T[] inMetaData) : this(inSizeX, inSizeY) {
			data = new List<T>(inMetaData).ToArray();
		}

		public override Type Type { get { return typeof(T); } }

		public int SizeX => sizeX;

		public int SizeY => sizeY;

		public bool CanRead() { return !locked_0; }

		public void Set(T value) {
			for (int i = 0; i < data.Length; i++)
				data[i] = value;
		}

		public T this[int index] {
			get {
				if (locked_1)
					return tempData[index];

				lastIndex = index;
				return data[index];
			}

			set {
				if (!locked_1) {
					lastIndex = index;
					data[index] = value;
				}
			}
		}

		public T[] Data {
			get => data;
			set => data = value;
		}

		public void Copy(BaseMap<T> map, int from, int to) {
			data[to] = map[from];
		}

		public void Set(BaseMap<T> map) {
			sizeY = map.sizeY;
			sizeX = map.sizeX;
			data = (T[])map.data.Clone();
		}

		public override void Enlarge(int scale) {
			locked_1 = true;
			tempData = (T[])data.Clone();

			T[] newData = new T[(sizeX * scale) * (sizeY * scale)];

			for (int y = 0; y < sizeY; y++) {
				for (int x = 0; x < sizeX; x++) {
					for (int sx = 0; sx < scale; sx++) {
						for (int sy = 0; sy < scale; sy++) {
							newData[((x * scale) + sx) + (((y * scale) + sy) * (sizeX * scale))] = data[x + y * sizeX];
						}
					}
				}
			}

			data = newData;

			sizeX *= scale;
			sizeY *= scale;

			locked_1 = false;
		}

		public void Clear() {
			data = new T[sizeY * sizeX];
		}

		public BaseMap<T> Clone() {
			return new BaseMap<T>(sizeX, sizeY, data);
		}
	}
}
