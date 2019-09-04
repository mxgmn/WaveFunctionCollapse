namespace WaveFunctionCollapse.Builders
{
    /// <summary>
    /// Class to improve the creation of SimpleTiledModels
    /// </summary>
    public class SimpleTiledModelBuilder
    {
        private string Name { get; set; }
        private string SubsetName { get; set; }
        private int Width { get; set; }
        private int Height { get; set; }
        private bool Periodic { get; set; }
        private bool Black { get; set; }

        public SimpleTiledModelBuilder WithName(string name)
        {
            Name = name;
            return this;
        }

        public SimpleTiledModelBuilder WithSubsetName(string subsetName)
        {
            SubsetName = subsetName;
            return this;
        }

        public SimpleTiledModelBuilder WithWidth(int width)
        {
            Width = width;
            return this;
        }

        public SimpleTiledModelBuilder WithHeight(int height)
        {
            Height = height;
            return this;
        }

        public SimpleTiledModelBuilder WithPeriodic(bool periodic)
        {
            Periodic = periodic;
            return this;
        }

        public SimpleTiledModelBuilder WithBlack(bool black)
        {
            Black = black;
            return this;
        }

        public SimpleTiledModel Build()
        {
            return new SimpleTiledModel(Name, SubsetName, Width, Height, Periodic, Black);
        }
    }
}
