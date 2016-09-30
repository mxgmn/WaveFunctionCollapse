/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Drawing;
using System.Collections.Generic;

class OverlappingModel : Model
{
	int[][][][] propagator;
	int N;

	byte[][] patterns;
	List<Color> colors;
	int foundation;

	public OverlappingModel(string name, int N, int width, int height, bool periodicInput, bool periodicOutput, int symmetry, int foundation)
	{
		this.N = N;
		FMX = width;
		FMY = height;
		periodic = periodicOutput;

		var bitmap = new Bitmap($"samples/{name}.bmp");
		int SMX = bitmap.Width, SMY = bitmap.Height;
		byte[,] sample = new byte[SMX, SMY];
		colors = new List<Color>();

		for (int y = 0; y < SMY; y++) for (int x = 0; x < SMX; x++)
			{
				Color color = bitmap.GetPixel(x, y);

				int i = 0;
				foreach (var c in colors)
				{
					if (c == color) break;
					i++;
				}

				if (i == colors.Count) colors.Add(color);
				sample[x, y] = (byte)i;
			}

		int C = colors.Count;
		int W = Stuff.Power(C, N * N);

		Func<Func<int, int, byte>, byte[]> pattern = f =>
		{
			byte[] result = new byte[N * N];
			for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) result[x + y * N] = f(x, y);
			return result;
		};

		Func<int, int, byte[]> patternFromSample = (x, y) => pattern((dx, dy) => sample[(x + dx) % SMX, (y + dy) % SMY]);
		Func<byte[], byte[]> rotate = p => pattern((x, y) => p[N - 1 - y + x * N]);
		Func<byte[], byte[]> reflect = p => pattern((x, y) => p[N - 1 - x + y * N]);

		Func<byte[], int> index = p =>
		{
			int result = 0, power = 1;
			for (int i = 0; i < p.Length; i++)
			{
				result += p[p.Length - 1 - i] * power;
				power *= C;
			}
			return result;
		};

		Func<int, byte[]> patternFromIndex = ind =>
		{
			int residue = ind, power = W;
			byte[] result = new byte[N * N];

			for (int i = 0; i < result.Length; i++)
			{
				power /= C;
				int count = 0;

				while (residue >= power)
				{
					residue -= power;
					count++;
				}

				result[i] = (byte)count;
			}

			return result;
		};

		Dictionary<int, int> weights = new Dictionary<int, int>();
		for (int y = 0; y < (periodicInput ? SMY : SMY - N + 1); y++) for (int x = 0; x < (periodicInput ? SMX : SMX - N + 1); x++)
			{
				byte[][] ps = new byte[8][];

				ps[0] = patternFromSample(x, y);
				ps[1] = reflect(ps[0]);
				ps[2] = rotate(ps[0]);
				ps[3] = reflect(ps[2]);
				ps[4] = rotate(ps[2]);
				ps[5] = reflect(ps[4]);
				ps[6] = rotate(ps[4]);
				ps[7] = reflect(ps[6]);

				for (int k = 0; k < symmetry; k++)
				{
					int ind = index(ps[k]);
					if (weights.ContainsKey(ind)) weights[ind]++;
					else weights.Add(ind, 1);
				}
			}

		T = weights.Count;
		this.foundation = (foundation + T) % T;

		patterns = new byte[T][];
		stationary = new double[T];
		propagator = new int[T][][][];

		int counter = 0;
		foreach (int w in weights.Keys)
		{
			patterns[counter] = patternFromIndex(w);
			stationary[counter] = weights[w];
			counter++;
		}

		wave = new bool[FMX][][];
		changes = new bool[FMX][];
		for (int x = 0; x < FMX; x++)
		{
			wave[x] = new bool[FMY][];
			changes[x] = new bool[FMY];
			for (int y = 0; y < FMY; y++)
			{
				wave[x][y] = new bool[T];
				changes[x][y] = false;
				for (int t = 0; t < T; t++) wave[x][y][t] = true;
			}
		}

