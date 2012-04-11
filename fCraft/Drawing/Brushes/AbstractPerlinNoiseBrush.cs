// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Linq;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    public abstract class AbstractPerlinNoiseBrush : IBrushInstance {
        public int Seed { get; set; }
        public float Coverage { get; set; }
        // ReSharper disable MemberCanBeProtected.Global
        public float Frequency { get; set; }
        // ReSharper restore MemberCanBeProtected.Global
        public int Octaves { get; set; }
        public float Persistence { get; set; }

        protected Block[] Blocks { get; set; }
        protected int[] BlockRatios { get; set; }

        float[] computedThresholds;
        float normMultiplier, normConstant;
        PerlinNoise3D noise3D;

        static readonly object SeedGenLock = new object();
        static readonly Random SeedGenerator = new Random();

        protected AbstractPerlinNoiseBrush() {
            lock( SeedGenLock ) {
                Seed = SeedGenerator.Next();
            }
            Blocks = new Block[0];
            BlockRatios = new int[0];
            Coverage = 0.5f;
            Persistence = 0.75f;
            Frequency = 0.08f;
            Octaves = 3;
        }

        protected AbstractPerlinNoiseBrush( Block oneBlock, int ratio )
            : this() {
            Blocks = new[] { oneBlock, Block.Undefined };
            BlockRatios = new[] { ratio, 1 };
        }

        protected AbstractPerlinNoiseBrush( Block[] blocks, int[] ratios )
            : this() {
            Blocks = blocks;
            BlockRatios = ratios;
        }


        protected AbstractPerlinNoiseBrush( [NotNull] AbstractPerlinNoiseBrush other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            Blocks = other.Blocks;
            BlockRatios = other.BlockRatios;
            Seed = other.Seed;
            Coverage = other.Coverage;
            Frequency = other.Frequency;
            Octaves = other.Octaves;
            Persistence = other.Persistence;
        }


        public virtual bool Begin( [NotNull] Player player, [NotNull] DrawOperation op ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( op == null ) throw new ArgumentNullException( "op" );

            if( op.Bounds.Volume > 32 * 32 * 32 ) {
                player.Message( "{0} brush: Preparing, please wait...", Brush.Factory.Name );
            }

            noise3D = new PerlinNoise3D( new Random( Seed ) ) {
                Amplitude = 1,
                Frequency = Frequency,
                Octaves = Octaves,
                Persistence = Persistence
            };

            // generate and normalize the raw (float) data
            float[, ,] rawData = new float[op.Bounds.Width, op.Bounds.Length, op.Bounds.Height];
            for( int x = 0; x < op.Bounds.Width; x++ ) {
                for( int y = 0; y < op.Bounds.Length; y++ ) {
                    for( int z = 0; z < op.Bounds.Height; z++ ) {
                        rawData[x, y, z] = noise3D.Compute( x, y, z );
                    }
                }
            }
            Noise.Normalize( rawData, out normMultiplier, out normConstant );
            if( MapAllValues( rawData ) ) {
                Noise.Normalize( rawData, out normMultiplier, out normConstant );
            }

            // create a mapping of raw data to blocks
            int totalBlocks = BlockRatios.Sum();
            int blocksSoFar = BlockRatios[0];
            computedThresholds = new float[Blocks.Length];
            computedThresholds[0] = 0;
            for( int i = 1; i < Blocks.Length; i++ ) {
                float desiredCoverage = blocksSoFar / (float)totalBlocks;
                computedThresholds[i] = Noise.FindThreshold( rawData, desiredCoverage );
                blocksSoFar += BlockRatios[i];
            }
            return true;
        }


        public virtual Block NextBlock( [NotNull] DrawOperation op ) {
            if( op == null ) throw new ArgumentNullException( "op" );
            Vector3I relativeCoords = op.Coords - op.Bounds.MinVertex;
            float value = noise3D.Compute( relativeCoords.X, relativeCoords.Y, relativeCoords.Z );

            // normalize value
            value = value * normMultiplier + normConstant;

            // apply child transform
            value = MapValue( value );

            // find the right block type for given value
            for( int i = 1; i < Blocks.Length; i++ ) {
                if( computedThresholds[i] > value ) {
                    return Blocks[i - 1];
                }
            }
            return Blocks[Blocks.Length - 1];
        }


        protected abstract float MapValue( float rawValue );


        protected abstract bool MapAllValues( float[, ,] rawValues );


        public virtual void End() { }


        public abstract IBrush Brush { get; }


        public abstract string InstanceDescription { get; }


        public bool HasAlternateBlock {
            get { return false; }
        }
    }
}