# WaveFunctionCollapse
This program generates bitmaps that are locally similar to the input bitmap.
<p align="center"><img alt="main collage" src="images/wfc.png"></p>
<p align="center"><img alt="main gif" src="images/wfc.gif"></p>

Local similarity means that

* (C1) The output should contain only those NxN patterns of pixels that are present in the input.
* (Weak C2) Distribution of NxN patterns in the input should be similar to the distribution of NxN patterns over a sufficiently large number of outputs. In other words, probability to meet a particular pattern in the output should be close to the density of such patterns in the input.

In the examples a typical value of N is 3.
<p align="center"><img alt="local similarity" src="images/patterns.png"></p>

WFC initializes output bitmap in a completely unobserved state, where each pixel value is in superposition of colors of the input bitmap (so if the input was black & white then the unobserved states are shown in different shades of grey). The coefficients in these superpositions are real numbers, not complex numbers, so it doesn't do the actual quantum mechanics, but it was inspired by QM. Then the program goes into the observation-propagation cycle:

* On each observation step an NxN region is chosen among the unobserved which has the lowest Shannon entropy. This region's state then collapses into a definite state according to its coefficients and the distribution of NxN patterns in the input.
* On each propagation step new information gained from the collapse on the previous step propagates through the output.

On each step the number of non-zero coefficients decreases and in the end we have a completely observed state, the wave function has collapsed.

It may happen that during propagation all the coefficients for a certain pixel become zero. That means that the algorithm has run into a contradiction and can not continue. The problem of determining whether a certain bitmap allows other nontrivial bitmaps satisfying condition (C1) is NP-hard, so it's impossible to create a fast solution that always finishes. In practice, however, the algorithm runs into contradictions surprisingly rarely.