		Func<byte[], byte[], int, int, bool> agrees = (p1, p2, dx, dy) =>
		{
			int xmin = dx < 0 ? 0 : dx, xmax = dx < 0 ? dx + N : N, ymin = dy < 0 ? 0 : dy, ymax = dy < 0 ? dy + N : N;
			for (int y = ymin; y < ymax; y++) for (int x = xmin; x < xmax; x++) if (p1[x + N * y] != p2[x - dx + N * (y - dy)]) return false;
			return true;
		};

		for (int t = 0; t < T; t++)
		{
			propagator[t] = new int[2 * N - 1][][];
			for (int x = 0; x < 2 * N - 1; x++)
			{
				propagator[t][x] = new int[2 * N - 1][];
				for (int y = 0; y < 2 * N - 1; y++)
				{
					List<int> list = new List<int>();
					for (int t2 = 0; t2 < T; t2++) if (agrees(patterns[t], patterns[t2], x - N + 1, y - N + 1)) list.Add(t2);
					propagator[t][x][y] = new int[list.Count];
					for (int c = 0; c < list.Count; c++) propagator[t][x][y][c] = list[c];
				}
			}
		}
	}

	protected override bool OnBoundary(int x, int y) => !periodic && (x + N > FMX || y + N > FMY);

	override protected bool Propagate()
	{
		bool change = false, b;
		int x2, y2, sx, sy;
		bool[] allowed;

		for (int x1 = 0; x1 < FMX; x1++) for (int y1 = 0; y1 < FMY; y1++) if (changes[x1][y1])
				{
					changes[x1][y1] = false;
					for (int dx = -N + 1; dx < N; dx++) for (int dy = -N + 1; dy < N; dy++)
						{
							x2 = x1 + dx;
							y2 = y1 + dy;

							sx = x2;
							if (sx < 0) sx += FMX;
							else if (sx >= FMX) sx -= FMX;

							sy = y2;
							if (sy < 0) sy += FMY;
							else if (sy >= FMY) sy -= FMY;

							if (!periodic && (sx + N > FMX || sy + N > FMY)) continue;
							allowed = wave[sx][sy];

							for (int t2 = 0; t2 < T; t2++)
							{
								b = false;
								int[] prop = propagator[t2][N - 1 - dx][N - 1 - dy];
								for (int i1 = 0; i1 < prop.Length && !b; i1++) b = wave[x1][y1][prop[i1]];

								if (allowed[t2] && !b)
								{
									changes[sx][sy] = true;
									change = true;
									allowed[t2] = false;
								}
							}
						}
				}

		return change;
	}

	public override Bitmap Graphics()
	{
		Bitmap result = new Bitmap(FMX, FMY);

		for (int y = 0; y < FMY; y++) for (int x = 0; x < FMX; x++)
			{
				List<byte> contributors = new List<byte>();
				for (int dy = 0; dy < N; dy++) for (int dx = 0; dx < N; dx++)
					{
						int sx = x - dx;
						if (sx < 0) sx += FMX;

						int sy = y - dy;
						if (sy < 0) sy += FMY;

						if (OnBoundary(sx, sy)) continue;
						for (int t = 0; t < T; t++) if (wave[sx][sy][t]) contributors.Add(patterns[t][dx + dy * N]);
					}

				int r = 0, g = 0, b = 0;
				foreach (byte c in contributors)
				{
					Color color = colors[c];
					r += color.R;
					g += color.G;
					b += color.B;
				}

				float lambda = 1.0f / (float)contributors.Count;
				result.SetPixel(x, y, Color.FromArgb((int)(lambda * r), (int)(lambda * g), (int)(lambda * b)));
			}

		return result;
	}

	protected override void Clear()
	{
		base.Clear();

		if (foundation != 0)
		{
			for (int x = 0; x < FMX; x++)
			{
				for (int t = 0; t < T; t++) if (t != foundation) wave[x][FMY - 1][t] = false;
				changes[x][FMY - 1] = true;

				for (int y = 0; y < FMY - 1; y++)
				{
					wave[x][y][foundation] = false;
					changes[x][y] = true;
				}

				while (Propagate());
			}
		}
	}
}