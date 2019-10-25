/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

class OverlappingModel : Model
{
    int N;
    byte[][] patterns;
    List<Color> colors;
    int ground;

    public OverlappingModel(string name, int N, int width, int height, bool periodicInput, bool periodicOutput, int symmetry, int ground)
        : base(width, height)
    {
        this.N = N;
        periodic = periodicOutput;

        var bitmap = new Bitmap($"samples/{name}.png");
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
        long W = C.ToPower(N * N);

        byte[] pattern(Func<int, int, byte> f)
        {
            byte[] result = new byte[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) result[x + y * N] = f(x, y);
            return result;
        };

        byte[] patternFromSample(int x, int y) => pattern((dx, dy) => sample[(x + dx) % SMX, (y + dy) % SMY]);
        byte[] rotate(byte[] p) => pattern((x, y) => p[N - 1 - y + x * N]);
        byte[] reflect(byte[] p) => pattern((x, y) => p[N - 1 - x + y * N]);

        long index(byte[] p)
        {
            long result = 0, power = 1;
            for (int i = 0; i < p.Length; i++)
            {
                result += p[p.Length - 1 - i] * power;
                power *= C;
            }
            return result;
        };

        byte[] patternFromIndex(long ind)
        {
            long residue = ind, power = W;
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

        Dictionary<long, int> weights = new Dictionary<long, int>();
        List<long> ordering = new List<long>();

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
                    long ind = index(ps[k]);
                    if (weights.ContainsKey(ind)) weights[ind]++;
                    else
                    {
                        weights.Add(ind, 1);
                        ordering.Add(ind);
                    }
                }
            }

        T = weights.Count;
        this.ground = (ground + T) % T;
        patterns = new byte[T][];
        base.weights = new double[T];

        int counter = 0;
        foreach (long w in ordering)
        {
            patterns[counter] = patternFromIndex(w);
            base.weights[counter] = weights[w];
            counter++;
        }

        bool agrees(byte[] p1, byte[] p2, int dx, int dy)
        {
            int xmin = dx < 0 ? 0 : dx, xmax = dx < 0 ? dx + N : N, ymin = dy < 0 ? 0 : dy, ymax = dy < 0 ? dy + N : N;
            for (int y = ymin; y < ymax; y++) for (int x = xmin; x < xmax; x++) if (p1[x + N * y] != p2[x - dx + N * (y - dy)]) return false;
            return true;
        };

        propagator = new int[4][][];
        for (int d = 0; d < 4; d++)
        {
            propagator[d] = new int[T][];
            for (int t = 0; t < T; t++)
            {
                List<int> list = new List<int>();
                for (int t2 = 0; t2 < T; t2++) if (agrees(patterns[t], patterns[t2], DX[d], DY[d])) list.Add(t2);
                propagator[d][t] = new int[list.Count];
                for (int c = 0; c < list.Count; c++) propagator[d][t][c] = list[c];
            }
        }
    }

    protected override bool OnBoundary(int x, int y) => !periodic && (x + N > FMX || y + N > FMY || x < 0 || y < 0);

    public override Bitmap Graphics()
    {
        Bitmap result = new Bitmap(FMX, FMY);
        int[] bitmapData = new int[result.Height * result.Width];

        if (observed != null)
        {
            for (int y = 0; y < FMY; y++)
            {
                int dy = y < FMY - N + 1 ? 0 : N - 1;
                for (int x = 0; x < FMX; x++)
                {
                    int dx = x < FMX - N + 1 ? 0 : N - 1;
                    Color c = colors[patterns[observed[x - dx + (y - dy) * FMX]][dx + dy * N]];
                    bitmapData[x + y * FMX] = unchecked((int)0xff000000 | (c.R << 16) | (c.G << 8) | c.B);
                }
            }
        }
        else
        {
            for (int i = 0; i < wave.Length; i++)
            {
                int contributors = 0, r = 0, g = 0, b = 0;
                int x = i % FMX, y = i / FMX;

                for (int dy = 0; dy < N; dy++) for (int dx = 0; dx < N; dx++)
                    {
                        int sx = x - dx;
                        if (sx < 0) sx += FMX;

                        int sy = y - dy;
                        if (sy < 0) sy += FMY;

                        int s = sx + sy * FMX;
                        if (OnBoundary(sx, sy)) continue;
                        for (int t = 0; t < T; t++) if (wave[s][t])
                            {
                                contributors++;
                                Color color = colors[patterns[t][dx + dy * N]];
                                r += color.R;
                                g += color.G;
                                b += color.B;
                            }
                    }

                bitmapData[i] = unchecked((int)0xff000000 | ((r / contributors) << 16) | ((g / contributors) << 8) | b / contributors);
            }
        }

        var bits = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        System.Runtime.InteropServices.Marshal.Copy(bitmapData, 0, bits.Scan0, bitmapData.Length);
        result.UnlockBits(bits);

        return result;
    }

    protected override void Clear()
    {
        base.Clear();

        if (ground != 0)
        {
            for (int x = 0; x < FMX; x++)
            {
                for (int t = 0; t < T; t++) if (t != ground) Ban(x + (FMY - 1) * FMX, t);
                for (int y = 0; y < FMY - 1; y++) Ban(x + y * FMX, ground);
            }

            Propagate();
        }
    }
}
