/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Xml.Linq;
using System.Diagnostics;
using WaveFunctionCollapse.Extensions;
using WaveFunctionCollapse.Builders;

static class Program
{
    private const string WIDTH_KEY = "width";
    private const string HEIGHT_KEY = "height";
    private const string PERIODIC_KEY = "periodic";

    static void Main()
    {
        Stopwatch sw = Stopwatch.StartNew();

        Random random = new Random();
        XDocument xdoc = XDocument.Load("samples.xml");

        int counter = 1;
        foreach (XElement xelem in xdoc.Root.Elements("overlapping", "simpletiled"))
        {
            Model model;
            string name = xelem.Get<string>("name");
            Console.WriteLine($"< {name}");

            if (xelem.Name == "overlapping")
            {
                model = new OverlappingModelBuilder()
                    .WithN(xelem.Get("N", 2))
                    .WithName(name)
                    .WithHeight(xelem.Get(WIDTH_KEY, 48))
                    .WithWidth(xelem.Get(HEIGHT_KEY, 48))
                    .WithPeriodicInput(xelem.Get("periodicInput", true))
                    .WithPeriodicOutput(xelem.Get(PERIODIC_KEY, false))
                    .WithSymmetry(xelem.Get("symmetry", 8))
                    .WithGround(xelem.Get("ground", 0))
                    .Build();
            }
            else if (xelem.Name == "simpletiled")
            {
                model = new SimpleTiledModelBuilder()
                    .WithName(name)
                    .WithSubsetName(xelem.Get<string>("subset"))
                    .WithWidth(xelem.Get(WIDTH_KEY, 10))
                    .WithHeight(xelem.Get(HEIGHT_KEY, 10))
                    .WithPeriodic(xelem.Get(PERIODIC_KEY, false))
                    .WithBlack(xelem.Get("black", false))
                    .Build();
            }
            else continue;

            for (int i = 0; i < xelem.Get("screenshots", 2); i++)
            {
                for (int k = 0; k < 10; k++)
                {
                    Console.Write("> ");
                    int seed = random.Next();
                    bool finished = model.Run(seed, xelem.Get("limit", 0));
                    if (finished)
                    {
                        Console.WriteLine("DONE");

                        model.Graphics().Save($"{counter} {name} {i}.png");
                        if (model is SimpleTiledModel && xelem.Get("textOutput", false))
                            System.IO.File.WriteAllText($"{counter} {name} {i}.txt", (model as SimpleTiledModel).TextOutput());

                        break;
                    }
                    else Console.WriteLine("CONTRADICTION");
                }
            }

            counter++;
        }

        Console.WriteLine($"time = {sw.ElapsedMilliseconds}");
    }
}
