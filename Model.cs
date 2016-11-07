/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;

abstract class Model
{
	protected bool[][][] wave;
	protected bool[][] changes;
	protected double[] stationary;
	protected int[][] observed;

	protected Random random;
	protected int FMX, FMY, T, limit;
	protected bool periodic;

	double[] logProb;
	double logT;

	protected Model(int width, int height)
	{
		FMX = width;
		FMY = height;

		wave = new bool[FMX][][];
		changes = new bool[FMX][];
		for (int x = 0; x < FMX; x++)
		{
			wave[x] = new bool[FMY][];
			changes[x] = new bool[FMY];
		}
	}

	protected abstract bool Propagate();

	bool? Observe()
	{
		double min = 1E+3, sum, mainSum, logSum, noise, entropy;
		int argminx = -1, argminy = -1, amount;
		bool[] w;

		for (int x = 0; x < FMX; x++) for (int y = 0; y < FMY; y++)
			{
				if (OnBoundary(x, y)) continue;

				w = wave[x][y];
				amount = 0;
				sum = 0;

				for (int t = 0; t < T; t++) if (w[t])
					{
						amount += 1;
						sum += stationary[t];
					}

				if (sum == 0) return false;

				noise = 1E-6 * random.NextDouble();

				if (amount == 1) entropy = 0;
				else if (amount == T) entropy = logT;
				else
				{
					mainSum = 0;
					logSum = Math.Log(sum);
					for (int t = 0; t < T; t++) if (w[t]) mainSum += stationary[t] * logProb[t];
					entropy = logSum - mainSum / sum;
				}

				if (entropy > 0 && entropy + noise < min)
				{
					min = entropy + noise;
					argminx = x;
					argminy = y;
				}
			}

		if (argminx == -1 && argminy == -1)
		{
			observed = new int[FMX][];
			for (int x = 0; x < FMX; x++)
			{
				observed[x] = new int[FMY];
				for (int y = 0; y < FMY; y++) for (int t = 0; t < T; t++) if (wave[x][y][t])
						{
							observed[x][y] = t;
							break;
						}
			}
							
			return true;
		}

		double[] distribution = new double[T];
		for (int t = 0; t < T; t++) distribution[t] = wave[argminx][argminy][t] ? stationary[t] : 0;
		int r = distribution.Random(random.NextDouble());
		for (int t = 0; t < T; t++) wave[argminx][argminy][t] = t == r;
		changes[argminx][argminy] = true;

		return null;
	}

	public bool Run(int seed, int limit)
	{
		logT = Math.Log(T);
		logProb = new double[T];
		for (int t = 0; t < T; t++) logProb[t] = Math.Log(stationary[t]);

		Clear();

		random = new Random(seed);

		for (int l = 0; l < limit || limit == 0; l++)
		{
			bool? result = Observe();
			if (result != null) return (bool)result;
			while (Propagate());
		}

		return true;
	}

	protected virtual void Clear()
	{
		for (int x = 0; x < FMX; x++) for (int y = 0; y < FMY; y++)
			{
				for (int t = 0; t < T; t++) wave[x][y][t] = true;
				changes[x][y] = false;
			}
	}

	protected abstract bool OnBoundary(int x, int y);
	public abstract System.Drawing.Bitmap Graphics();
}
