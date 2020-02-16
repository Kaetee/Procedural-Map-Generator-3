using System;
using System.Collections.Generic;

namespace MapMaker {
	// Recursive list
	public class SeedBranch<T> {
		HeightSeed<T> seed;
		List<SeedBranch<T>> subBranch;

		public SeedBranch() : this(new HeightSeed<T>(), new List<SeedBranch<T>>()) { }

		public SeedBranch(HeightSeed<T> inSeed) {
			seed = inSeed;
			subBranch = new List<SeedBranch<T>>();
		}

		public SeedBranch(HeightSeed<T> inSeed, List<SeedBranch<T>> inSubBranch) {
			seed = inSeed;
			subBranch = inSubBranch;
		}

		public HeightSeed<T> Seed {
			get => seed;
			set => seed = value;
		}

		public List<SeedBranch<T>> SubBranch {
			get => subBranch;
			set => subBranch = value;
		}

		public void TranslateBranch(vec2i displacement, vec2i mapSize) {
			int xCarry = 0;
			int yCarry = 0;

			FixRange(seed.x + displacement.x, seed.y + displacement.y, mapSize.x, mapSize.y, ref xCarry, ref yCarry, true);

			seed.x += displacement.x + xCarry;
			seed.y += displacement.y + yCarry;

			for (int i = 0; i < subBranch.Count; i++)
				subBranch[i].TranslateBranch(displacement, mapSize);
		}

		public List<vec2i> GetAllCoordinates() {
			List<vec2i> output = new List<vec2i>();

			output.Add(new vec2i(seed.x, seed.y));

			foreach (SeedBranch<T> sb in subBranch)
				output.AddRange(sb.GetAllCoordinates());

			return output;
		}

		public int GetTotalSize() {
			int size = 0;

			if (seed != null)
				size++;

			foreach (SeedBranch<T> branch in subBranch)
				size += branch.GetTotalSize();

			return size;
		}
		
		public void FixRange(int x, int y, int rangeX, int rangeY, ref int xCarry, ref int yCarry, bool rotatePoles) {
			xCarry = 0;
			yCarry = 0;

			if (y < 0) {
				yCarry += 2 * (Math.Abs(y));

				if (rotatePoles)
					xCarry += rangeX / 2;
			}

			// If the square goes under the bottom of the map, move 50% to the right and read up
			if (y > rangeY - 1) {
				yCarry -= 2 * (y - (rangeY - 1));

				if (rotatePoles)
					xCarry += rangeX / 2;
			}

			// If the square goes off the left side of the map, start reading from the right
			if (x + xCarry < 0)
				xCarry += rangeX;

			// If the square goes off the right side of the map, start reading from the left
			if (x + xCarry > rangeX - 1)
				xCarry += -rangeX;

			if (x + xCarry > rangeX - 1)
				xCarry -= rangeX;

			if (x + xCarry < 0)
				xCarry += rangeX;
		}
	}
}
