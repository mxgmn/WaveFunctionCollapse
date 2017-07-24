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
	protected bool[][] wave;
	protected double[] stationary;
	protected int[] observed;

	protected bool[] changes;
	protected int[] stack;
	protected int stacksize;

	protected Random random;
	protected int FMX, FMY, T;
	protected bool periodic;

	double[] logProb;
	double logT;

	protected Model(int width, int height)
	{
		FMX = width;
		FMY = height;

		wave = new bool[FMX * FMY][];
		changes = new bool[FMX * FMY];

		stack = new int[FMX * FMY];
		stacksize = 0;
	}

	protected abstract void Propagate();

	bool? Observe()
	{
		double min = 1E+3;
		int argmin = -1;

		for (int i = 0; i < wave.Length; i++)
		{
			if (OnBoundary(i)) continue;

			bool[] w = wave[i];
			int amount = 0;
			double sum = 0;

			for (int t = 0; t < T; t++) if (w[t])
				{
					amount += 1;
					sum += stationary[t];
				}

			if (sum == 0) return false;

			double noise = 1E-6 * random.NextDouble();

			double entropy;
			if (amount == 1) entropy = 0;
			else if (amount == T) entropy = logT;
			else
			{
				double mainSum = 0;
				double logSum = Math.Log(sum);
				for (int t = 0; t < T; t++) if (w[t]) mainSum += stationary[t] * logProb[t];
				entropy = logSum - mainSum / sum;
			}

			if (entropy > 0 && entropy + noise < min)
			{
				min = entropy + noise;
				argmin = i;
			}
		}

		if (argmin == -1)
		{
			observed = new int[FMX * FMY];
			for (int i = 0; i < wave.Length; i++) for (int t = 0; t < T; t++) if (wave[i][t]) { observed[i] = t; break; }
			return true;
		}

		double[] distribution = new double[T];
		for (int t = 0; t < T; t++) distribution[t] = wave[argmin][t] ? stationary[t] : 0;
		int r = distribution.Random(random.NextDouble());
		for (int t = 0; t < T; t++) wave[argmin][t] = t == r;
		Change(argmin);

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
			Propagate();
		}

		return true;
	}

	protected void Change(int i)
	{
		if (changes[i]) return;

		stack[stacksize] = i;
		stacksize++;
		changes[i] = true;
	}

	protected virtual void Clear()
	{
		for (int i = 0; i < wave.Length; i++)
		{
			for (int t = 0; t < T; t++) wave[i][t] = true;
			changes[i] = false;
		}
	}

	protected abstract bool OnBoundary(int i);
	public abstract System.Drawing.Bitmap Graphics();
}