Wave Function Collapse algorithm has been implemented in
[C++](https://github.com/math-fehr/fast-wfc),
[Python](https://github.com/ikarth/wfc_2019f),
[Kotlin](https://github.com/j-roskopf/WFC),
[Rust](https://github.com/sdleffler/collapse),
[Julia](https://github.com/roberthoenig/WaveFunctionCollapse.jl),
[Go](https://github.com/shawnridgeway/wfc),
[Haxe](https://github.com/Mitim-84/WFC-Gen),
[Java](https://github.com/sjcasey21/wavefunctioncollapse),
[Clojure](https://github.com/sjcasey21/wavefunctioncollapse-clj),
[Free Pascal](https://github.com/PascalCorpsman/mini_projects/tree/main/miniprojects/Wave_function_collapse/Overlap_model),
[p5js](https://github.com/D-T-666/wave-function-collapse-p5),
[JavaScript](https://github.com/kchapelier/wavefunctioncollapse)
and adapted to [Unity](https://selfsame.itch.io/unitywfc),
[Unreal Engine 5](https://docs.unrealengine.com/5.0/en-US/BlueprintAPI/WaveFunctionCollapse/)
and [Houdini](https://www.sidefx.com/tutorials/wfc-dungeon-generator/).
You can [build WFC from source](https://github.com/mxgmn/WaveFunctionCollapse#how-to-build),
download an official [release](https://github.com/mxgmn/WaveFunctionCollapse/releases) for Windows,
download an interactive graphical version from [itch.io](https://exutumno.itch.io/wavefunctioncollapse)
or [run it in the browser](http://www.kchapelier.com/wfc-example/overlapping-model.html).
WFC generates levels in [Bad North](https://www.badnorth.com/),
[Caves of Qud](https://store.steampowered.com/app/333640/Caves_of_Qud/),
[Dead Static Drive](https://twitter.com/deadstaticdrive),
[Townscaper](https://store.steampowered.com/app/1291340/Townscaper/),
[Matrix Awakens](https://www.youtube.com/watch?v=usJrcwN6T4I),
[several](https://arcadia-clojure.itch.io/proc-skater-2016)
[smaller](https://arcadia-clojure.itch.io/swapland)
[games](https://marian42.itch.io/wfc) and many prototypes.
It led to [new](https://escholarship.org/uc/item/3rm1w0mn)
[research](https://hal.inria.fr/hal-01706539v3/document).
For [more](https://twitter.com/OskSta/status/784847588893814785)
[related](https://twitter.com/dwtw/status/810166761270243328)
[work](https://github.com/mewo2/oisin),
[explanations](https://trasevol.dog/2017/09/01/di19/),
[interactive demos](http://oskarstalberg.com/game/wave/wave.html),
[guides](https://www.dropbox.com/s/zeiat1w8zre9ro8/Knots%20breakdown.png?dl=0),
[tutorials](http://www.procjam.com/tutorials/wfc/)
and [examples](https://twitter.com/ExUtumno/status/895684431477747715)
see the [ports, forks and spinoffs section](https://github.com/mxgmn/WaveFunctionCollapse#notable-ports-forks-and-spinoffs).

Watch a video demonstration of WFC algorithm on YouTube: [https://youtu.be/DOQTr2Xmlz0](https://youtu.be/DOQTr2Xmlz0)

## Algorithm
1. Read the input bitmap and count NxN patterns.
    1. (optional) Augment pattern data with rotations and reflections.
2. Create an array with the dimensions of the output (called "wave" in the source). Each element of this array represents a state of an NxN region in the output. A state of an NxN region is a superposition of NxN patterns of the input with boolean coefficients (so a state of a pixel in the output is a superposition of input colors with real coefficients). False coefficient means that the corresponding pattern is forbidden, true coefficient means that the corresponding pattern is not yet forbidden.
3. Initialize the wave in the completely unobserved state, i.e. with all the boolean coefficients being true.
4. Repeat the following steps:
    1. Observation:
        1. Find a wave element with the minimal nonzero entropy. If there is no such elements (if all elements have zero or undefined entropy) then break the cycle (4) and go to step (5).
        2. Collapse this element into a definite state according to its coefficients and the distribution of NxN patterns in the input.
    2. Propagation: propagate information gained on the previous observation step.
5. By now all the wave elements are either in a completely observed state (all the coefficients except one being zero) or in the contradictory state (all the coefficients being zero). In the first case return the output. In the second case finish the work without returning anything.

## Tilemap generation
The simplest nontrivial case of the algorithm is when NxN=1x2 (well, NxM). If we simplify it even further by storing not the probabilities of pairs of colors but the probabilities of colors themselves, we get what we call a "simple tiled model". The propagation phase in this model is just adjacency constraint propagation. It's convenient to initialize the simple tiled model with a list of tiles and their adjacency data (adjacency data can be viewed as a large set of very small samples) rather than a sample bitmap.
<p align="center"><a href="http://i.imgur.com/jIctSoT.gifv"><img src="images/tile.gif"/></a></p>
<!--<p align="center">
  <a href="images/tile.gif">GIF</a> |
  <a href="http://i.imgur.com/jIctSoT.gifv">GIFV</a>
</p>-->

Lists of all the possible pairs of adjacent tiles in practical tilesets can be quite long, so I implemented a symmetry system for tiles to shorten the enumeration. In this system each tile should be assigned with its symmetry type.
<p align="center"><img alt="symmetries" src="images/symmetry-system.png"></p>

Note that the tiles have the same symmetry type as their assigned letters (or, in other words, actions of the 
dihedral group D4 are isomorphic for tiles and their corresponding letters). With this system it's enough to enumerate pairs of adjacent tiles only up to symmetry, which makes lists of adjacencies for tilesets with many symmetrical tiles (even the summer tileset, despite drawings not being symmetrical the system considers such tiles to be symmetrical) several times shorter.
<p align="center">
<img alt="knots" src="images/knots.png">
<img alt="tiled rooms" src="images/rooms.png">
<img alt="circuit 1" src="images/circuit-1.png">
<img alt="circuit 2" src="images/circuit-2.png">
<img alt="circles" src="images/circles.png">
<img alt="castle" src="images/castle.png">
<img alt="summer 1" src="images/summer-1.png">
<img alt="summer 2" src="images/summer-2.png">
</p>

Note that the unrestrained knot tileset (with all 5 tiles being allowed) is not interesting for WFC, because you can't run into a situation where you can't place a tile. We call tilesets with this property "easy". Without special heuristics easy tilesets don't produce interesting global arrangements, because correlations of tiles in easy tilesets quickly fall off with a distance. Many easy tilesets can be found on [Guy Walker's website](http://cr31.co.uk/stagecast/wang/tiles_e.html). Consider the "Dual" 2-edge tileset there. How can it generate knots (without t-junctions, not easy) while being easy? The answer is, it can only generate a narrow class of knots, it can't produce an arbitrary knot.

Note also that Circuit, Summer and Rooms tilesets are non-Wang. That is, their adjacency data cannot be induced from edge labels. For example, in Circuit two Corners cannot be adjacent, yet they can be connected with a Connection tile, and diagonal tracks cannot change direction.

## Higher dimensions
WFC algorithm in higher dimensions works completely the same way as in dimension 2, though performance becomes an issue. These voxel models were generated with N=2 overlapping tiled model using 5x5x5 and 5x5x2 blocks and additional heuristics (height, density, curvature, ...).
<p align="center"><img alt="voxels" src="images/castles-3d.png"></p>

Higher resolution screenshots: [1](http://i.imgur.com/0bsjlBY.png), [2](http://i.imgur.com/GduN0Vr.png), [3](http://i.imgur.com/IEOsbIy.png).

[MarkovJunior](https://github.com/mxgmn/MarkovJunior) repository contains an implementation of the 3d simple tiled model with many [tilesets](https://github.com/mxgmn/MarkovJunior/tree/main/resources/tilesets) and [examples](https://github.com/mxgmn/MarkovJunior/blob/main/images/top-1764.png).

## Constrained synthesis
WFC algorithm supports constraints. Therefore, it can be easily combined with other generative algorithms or with manual creation.

Here is WFC autocompleting a level started by a human:
<p align="center"><a href="http://i.imgur.com/X3aNDUv.gifv"><img src="images/constrained.gif"/></a></p>
<!--<p align="center">
  <a href="images/constrained.gif">GIF</a> |
  <a href="http://i.imgur.com/X3aNDUv.gifv">GIFV</a>
</p>-->

[ConvChain](https://github.com/mxgmn/ConvChain) algorithm satisfies the strong version of the condition (C2): the limit distribution of NxN patterns in the outputs it is producing is exactly the same as the distributions of patterns in the input. However, ConvChain doesn't satisfy (C1): it often produces noticeable defects. It makes sense to run ConvChain first to get a well-sampled configuration and then run WFC to correct local defects. This is similar to a common strategy in optimization: first run a Monte-Carlo method to find a point close to a global optimum and then run a gradient descent from that point for greater accuracy.

P. F. Harrison's [texture synthesis](https://github.com/mxgmn/TextureSynthesis) algorithm is significantly faster than WFC, but it has trouble with long correlations (for example, it's difficult for this algorithm to synthesize brick wall textures with correctly aligned bricks). But this is exactly where WFC shines, and Harrison's algorithm supports constraints. It makes sense first to generate a perfect brick wall blueprint with WFC and then run a constrained texture synthesis algorithm on that blueprint.

## Comments
Why the minimal entropy heuristic? I noticed that when humans draw something they often follow the [minimal entropy heuristic](images/lowest-entropy-heuristic.gif) themselves. That's why the algorithm is so enjoyable to watch.

The overlapping model relates to the simple tiled model the same way higher order Markov chains relate to order one Markov chains.

WFC's propagation phase is very similar to the loopy belief propagation algorithm. In fact, I first programmed belief propagation, but then switched to constraint propagation with a saved stationary distribution, because BP is significantly slower without a massive parallelization (on a CPU) and didn't produce significantly better results in my problems.

Note that the "Simple Knot" and "Trick Knot" samples have 3 colors, not 2.

One of the dimensions can be time. In particular, d-dimensional WFC captures the behaviour of any (d-1)-dimensional cellular automata.

## Used work
1. Alexei A. Efros and Thomas K. Leung, [Texture Synthesis by Non-parametric Sampling](https://www2.eecs.berkeley.edu/Research/Projects/CS/vision/papers/efros-iccv99.pdf), 1999. WaveFunctionCollapse is a [texture synthesis](https://en.wikipedia.org/wiki/Texture_synthesis) algorithm. Compared to the earlier texture synthesis algorithms, WFC guarantees that the output contains only those NxN patterns that are present in the input. This makes WFC perfect for level generation in games and pixel art, and less suited for large full-color textures.
2. Paul C. Merrell, [Model Synthesis](http://graphics.stanford.edu/~pmerrell/thesis.pdf), 2009. Merrell derives adjacency constraints between tiles from an example model and generates a new larger model with the AC-3 algorithm. We generalize his approach to work with NxN overlapping patterns of tiles instead of individual tiles. This allows to use a single image as the input to the algorithm. By varying N, we can make the output look more like the input or less. We introduce the [lowest entropy heuristic](images/lowest-entropy-heuristic.gif) that removes the [directional bias](images/directional-bias.png) in generated results, is defined for irregular grids and is better suited for [pre-constrained problems](images/constrained.gif). We implement a tile symmetry system to reduce the sizes of inputs. We visualize partially observed states, either with [color averaging](images/wfc.gif) or [per-voxel voting](https://twitter.com/ExUtumno/status/900395635412787202). Merrell also introduced a method of incrementally modifying the model in parts to reduce the failure rate (which we don't use here). Recently the author created a [page](https://paulmerrell.org/model-synthesis/) for model synthesis and published [code](https://github.com/merrell42/model-synthesis).
3. Alan K. Mackworth, [Consistency in Networks of Relations](https://www.cs.ubc.ca/~mack/Publications/AI77.pdf), 1977. WFC translates a texture synthesis problem into a constraint satisfaction problem. Currently it uses the [AC-4 algorithm](http://www.cs.utah.edu/~tch/CS4300/resources/AC4.pdf) by Roger Mohr and Thomas C. Henderson, 1986.
4. Paul F. Harrison, [Image Texture Tools](http://logarithmic.net/pfh-files/thesis/dissertation.pdf), 2005. WFC was also influenced by the declarative texture synthesis chapter of Paul Harrison's dissertation. The author defines adjacency data of tiles by labeling their borders and uses backtracking search to fill the tilemap. A [demonstration of the algorithm](https://logarithmic.net/ghost.xhtml) is available on the web.

## How to build
WFC is a console application that depends only on the standard library. Get [.NET Core](https://www.microsoft.com/net/download) for Windows, Linux or macOS and run
```
dotnet run --configuration Release WaveFunctionCollapse.csproj
```
Generated results are saved into the `output` folder. Edit `samples.xml` to change model parameters.

Alternatively, use build instructions from the community for various platforms from the [relevant issue](https://github.com/mxgmn/WaveFunctionCollapse/issues/3). Casey Marshall made a [pull request](https://github.com/mxgmn/WaveFunctionCollapse/pull/18) that makes using the program with the command line more convenient and includes snap packaging.

## Notable ports, forks and spinoffs
* Emil Ernerfeldt made a [C++ port](https://github.com/emilk/wfc).
* [Max Aller](https://github.com/nanodeath) made a Kotlin (JVM) library, [Kollapse](https://gitlab.com/nanodeath/kollapse). Joseph Roskopf made a line by line Kotlin [port](https://github.com/j-roskopf/WFC) of the optimized 2018 version. Edwin Jakobs made a [Kotlin library](https://github.com/edwinRNDR/wfc) that supports [3d examples](https://www.youtube.com/watch?v=g4Ih8wxBh1E).
* [Kevin Chapelier](https://github.com/kchapelier) made a [JavaScript port](http://www.kchapelier.com/wfc-example/overlapping-model.html).
* Oskar Stålberg programmed a 3d tiled model, a 2d tiled model for irregular grids on a sphere and is building beautiful 3d tilesets for them: [1](https://twitter.com/OskSta/status/787319655648100352), [2](https://twitter.com/OskSta/status/784847588893814785), [3](https://twitter.com/OskSta/status/784847933686575104), [4](https://twitter.com/OskSta/status/784848286272327680), [5](https://twitter.com/OskSta/status/793545297376972801), [6](https://twitter.com/OskSta/status/793806535898136576), [7](https://twitter.com/OskSta/status/802496920790777856), [8](https://twitter.com/OskSta/status/804291629561577472), [9](https://twitter.com/OskSta/status/806856212260278272), [10](https://twitter.com/OskSta/status/806904557502464000), [11](https://twitter.com/OskSta/status/818857408848130048), [12](https://twitter.com/OskSta/status/832633189277409280), [13](https://twitter.com/OskSta/status/851170356530475008), [14](https://twitter.com/OskSta/status/858301207936458752), [15](https://twitter.com/OskSta/status/863019585162932224).
* [Joseph Parker](https://github.com/selfsame) adapted [WFC to Unity](https://selfsame.itch.io/unitywfc) and used it generate skateparks in the [Proc Skater 2016](https://arcadia-clojure.itch.io/proc-skater-2016) game, [fantastic plateaus](https://twitter.com/jplur_/status/929482200034226176) in the 2017 game [Swapland](https://arcadia-clojure.itch.io/swapland) and [platform levels](https://twitter.com/jplur_/status/1053458654454865921) in the 2018 game [Bug with a Gun](https://selfsame.itch.io/bug-with-a-gun).
* [Martin O'Leary](https://github.com/mewo2) applied a [WFC-like algorithm](https://github.com/mewo2/oisin) to poetry generation: [1](https://twitter.com/mewo2/status/789167437518217216), [2](https://twitter.com/mewo2/status/789177702620114945), [3](https://twitter.com/mewo2/status/789187174683987968), [4](https://twitter.com/mewo2/status/789897712372183041).
* [Nick Nenov](https://github.com/NNNenov) made a [3d voxel tileset](https://twitter.com/NNNenov/status/789903180226301953) based on my Castle tileset. Nick uses text output option in the tiled model to reconstruct 3d models in Cinema 4D.
* Sean Leffler implemented the [overlapping model in Rust](https://github.com/sdleffler/collapse).
* rid5x is making an [OCaml version of WFC](https://twitter.com/rid5x/status/782442620459114496).
* I made an [interactive version](https://twitter.com/ExUtumno/status/798571284342837249) of the overlapping model, you can download the GUI executable from the [WFC itch.io page](https://exutumno.itch.io/wavefunctioncollapse).
* [Brian Bucklew](https://github.com/unormal) built a level generation pipeline that applies WFC in multiple passes for the [Caves of Qud](http://store.steampowered.com/app/333640) game: [1](https://twitter.com/unormal/status/805987523596091392), [2](https://twitter.com/unormal/status/808566029387448320), [3](https://twitter.com/unormal/status/808523056259993601), [4](https://twitter.com/unormal/status/808523493994364928), [5](https://twitter.com/unormal/status/808519575264497666), [6](https://twitter.com/unormal/status/808519216185876480), [7](https://twitter.com/unormal/status/808795396508123136), [8](https://twitter.com/unormal/status/808860105093632001), [9](https://twitter.com/unormal/status/809637856432033792), [10](https://twitter.com/unormal/status/810239794433425408), [11](https://twitter.com/unormal/status/811034574973243393), [12](https://twitter.com/unormal/status/811720423419314176), [13](https://twitter.com/unormal/status/811034037259276290), [14](https://twitter.com/unormal/status/810971337309224960), [15](https://twitter.com/unormal/status/811405368777723909), [16](https://twitter.com/ptychomancer/status/812053801544757248), [17](https://twitter.com/unormal/status/812159308263788544), [18](https://twitter.com/unormal/status/812158749838340096), [19](https://twitter.com/unormal/status/814569437181476864), [20](https://twitter.com/unormal/status/814570383189876738), [21](https://twitter.com/unormal/status/819725864623603712), [22](https://twitter.com/unormal/status/984719207156862976).
* [Danny Wynne](https://github.com/dannywynne) implemented a [3d tiled model](https://twitter.com/dwtw/status/810166761270243328).
* Arvi Teikari programmed a [texture synthesis algorithm with the entropy heuristic](http://www.hempuli.com/blogblog/archives/1598) in Lua. Headchant [ported](https://github.com/headchant/iga) it to work with LÖVE.
* Isaac Karth made a [Python port](https://github.com/ikarth/wfc_python) of the overlapping model.
* Oskar Stålberg made an [interactive version](http://oskarstalberg.com/game/wave/wave.html) of the tiled model that runs in the browser.
* [Matt Rix](https://github.com/MattRix) implemented a 3d tiled model ([1](https://twitter.com/MattRix/status/869403586664570880), [2](https://twitter.com/MattRix/status/870999185167962113), [3](https://twitter.com/MattRix/status/871054734018453505), [4](https://twitter.com/MattRix/status/871056805761359872)) and made a 3-dimensional tiled model where one of the dimensions is time ([1](https://twitter.com/MattRix/status/872674537799913472), [2](https://twitter.com/MattRix/status/872648369625325568), [3](https://twitter.com/MattRix/status/872645716660891648), [4](https://twitter.com/MattRix/status/872641331956518914), [5](https://twitter.com/MattRix/status/979020989181890560)).
* [Nick Nenov](https://github.com/NNNenov) made a [visual guide](https://www.dropbox.com/s/zeiat1w8zre9ro8/Knots%20breakdown.png?dl=0) to the tile symmetry system.
* [Isaac Karth](https://github.com/ikarth) and [Adam M. Smith](https://github.com/rndmcnlly) wrote a [paper](https://ieeexplore.ieee.org/document/9421370) ([open access link](https://escholarship.org/uc/item/3rm1w0mn)) in which they examine the role of backtracking and different possible heuristics in WFC, experiment with global constraints and combine WFC with VQ-VAE. Earlier in 2017, the authors wrote a [workshop paper](https://adamsmith.as/papers/wfc_is_constraint_solving_in_the_wild.pdf) where they formulate WFC as an ASP problem, use general constraint solver [clingo](https://github.com/potassco/clingo) to generate bitmaps, trace WFC's history and give a detailed explanation of the algorithm.
* Sylvain Lefebvre made a [C++ implementation](https://github.com/sylefeb/VoxModSynth) of 3d model synthesis, described the thought process of designing a sample and provided an example where adjacency constraints ensure that the output is connected (walkable).
* I generalized 3d WFC to work with cube symmetry group and made a tileset that generates [Escheresque scenes](https://twitter.com/ExUtumno/status/895684431477747715).
* There are many ways to visualize partially observed wave states. In the code, color values of possible options are averaged to produce the resulting color. Oskar Stålberg [shows](https://twitter.com/OskSta/status/863019585162932224) partially observed states as semi-transparent boxes, where the box is bigger for a state with more options. In the voxel setting I [visualize](https://twitter.com/ExUtumno/status/900395635412787202) wave states with per-voxel voting.
* Remy Devaux implemented the tiled model in PICO-8 and wrote an [article](https://trasevol.dog/2017/09/01/di19/) about generation of coherent data with an explanation of WFC.
* For the upcoming game [Bad North](https://www.badnorth.com/) Oskar Stålberg [uses](https://twitter.com/OskSta/status/917405214638006273) a heuristic that tries to select such tiles
that the resulting observed zone is navigable at each step.
* William Manning [implemented](https://github.com/heyx3/easywfc) the overlapping model in C# with the primary goal of making code readable, and provided it with WPF GUI.
* [Joseph Parker](https://gist.github.com/selfsame) wrote a WFC [tutorial](http://www.procjam.com/tutorials/wfc/) for Procjam 2017.
* [Aman Tiwari](https://github.com/aman-tiwari) formulated the connectivity constraint as an [ASP problem](https://gist.github.com/aman-tiwari/8a7b874cb1fd1270adc203b2af293f4c) for clingo.
* Matvey Khokhlov programmed a [3d overlapping model](https://github.com/MatveyK/Kazimir).
* [Sylvain Lefebvre](https://github.com/sylefeb), [Li-Yi Wei](https://github.com/1iyiwei) and [Connelly Barnes](https://github.com/connellybarnes) are [investigating](https://hal.archives-ouvertes.fr/hal-01706539/) the possibility of hiding information inside textures. They made a [tool](https://members.loria.fr/Sylvain.Lefebvre/infotexsyn/) that can encode text messages as WFC tilings and decode them back. This technique allows to use WFC tilings as QR codes.
* [Mathieu Fehr](https://github.com/math-fehr) and [Nathanael Courant](https://github.com/Ekdohibs) significantly [improved](https://github.com/math-fehr/fast-wfc) the running time of WFC, by an order of magnitude for the overlapping model. I [integrated](https://github.com/mxgmn/WaveFunctionCollapse/commit/fad1066b5000f7e9fbda0ef81bbea56799686670) their improvements into the code.
* Vasu Mahesh [ported](https://github.com/vasumahesh1/WFC_WebGL) 3d tiled model to TypeScript, made a new tileset and [visualised](https://vasumahesh1.github.io/WFC_WebGL) the generation process in WebGL.
* [Hwanhee Kim](https://github.com/greentec) experimented with 3d WFC and created/adapted many voxel tilesets: [1](https://twitter.com/greentecq/status/1025348928634408960), [2](https://twitter.com/greentecq/status/1004068394553913344), [3](https://twitter.com/greentecq/status/1005835830802305024), [4](https://twitter.com/greentecq/status/1022851327041265664), [5](https://twitter.com/greentecq/status/1011351814216736769), [6](https://twitter.com/greentecq/status/1008210550944387077), [7](https://twitter.com/greentecq/status/1006390606875070464), [8](https://twitter.com/greentecq/status/1015182718810841088).
* Oskar Stålberg gave a [talk](https://www.youtube.com/watch?v=0bcZb-SsnrA) about level generation in Bad North at the Everything Procedural Conference 2018.
* I [wrote](https://twitter.com/ExUtumno/status/1024314661951467521) about how to generate (approximately) unbiased paths between 2 points with WFC and other algorithms. I [implemented](https://github.com/mxgmn/MarkovJunior/blob/main/models/TilePath.xml) this method in MarkovJunior.
* [Isaac Karth](https://github.com/ikarth) and [Adam M. Smith](https://github.com/rndmcnlly) published a [preprint](https://arxiv.org/abs/1809.04432) where they describe a system based on WFC that learns from both positive and negative examples, and discuss it in a general context of dialogs with example-driven generators.
* Brendan Anthony [uses](https://steamcommunity.com/games/314230/announcements/detail/3369147113795750369) WFC to generate wall decorations in the game [Rodina](https://store.steampowered.com/app/314230/Rodina/).
* Tim Kong implemented the [overlapping model in Haxe](https://github.com/Mitim-84/WFC-Gen).
* In order to generate connected structures, Boris the Brave applied the [chiseling method](https://www.boristhebrave.com/2018/04/28/random-paths-via-chiseling) to WFC. He published a [library](https://boristhebrave.github.io/DeBroglie) that supports hex grids, additional constraints and backtracking.
* [Marian Kleineberg](https://github.com/marian42) [created](https://twitter.com/marian42_/status/1061785383057440768) an [infinite city generator](https://marian42.itch.io/wfc) based on the tiled model for Procjam 2018. He wrote an [article](https://marian42.de/article/wfc) describing his approaches to setting adjacencies, backtracking and the online variation of WFC.
* Sol Bekic [programmed](https://github.com/s-ol/gpWFC) the tiled model that runs on GPU using PyOpenCL. Instead of keeping a queue of nodes to propagate from, it propagates from every node on the grid in parallel.
* Wouter van Oortmerssen [implemented](https://github.com/aardappel/lobster/commit/703f67472bfd80c26bb626e1d5c22ec91047da98) the tiled model in a single C++ function, with a structure similar to a priority queue for faster observation.
* Robert Hoenig [implemented](https://github.com/roberthoenig/WaveFunctionCollapse.jl) the overlapping model in Julia, with an option to propagate constraints only locally.
* [Edwin Jakobs](https://github.com/edwinRNDR) applied WFC to [style transfer](https://twitter.com/voorbeeld/status/1073874337248239616) and [dithering](https://twitter.com/voorbeeld/status/1073875725499985926).
* Breanna Baltaxe-Admony [applied](https://github.com/bbaltaxe/wfc-piano-roll) WFC to music generation.
* Shawn Ridgeway made a [Go port](https://github.com/shawnridgeway/wfc).
* For the Global Game Jam 2019, [Andy Wallace](https://github.com/andymasteroffish) made a [game](http://andymakesgames.tumblr.com/post/182363131350/global-game-jam-2019-maureens-chaotic-dungeon) in which the player can interact with WFC-based level generator by resetting portions of the level with various weapons.
* Stephen Sherratt wrote a [detailed explanation](https://gridbugs.org/wave-function-collapse/) of the overlapping model and made a [Rust library](https://github.com/stevebob/wfc). For the 7DRL Challenge 2019 he made a roguelike [Get Well Soon](https://gridbugs.org/get-well-soon/) that [uses](https://gridbugs.org/7drl2019-day1/) WFC to generate levels.
* Florian Drux created a [generalization](https://github.com/lamelizard/GraphWaveFunctionCollapse/blob/master/thesis.pdf) that works on graphs with arbitrary local structure and [implemented](https://github.com/lamelizard/GraphWaveFunctionCollapse) it in Python.
* Bob Burrough [discovered](https://twitter.com/ExUtumno/status/1119996185199116289) a percolation-like phase transition in one of the tilesets that manifests in spiking contradiction rate.
* Oskar Stålberg combined WFC with marching cubes on irregular grids and made a town building toy [Townscaper](https://store.steampowered.com/app/1291340/Townscaper/) based on it: [1](https://twitter.com/OskSta/status/1164926304640229376), [2](https://twitter.com/OskSta/status/1168168400155267072), [3](https://twitter.com/OskSta/status/1181464374839521280), [4](https://twitter.com/OskSta/status/1189109278361165825), [5](https://twitter.com/OskSta/status/1189902695303458816), [6](https://www.youtube.com/watch?v=1hqt8JkYRdI). Oskar gave a number of talks and interviews about the mixed initiative town generation in Townscaper: [EPC2021](https://www.youtube.com/watch?v=NOJYZYqY6_M), [SGC21](https://www.youtube.com/watch?v=Uxeo9c-PX-w), [Konsoll 2021](https://www.youtube.com/watch?v=5xrRTOikBBg), [AI and Games](https://www.youtube.com/watch?v=_1fvJ5sHh6A).
* In his Rust roguelike tutorial, [Herbert Wolverson](https://github.com/thebracket) wrote a [chapter](http://bfnightly.bracketproductions.com/rustbook/chapter_33.html) about implementing the WFC algorithm from scratch.
* At the [Game Developers Conference 2019](https://www.youtube.com/watch?v=AdCgi9E90jw) and the [Roguelike Celebration 2019](https://www.youtube.com/watch?v=fnFj3dOKcIQ), [Brian Bucklew](https://github.com/unormal) gave talks about WFC and how Freehold Games uses it to generate levels in [Caves of Qud](https://store.steampowered.com/app/333640/Caves_of_Qud/). The talks discuss problems with overfitting and homogeny, level connectedness and combining WFC with constructive procgen methods.
* [Boris the Brave](https://github.com/boristhebrave) published a [commercial Unity asset](https://assetstore.unity.com/packages/tools/modeling/tessera-procedural-tile-based-generator-155425) based on the tiled model.
* Steven Casey ported WFC to [Java](https://github.com/sjcasey21/wavefunctioncollapse) and [Clojure](https://github.com/sjcasey21/wavefunctioncollapse-clj).
* Nuño de la Serna implemented the 3d tiled model in an [openFrameworks addon](https://github.com/action-script/ofxWFC3D) that supports tiles with no symmetries.
* [Paul Ambrosiussen](https://github.com/Ambrosiussen) [integrated](https://github.com/sideeffects/SideFXLabs) the overlapping model into Houdini and gave a [talk](https://vimeo.com/400993662) about the algorithm and his implementation at Houdini HIVE 2020.
* Keijiro Takahashi [implemented](https://github.com/keijiro/WfcMaze) a 3d tiled model and generated Escheresque scenes with it: [1](https://twitter.com/_kzr/status/1248993799960838144), [2](https://twitter.com/_kzr/status/1248990065327345664), [3](https://twitter.com/_kzr/status/1248884103274827777), [4](https://twitter.com/_kzr/status/1248268624495689728), [5](https://twitter.com/_kzr/status/1249348597549682689).
* Simon Verstraete published a [tutorial](https://www.sidefx.com/tutorials/wfc-dungeon-generator/) about generating game levels for Unreal Engine 4 using the Houdini WFC tool: [0](https://www.youtube.com/watch?v=-5_FIqTDuzc), [1](https://www.youtube.com/watch?v=c06bSBYsFT8), [2](https://www.youtube.com/watch?v=u4NCs1F6zf8), [3](https://www.youtube.com/watch?v=YDpVUl213yo), [4](https://www.youtube.com/watch?v=ldcsvGuoW24).
* [Élie Michel](https://github.com/eliemichel) posted a [twitter thread](https://twitter.com/exppad/status/1267045322116734977) that explains the relationship between the overlapping and the tiled models.
* [Lionel Radisson](https://github.com/MAKIO135) published an interactive [Observable notebook](https://observablehq.com/@makio135/super-mario-wfc) that generates Mario and Zelda-like levels with the overlapping model: [1](https://twitter.com/MAKIO135/status/1271187284424040449), [2](https://twitter.com/MAKIO135/status/1268308728782045184), [3](https://twitter.com/MAKIO135/status/1271015222321561600), [4](https://twitter.com/MAKIO135/status/1271113760472694784).
* Łukasz Jakubowski, Maciej Kaszlewicz, Paweł Kroll and Stefan Radziuk [implemented](https://github.com/ic-pcg/waveFunctionCollapse) the tiled model in C.
* [Ivan Donchevskii](https://github.com/yvvan) published a [commercial Unreal Engine plugin](https://www.unrealengine.com/marketplace/en-US/product/procedural-environment-generator-wfc) based on the tiled model.
* [Ján Pernecký](https://github.com/janper) and [Ján Tóth](https://github.com/yanchith) published a [Grasshopper plugin](https://github.com/subdgtl/Monoceros) that extends the tiled model.
* Krystian Samp made a [single-file overlapping WFC library in C](https://github.com/krychu/wfc).
* [Gerald Krystian](https://github.com/amarcolina) made an [interactive tool](https://amarcolina.github.io/WFC-Explorer/) that explores the tiled model where tile adjacencies are induced from edge labels.
* DeepMind open-ended learning team [used](https://arxiv.org/abs/2107.12808) WFC to generate arenas for reinforcement learning agents.
* Oskar Stålberg [made](https://twitter.com/OskSta/status/1447483550257799171) an island generator that combines triangle and quad tiles and uses a custom observation heuristic that doesn't produce local minimums.
* [Boris the Brave](https://github.com/boristhebrave) [applied](https://www.boristhebrave.com/2021/11/08/infinite-modifying-in-blocks/) [Paul Merrell's](https://github.com/merrell42) modifying in blocks technique to the lazy generation of unbounded tile configurations. Marian Kleineberg has [implemented](https://marian42.de/article/infinite-wfc) this method into his [infinite city generator](https://github.com/marian42/wavefunctioncollapse).
* Vladimir Pleskonjić created a [single-header WFC library in C](https://github.com/vplesko/libwfc), accompanied by a CLI tool and a basic GUI tool.
* Pedro Carmo created a [Python tool](https://github.com/carmop/2D-sidescrolling-platformer-level-wfc) that uses WFC to generate 2D side scrolling platformer levels.

## Credits
Circles tileset is taken from [Mario Klingemann](https://twitter.com/quasimondo/status/778196128957403136). FloorPlan tileset is taken from [Lingdong Huang](https://github.com/LingDong-/ndwfc). Summer tiles were drawn by Hermann Hillmann. Cat overlapping sample is taken from the Nyan Cat video, Water + Forest + Mountains samples are taken from Ultima IV, 3Bricks sample is taken from Dungeon Crawl Stone Soup, Qud sample was made by Brian Bucklew, MagicOffice + Spirals samples - by rid5x, ColoredCity + Link + Link 2 + Mazelike + RedDot + SmileCity samples - by Arvi Teikari, Wall sample - by Arcaniax, NotKnot + Sand + Wrinkles samples - by Krystian Samp, Circle sample - by Noah Buddy. The rest of the examples and tilesets were made by me. Idea of generating integrated circuits was suggested to me by [Moonasaur](https://twitter.com/Moonasaur/status/759890746350731264) and their style was taken from Zachtronics' [Ruckingenur II](http://www.zachtronics.com/ruckingenur-ii/). Voxel models were rendered in [MagicaVoxel](http://ephtracy.github.io/).
<p align="center"><img alt="second collage" src="images/wfc-2.png"></p>
<p align="center"><img alt="voxel perspective" src="images/castle-3d.png"></p>
