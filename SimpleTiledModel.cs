/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Linq;
using System.Drawing;
using System.Xml.Linq;
using System.Drawing.Imaging;
using System.Collections.Generic;

class SimpleTiledModel : Model
{
    List<Color[]> tiles;
    List<string> tilenames;
    int tilesize;
    bool black;

    public SimpleTiledModel(string name, string subsetName, int width, int height, bool periodic, bool black) : base(width, height)
    {
        this.periodic = periodic;
        this.black = black;

        XElement xroot = XDocument.Load($"samples/{name}/data.xml").Root;
        tilesize = xroot.Get("size", 16);
        bool unique = xroot.Get("unique", false);

        List<string> subset = null;
        if (subsetName != null)
        {
            XElement xsubset = xroot.Element("subsets").Elements("subset").FirstOrDefault(x => x.Get<string>("name") == subsetName);
            if (xsubset == null) Console.WriteLine($"ERROR: subset {subsetName} is not found");
            else subset = xsubset.Elements("tile").Select(x => x.Get<string>("name")).ToList();
        }

        Color[] tile(Func<int, int, Color> f)
        {
            Color[] result = new Color[tilesize * tilesize];
            for (int y = 0; y < tilesize; y++) for (int x = 0; x < tilesize; x++) result[x + y * tilesize] = f(x, y);
            return result;
        };

        Color[] rotate(Color[] array) => tile((x, y) => array[tilesize - 1 - y + x * tilesize]);
        Color[] reflect(Color[] array) => tile((x, y) => array[tilesize - 1 - x + y * tilesize]);

        tiles = new List<Color[]>();
        tilenames = new List<string>();
        var tempStationary = new List<double>();

        List<int[]> action = new List<int[]>();
        Dictionary<string, int> firstOccurrence = new Dictionary<string, int>();

        foreach (XElement xtile in xroot.Element("tiles").Elements("tile"))
        {
            string tilename = xtile.Get<string>("name");
            if (subset != null && !subset.Contains(tilename)) continue;

            Func<int, int> a, b;
            int cardinality;

            char sym = xtile.Get("symmetry", 'X');
            if (sym == 'L')
            {
                cardinality = 4;
                a = i => (i + 1) % 4;
                b = i => i % 2 == 0 ? i + 1 : i - 1;
            }
            else if (sym == 'T')
            {
                cardinality = 4;
                a = i => (i + 1) % 4;
                b = i => i % 2 == 0 ? i : 4 - i;
            }
            else if (sym == 'I')
            {
                cardinality = 2;
                a = i => 1 - i;
                b = i => i;
            }
            else if (sym == '\\')
            {
                cardinality = 2;
                a = i => 1 - i;
                b = i => 1 - i;
            }
            else if (sym == 'F')
            {
                cardinality = 8;
                a = i => i < 4 ? (i + 1) % 4 : 4 + (i - 1) % 4;
                b = i => i < 4 ? i + 4 : i - 4;
            }
            else
            {
                cardinality = 1;
                a = i => i;
                b = i => i;
            }

            T = action.Count;
            firstOccurrence.Add(tilename, T);

            int[][] map = new int[cardinality][];
            for (int t = 0; t < cardinality; t++)
            {
                map[t] = new int[8];

                map[t][0] = t;
                map[t][1] = a(t);
                map[t][2] = a(a(t));
                map[t][3] = a(a(a(t)));
                map[t][4] = b(t);
                map[t][5] = b(a(t));
                map[t][6] = b(a(a(t)));
                map[t][7] = b(a(a(a(t))));

                for (int s = 0; s < 8; s++) map[t][s] += T;

                action.Add(map[t]);
            }

            if (unique)
            {
                for (int t = 0; t < cardinality; t++)
                {
                    Bitmap bitmap = new Bitmap($"samples/{name}/{tilename} {t}.png");
                    tiles.Add(tile((x, y) => bitmap.GetPixel(x, y)));
                    tilenames.Add($"{tilename} {t}");
                }
            }
            else
            {
                Bitmap bitmap = new Bitmap($"samples/{name}/{tilename}.png");
                tiles.Add(tile((x, y) => bitmap.GetPixel(x, y)));
                tilenames.Add($"{tilename} 0");

                for (int t = 1; t < cardinality; t++)
                {
                    if (t <= 3) tiles.Add(rotate(tiles[T + t - 1]));
                    if (t >= 4) tiles.Add(reflect(tiles[T + t - 4]));
                    tilenames.Add($"{tilename} {t}");
                }
            }

            for (int t = 0; t < cardinality; t++) tempStationary.Add(xtile.Get("weight", 1.0f));
        }

        T = action.Count;
        weights = tempStationary.ToArray();

        propagator = new int[4][][];
        var tempPropagator = new bool[4][][];
        for (int d = 0; d < 4; d++)
        {
            tempPropagator[d] = new bool[T][];
            propagator[d] = new int[T][];
            for (int t = 0; t < T; t++) tempPropagator[d][t] = new bool[T];
        }

        foreach (XElement xneighbor in xroot.Element("neighbors").Elements("neighbor"))
        {
            string[] left = xneighbor.Get<string>("left").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] right = xneighbor.Get<string>("right").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (subset != null && (!subset.Contains(left[0]) || !subset.Contains(right[0]))) continue;

            int L = action[firstOccurrence[left[0]]][left.Length == 1 ? 0 : int.Parse(left[1])], D = action[L][1];
            int R = action[firstOccurrence[right[0]]][right.Length == 1 ? 0 : int.Parse(right[1])], U = action[R][1];

            tempPropagator[0][R][L] = true;
            tempPropagator[0][action[R][6]][action[L][6]] = true;
            tempPropagator[0][action[L][4]][action[R][4]] = true;
            tempPropagator[0][action[L][2]][action[R][2]] = true;

            tempPropagator[1][U][D] = true;
            tempPropagator[1][action[D][6]][action[U][6]] = true;
            tempPropagator[1][action[U][4]][action[D][4]] = true;
            tempPropagator[1][action[D][2]][action[U][2]] = true;
        }

