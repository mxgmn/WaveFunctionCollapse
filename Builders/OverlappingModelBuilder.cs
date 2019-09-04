using System;
using System.Collections.Generic;
using System.Text;

namespace WaveFunctionCollapse.Builders
{
    /// <summary>
    /// Class to improve the creation of OverlappingModels
    /// </summary>
    public class OverlappingModelBuilder
    {
        private string Name { get; set; }
        private int N { get; set; }
        private int Width { get; set; }
        private int Height { get; set; }
        private bool PeriodicInput { get; set; }
        private bool PeriodicOutput { get; set; }
        private int Symmetry { get; set; }
        private int Ground { get; set; }

        public OverlappingModelBuilder WithName(string name)
        {
            Name = name;
            return this;
        }

        public OverlappingModelBuilder WithN(int n)
        {
            N = n;
            return this;
        }

        public OverlappingModelBuilder WithWidth(int width)
        {
            Width = width;
            return this;
        }

        public OverlappingModelBuilder WithHeight(int height)
        {
            Height = height;
            return this;
        }

        public OverlappingModelBuilder WithPeriodicInput(bool periodicInput)
        {
            PeriodicInput = periodicInput;
            return this;
        }

        public OverlappingModelBuilder WithPeriodicOutput(bool periodicOutput)
        {
            PeriodicOutput = periodicOutput;
            return this;
        }

        public OverlappingModelBuilder WithSymmetry(int symmetry)
        {
            Symmetry = symmetry;
            return this;
        }

        public OverlappingModelBuilder WithGround(int ground)
        {
            Ground = ground;
            return this;
        }

        public OverlappingModel Build()
        {
            return new OverlappingModel(Name, N, Width, Height, PeriodicInput, PeriodicOutput, Symmetry, Ground);
        }
    }
}
