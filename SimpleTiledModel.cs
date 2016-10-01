/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Xml;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;

class SimpleTiledModel : Model
{
	bool[][][] propagator;

	List<Color[]> tiles;
	int tilesize;
	bool black;

	public SimpleTiledModel(string name, string subsetName, int width, int height, bool periodic, bool black)
	{
		FMX = width;
		FMY = height;
		this.periodic = periodic;
		this.black = black;

		var xdoc = new XmlDocument();
		xdoc.Load($"samples/{name}/data.xml");
		XmlNode xnode = xdoc.FirstChild;
		tilesize = xnode.Get("size", 16);
		bool unique = xnode.Get("unique", false);
		xnode = xnode.FirstChild;

		List<string> subset = null;
		if (subsetName != default(string))
		{
			subset = new List<string>();
			foreach (XmlNode xsubset in xnode.NextSibling.NextSibling.ChildNodes) 
				if (xsubset.NodeType != XmlNodeType.Comment && xsubset.Get<string>("name") == subsetName)
					foreach (XmlNode stile in xsubset.ChildNodes) subset.Add(stile.Get<string>("name"));
		}

		Func<Func<int, int, Color>, Color[]> tile = f =>
		{
			Color[] result = new Color[tilesize * tilesize];
			for (int y = 0; y < tilesize; y++) for (int x = 0; x < tilesize; x++) result[x + y * tilesize] = f(x, y);
			return result;
		};

		Func<Color[], Color[]> rotate = array => tile((x, y) => array[tilesize - 1 - y + x * tilesize]);

		tiles = new List<Color[]>();
		var tempStationary = new List<double>();

		List<int[]> action = new List<int[]>();
		Dictionary<string, int> firstOccurrence = new Dictionary<string, int>();

		foreach (XmlNode xtile in xnode.ChildNodes)
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
					Bitmap bitmap = new Bitmap($"samples/{name}/{tilename} {t}.bmp");
					tiles.Add(tile((x, y) => bitmap.GetPixel(x, y)));
				}
			}
			else
			{
				Bitmap bitmap = new Bitmap($"samples/{name}/{tilename}.bmp");
				tiles.Add(tile((x, y) => bitmap.GetPixel(x, y)));
				for (int t = 1; t < cardinality; t++) tiles.Add(rotate(tiles[T + t - 1]));
			}

			for (int t = 0; t < cardinality; t++) tempStationary.Add(xtile.Get("weight", 1.0f));
		}

		T = action.Count;
		stationary = tempStationary.ToArray();

		propagator = new bool[4][][];
		for (int d = 0; d < 4; d++)
		{
			propagator[d] = new bool[T][];
			for (int t = 0; t < T; t++) propagator[d][t] = new bool[T];
		}

		wave = new bool[FMX][][];
		changes = new bool[FMX][];
		for (int x = 0; x < FMX; x++)
		{
			wave[x] = new bool[FMY][];
			changes[x] = new bool[FMY];
			for (int y = 0; y < FMY; y++) wave[x][y] = new bool[T];
		}

		foreach (XmlNode xneighbor in xnode.NextSibling.ChildNodes)
		{
			string[] left = xneighbor.Get<string>("left").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			string[] right = xneighbor.Get<string>("right").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			if (subset != null && (!subset.Contains(left[0]) || !subset.Contains(right[0]))) continue;

			int L = action[firstOccurrence[left[0]]][left.Length == 1 ? 0 : int.Parse(left[1])], D = action[L][1];
			int R = action[firstOccurrence[right[0]]][right.Length == 1 ? 0 : int.Parse(right[1])], U = action[R][1];

			propagator[0][R][L] = true;
			propagator[0][action[R][6]][action[L][6]] = true;
			propagator[0][action[L][4]][action[R][4]] = true;
			propagator[0][action[L][2]][action[R][2]] = true;

			propagator[1][U][D] = true;
			propagator[1][action[D][6]][action[U][6]] = true;
			propagator[1][action[U][4]][action[D][4]] = true;
			propagator[1][action[D][2]][action[U][2]] = true;
		}

		 for (int t2 = 0; t2 < T; t2++)
            for (int t1 = 0; t1 < T; t1++)
            {
				propagator[2][t2][t1] = propagator[0][t1][t2];
				propagator[3][t2][t1] = propagator[1][t1][t2];
			}
	}

	protected override bool Propagate()
	{
		bool change = false, b;
		for (int x2 = 0; x2 < FMX; x2++)
            for (int y2 = 0; y2 < FMY; y2++)
                for (int d = 0; d < 4; d++)
				{
					int x1 = x2, y1 = y2;
					if (d == 0)
					{
						if (x2 == 0)
						{
							if (!periodic) continue;
							else x1 = FMX - 1;
						}
						else x1 = x2 - 1;
					}
					else if (d == 1)
					{
						if (y2 == FMY - 1)
						{
							if (!periodic) continue;
							else y1 = 0;
						}
						else y1 = y2 + 1;
					}
					else if (d == 2)
					{
						if (x2 == FMX - 1)
						{
							if (!periodic) continue;
							else x1 = 0;
						}
						else x1 = x2 + 1;
					}
					else
					{
						if (y2 == 0)
						{
							if (!periodic) continue;
							else y1 = FMY - 1;
						}
						else y1 = y2 - 1;
					}

					if (!changes[x1][y1]) continue;
                    bool[] w = wave[x1][y1];
                    bool[] wc = wave[x2][y2];

                    for (int t2 = 0; t2 < T; t2++) if (wc[t2])
						{
                            bool[] p = propagator[d][t2];

							b = false;
                            for (int t1 = 0; t1 < T && !b; t1++) if (w[t1]) b = p[t1];
							if (!b)
							{
								wave[x2][y2][t2] = false;
								changes[x2][y2] = true;
								change = true;
							}
						}
				}

		return change;
	}

	protected override bool OnBoundary(int x, int y) => false;

	public override Bitmap Graphics()
	{
		Bitmap result = new Bitmap(FMX * tilesize, FMY * tilesize);
        
        int[] bmpData = new int[result.Height * result.Width];

        for (int x = 0; x < FMX; x++) for (int y = 0; y < FMY; y++)
			{
				bool[] a = wave[x][y];
				int amount = (from b in a where b select 1).Sum();
				double lambda = 1.0 / (from t in Enumerable.Range(0, T) where a[t] select stationary[t]).Sum();

				for (int yt = 0; yt < tilesize; yt++) for (int xt = 0; xt < tilesize; xt++)
					{
                        if (black && amount == T) bmpData[x * tilesize + xt + (y * tilesize + yt) * FMX * tilesize] = unchecked((int)0xff000000);
                        else
                        {
                            double r = 0, g = 0, b = 0;
                            for (int t = 0; t < T; t++) if (wave[x][y][t])
                                {
                                    Color c = tiles[t][xt + yt * tilesize];
                                    r += (double)c.R * stationary[t] * lambda;
                                    g += (double)c.G * stationary[t] * lambda;
                                    b += (double)c.B * stationary[t] * lambda;
                                }

                            bmpData[x * tilesize + xt + (y * tilesize + yt) * FMX * tilesize] = unchecked((int)0xff000000 | ((int)r << 16) | ((int)g << 8) | (int)b);
                        }
					}
			}

        var bits = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        System.Runtime.InteropServices.Marshal.Copy(bmpData, 0, bits.Scan0, bmpData.Length);
        result.UnlockBits(bits);

        return result;
	}
}