        for (int t2 = 0; t2 < T; t2++) for (int t1 = 0; t1 < T; t1++)
            {
                tempPropagator[2][t2][t1] = tempPropagator[0][t1][t2];
                tempPropagator[3][t2][t1] = tempPropagator[1][t1][t2];
            }

        List<int>[][] sparsePropagator = new List<int>[4][];
        for (int d = 0; d < 4; d++)
        {
            sparsePropagator[d] = new List<int>[T];
            for (int t = 0; t < T; t++) sparsePropagator[d][t] = new List<int>();
        }

        for (int d = 0; d < 4; d++) for (int t1 = 0; t1 < T; t1++)
            {
                List<int> sp = sparsePropagator[d][t1];
                bool[] tp = tempPropagator[d][t1];

                for (int t2 = 0; t2 < T; t2++) if (tp[t2]) sp.Add(t2);

                int ST = sp.Count;
                if (ST == 0) Console.WriteLine($"ERROR: tile {tilenames[t1]} has no neighbors in direction {d}");
                propagator[d][t1] = new int[ST];
                for (int st = 0; st < ST; st++) propagator[d][t1][st] = sp[st];
            }
    }

    protected override bool OnBoundary(int x, int y) => !periodic && (x < 0 || y < 0 || x >= FMX || y >= FMY);

    public override Bitmap Graphics()
    {
        Bitmap result = new Bitmap(FMX * tilesize, FMY * tilesize);
        int[] bitmapData = new int[result.Height * result.Width];

        if (observed != null)
        {
            for (int x = 0; x < FMX; x++) for (int y = 0; y < FMY; y++)
                {
                    Color[] tile = tiles[observed[x + y * FMX]];
                    for (int yt = 0; yt < tilesize; yt++) for (int xt = 0; xt < tilesize; xt++)
                        {
                            Color c = tile[xt + yt * tilesize];
                            bitmapData[x * tilesize + xt + (y * tilesize + yt) * FMX * tilesize] =
                                unchecked((int)0xff000000 | (c.R << 16) | (c.G << 8) | c.B);
                        }
                }
        }
        else
        {
            for (int x = 0; x < FMX; x++) for (int y = 0; y < FMY; y++)
                {
                    bool[] a = wave[x + y * FMX];
                    int amount = (from b in a where b select 1).Sum();
                    double lambda = 1.0 / (from t in Enumerable.Range(0, T) where a[t] select weights[t]).Sum();

                    for (int yt = 0; yt < tilesize; yt++) for (int xt = 0; xt < tilesize; xt++)
                        {
                            if (black && amount == T) bitmapData[x * tilesize + xt + (y * tilesize + yt) * FMX * tilesize] = unchecked((int)0xff000000);
                            else
                            {
                                double r = 0, g = 0, b = 0;
                                for (int t = 0; t < T; t++) if (a[t])
                                    {
                                        Color c = tiles[t][xt + yt * tilesize];
                                        r += (double)c.R * weights[t] * lambda;
                                        g += (double)c.G * weights[t] * lambda;
                                        b += (double)c.B * weights[t] * lambda;
                                    }

                                bitmapData[x * tilesize + xt + (y * tilesize + yt) * FMX * tilesize] =
                                    unchecked((int)0xff000000 | ((int)r << 16) | ((int)g << 8) | (int)b);
                            }
                        }
                }
        }

        var bits = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        System.Runtime.InteropServices.Marshal.Copy(bitmapData, 0, bits.Scan0, bitmapData.Length);
        result.UnlockBits(bits);

        return result;
    }

    public string TextOutput()
    {
        var result = new System.Text.StringBuilder();

        for (int y = 0; y < FMY; y++)
        {
            for (int x = 0; x < FMX; x++) result.Append($"{tilenames[observed[x + y * FMX]]}, ");
            result.Append(Environment.NewLine);
        }

        return result.ToString();
    }
}
