// Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)

using System;

abstract class Model
{
    protected bool[][] wave;

    protected int[][][] propagator;
    int[][][] compatible;
    protected int[] observed;

    (int, int)[] stack;
    int stacksize, observedSoFar;

    protected int MX, MY, T, N;
    protected bool periodic, ground;

    protected double[] weights;
    double[] weightLogWeights, distribution;

    protected int[] sumsOfOnes;
    double sumOfWeights, sumOfWeightLogWeights, startingEntropy;
    protected double[] sumsOfWeights, sumsOfWeightLogWeights, entropies;

    public enum Heuristic { Entropy, MRV, Scanline };
    Heuristic heuristic;

    protected Model(int width, int height, int N, bool periodic, Heuristic heuristic)
    {
        MX = width;
        MY = height;
        this.N = N;
        this.periodic = periodic;
        this.heuristic = heuristic;
    }

    void Init()
    {
        wave = new bool[MX * MY][];
        compatible = new int[wave.Length][][];
        for (int i = 0; i < wave.Length; i++)
        {
            wave[i] = new bool[T];
            compatible[i] = new int[T][];
            for (int t = 0; t < T; t++) compatible[i][t] = new int[4];
        }
        distribution = new double[T];
        observed = new int[MX * MY];

        weightLogWeights = new double[T];
        sumOfWeights = 0;
        sumOfWeightLogWeights = 0;

        for (int t = 0; t < T; t++)
        {
            weightLogWeights[t] = weights[t] * Math.Log(weights[t]);
            sumOfWeights += weights[t];
            sumOfWeightLogWeights += weightLogWeights[t];
        }

        startingEntropy = Math.Log(sumOfWeights) - sumOfWeightLogWeights / sumOfWeights;

        sumsOfOnes = new int[MX * MY];
        sumsOfWeights = new double[MX * MY];
        sumsOfWeightLogWeights = new double[MX * MY];
        entropies = new double[MX * MY];

        stack = new (int, int)[wave.Length * T];
        stacksize = 0;
    }

    public bool Run(int seed, int limit)
    {
        if (wave == null) Init();

        Clear();
        Random random = new(seed);

        for (int l = 0; l < limit || limit < 0; l++)
        {
            int node = NextUnobservedNode(random);
            if (node >= 0)
            {
                Observe(node, random);
                bool success = Propagate();
                if (!success) return false;
            }
            else
            {
                for (int i = 0; i < wave.Length; i++) for (int t = 0; t < T; t++) if (wave[i][t]) { observed[i] = t; break; }
                return true;
            }
        }

        return true;
    }

    int NextUnobservedNode(Random random)
    {
        if (heuristic == Heuristic.Scanline)
        {
            for (int i = observedSoFar; i < wave.Length; i++)
            {
                if (!periodic && (i % MX + N > MX || i / MX + N > MY)) continue;
                if (sumsOfOnes[i] > 1)
                {
                    observedSoFar = i + 1;
                    return i;
                }
            }
            return -1;
        }

        double min = 1E+4;
        int argmin = -1;
        for (int i = 0; i < wave.Length; i++)
        {
            if (!periodic && (i % MX + N > MX || i / MX + N > MY)) continue;
            int remainingValues = sumsOfOnes[i];
            double entropy = heuristic == Heuristic.Entropy ? entropies[i] : remainingValues;
            if (remainingValues > 1 && entropy <= min)
            {
                double noise = 1E-6 * random.NextDouble();
                if (entropy + noise < min)
                {
                    min = entropy + noise;
                    argmin = i;
                }
            }
        }
        return argmin;
    }

    void Observe(int node, Random random)
    {
        bool[] w = wave[node];
        for (int t = 0; t < T; t++) distribution[t] = w[t] ? weights[t] : 0.0;
        int r = distribution.Random(random.NextDouble());
        for (int t = 0; t < T; t++) if (w[t] != (t == r)) Ban(node, t);
    }

    bool Propagate()
    {
        while (stacksize > 0)
        {
            (int i1, int t1) = stack[stacksize - 1];
            stacksize--;

            int x1 = i1 % MX;
            int y1 = i1 / MX;

            for (int d = 0; d < 4; d++)
            {
                int x2 = x1 + dx[d];
                int y2 = y1 + dy[d];
                if (!periodic && (x2 < 0 || y2 < 0 || x2 + N > MX || y2 + N > MY)) continue;

                if (x2 < 0) x2 += MX;
                else if (x2 >= MX) x2 -= MX;
                if (y2 < 0) y2 += MY;
                else if (y2 >= MY) y2 -= MY;

                int i2 = x2 + y2 * MX;
                int[] p = propagator[d][t1];
                int[][] compat = compatible[i2];

                for (int l = 0; l < p.Length; l++)
                {
                    int t2 = p[l];
                    int[] comp = compat[t2];

                    comp[d]--;
                    if (comp[d] == 0) Ban(i2, t2);
                }
            }
        }

        return sumsOfOnes[0] > 0;
    }

    void Ban(int i, int t)
    {
        wave[i][t] = false;

        int[] comp = compatible[i][t];
        for (int d = 0; d < 4; d++) comp[d] = 0;
        stack[stacksize] = (i, t);
        stacksize++;

        sumsOfOnes[i] -= 1;
        sumsOfWeights[i] -= weights[t];
        sumsOfWeightLogWeights[i] -= weightLogWeights[t];

        double sum = sumsOfWeights[i];
        entropies[i] = Math.Log(sum) - sumsOfWeightLogWeights[i] / sum;
    }

    void Clear()
    {
        for (int i = 0; i < wave.Length; i++)
        {
            for (int t = 0; t < T; t++)
            {
                wave[i][t] = true;
                for (int d = 0; d < 4; d++) compatible[i][t][d] = propagator[opposite[d]][t].Length;
            }

            sumsOfOnes[i] = weights.Length;
            sumsOfWeights[i] = sumOfWeights;
            sumsOfWeightLogWeights[i] = sumOfWeightLogWeights;
            entropies[i] = startingEntropy;
            observed[i] = -1;
        }
        observedSoFar = 0;

        if (ground)
        {
            for (int x = 0; x < MX; x++)
            {
                for (int t = 0; t < T - 1; t++) Ban(x + (MY - 1) * MX, t);
                for (int y = 0; y < MY - 1; y++) Ban(x + y * MX, T - 1);
            }
            Propagate();
        }
    }

    public abstract void Save(string filename);

    protected static int[] dx = { -1, 0, 1, 0 };
    protected static int[] dy = { 0, 1, 0, -1 };
    static int[] opposite = { 2, 3, 0, 1 };
}
