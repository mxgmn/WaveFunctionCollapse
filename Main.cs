/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Xml;

using NDesk.Options;

static class Program
{
	static void Main(string[] args)
	{
		bool showHelp = false;
		string samplesFrom = "";
		string inputPath = "";
		string outputFile = "out.png";

		string modelName = "overlapping";
		bool periodic = false;
		int width = 48;
		int height = 48;
		int limit = 0;

		int overlapN = 2;
		int overlapSymmetry = 8;
		int overlapGround = 0;
		bool overlapPeriodicInput = true;

		string tiledSubset = default(string);
		bool tiledBlack = false;

		var p = new OptionSet() {
			{ "samples-from=", "Process samples from XML {FILE}. All other options ignored.",
				v => samplesFrom = v },
			{ "i|input=", "Input {PATH}: PNG image for --model=overlapping; directory containing data.xml for --model=simpletiled",
				v => inputPath = v },
			{ "o|output=", "Output {FILE}, default=" + $"{outputFile}",
				v => outputFile = v },
			{ "w|width=", "Tiled image width {INT} when --model=simpletiled, default=" + $"{width}",
				(int v) => width = v },
			{ "h|height=", "Tiled image height {INT} when --model=simpletiled, default=" + $"{height}",
				(int v) => height = v },
			{ "m|model=", "Model {TYPE}: `overlapping` (default) or `simpletiled`. Required.",
				v => modelName = v },
			{ "n=", "{N} parameter, when --model=overlapping, default=" + $"{overlapN}",
				(int v) => overlapN  = v },
			{ "limit=", "Model limit {INT}, default="+ $"{limit}",
				(int v) => limit  = v },
			{ "p|periodic", "Periodic, default false",
				v => periodic = v != null },
			{ "symmetry=", "Symmetry {INT}, when --model=overlapping, default=" + $"{overlapSymmetry}",
				(int v) => overlapSymmetry = v },
			{ "ground=", "Ground {INT}, when --model=overlapping, default=" + $"{overlapGround}",
				(int v) => overlapGround = v },
			{ "pi|periodicInput=", "Periodic input {BOOL}, when --model=overlapping, default=" + $"{overlapPeriodicInput}",
				(bool v) => overlapPeriodicInput = v },
			{ "subset=", "Subset {NAME} in data.xml, when --model=simpletiled",
				v => tiledSubset = v },
			{ "black=", "Black, when --model=simpletiled, default false",
				v => tiledBlack = v != null },
			{ "help", "Display help and exit",
				v => showHelp = v != null },
		};
		try {
			p.Parse(args);
		}
		catch (OptionException e) {
			Console.Write("wfc: ");
			Console.Write(e.Message);
			Console.WriteLine("Try `wfc --help` for more information.");
			return;
		}

		if (showHelp) {
			ShowHelp(p);
			return;
		}
		
		if (samplesFrom != "") {
			processSamplesFrom(samplesFrom);
			return;
		}

		if (inputPath == "") {
			Console.WriteLine("wfc: missing input");
			ShowHelp(p);
			return;
		}

		Random random = new Random();
		Model model;

		if (modelName == "overlapping") {
			model = new OverlappingModel(
				inputPath, overlapN, width, height, overlapPeriodicInput, periodic, overlapSymmetry, overlapGround);
		} else if (modelName == "simpletiled") {
			model = new SimpleTiledModel(
				inputPath, tiledSubset, width, height, periodic, tiledBlack);
		} else {
			Console.WriteLine("wfc: unsupported model type: " + modelName);
			ShowHelp(p);
			return;
		}

		for (int k = 0; k < 10; k++)
		{
			int seed = random.Next();
			bool finished = model.Run(seed, limit);
			if (finished)
			{
				Console.WriteLine("DONE");
				model.Graphics().Save($"{outputFile}");
				break;
			}
			else Console.WriteLine("CONTRADICTION");
		}
	}

	static void processSamplesFrom(string samplesFrom) {
		Random random = new Random();
		var xdoc = new XmlDocument();
		xdoc.Load(samplesFrom);

		int counter = 1;
		foreach (XmlNode xnode in xdoc.FirstChild.ChildNodes)
		{
			if (xnode.Name == "#comment") continue;

			Model model;
			string name = xnode.Get<string>("name");
			Console.WriteLine($"< {name}");

			if (xnode.Name == "overlapping") {
				string inputPath = $"samples/{name}.png";
				model = new OverlappingModel(inputPath, xnode.Get("N", 2), xnode.Get("width", 48), xnode.Get("height", 48), 
					xnode.Get("periodicInput", true), xnode.Get("periodic", false),
					xnode.Get("symmetry", 8), xnode.Get("foundation", 0));
			} else if (xnode.Name == "simpletiled") {
				string inputPath = $"samples/{name}";
				model = new SimpleTiledModel(inputPath, xnode.Get<string>("subset"), 
					xnode.Get("width", 10), xnode.Get("height", 10), xnode.Get("periodic", false), xnode.Get("black", false));
			} else continue;

			for (int i = 0; i < xnode.Get("screenshots", 2); i++)
			{
				for (int k = 0; k < 10; k++)
				{
					Console.Write("> ");
					int seed = random.Next();
					bool finished = model.Run(seed, xnode.Get("limit", 0));
					if (finished)
					{
						Console.WriteLine("DONE");

						model.Graphics().Save($"{counter} {name} {i}.png");
						if (model is SimpleTiledModel && xnode.Get("textOutput", false))
							System.IO.File.WriteAllText($"{counter} {name} {i}.txt", (model as SimpleTiledModel).TextOutput());

						break;
					}
					else Console.WriteLine("CONTRADICTION");
				}
			}

			counter++;
		}
	}

	static void ShowHelp (OptionSet p)
	{
		Console.WriteLine ("Usage: wfc [OPTIONS]");
		Console.WriteLine ("Bitmap & tilemap generation from a single example with the help of ideas from quantum mechanics.");
		Console.WriteLine ();
		Console.WriteLine ("Options:");
		p.WriteOptionDescriptions (Console.Out);
	}
}
