using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using RgbColor = System.Drawing.Color;

namespace fCraft.Drawing {
    // Represents a palette of blocks, allowing matching RGB colors to their closest blocks equivalents
    public class BlockPalette : IEnumerable {
        // XN/YN/ZN are illuminant D65 tristimulus values
        const double XN = 95.047,
                     YN = 100.000,
                     ZN = 108.883,
                     // these constant are used in CIEXYZ -> CIELAB conversion
                     LinearThreshold = ( 6/29d )*( 6/29d )*( 6/29d ),
                     LinearMultiplier = ( 1/3d )*( 29/6d )*( 29/6d ),
                     LinearConstant = ( 4/29d );

        Dictionary<LabColor, Block[]> palette = new Dictionary<LabColor, Block[]>();
        public string Name { get; private set; }
        public int Layers { get; private set; }


        public BlockPalette( [NotNull] string name, int layers ) {
            if( name == null )
                throw new ArgumentNullException( "name" );
            Name = name;
            Layers = layers;
        }


        public void Add(RgbColor color, [NotNull] Block[] blocks) {
            Add( RgbToLab( color ), blocks );
        }

        protected void Add(LabColor color, Block[] blocks) {
            if( blocks == null ) {
                throw new ArgumentNullException("blocks");
            }
            if( blocks.Length != Layers ) {
                throw new ArgumentException("Number of blocks must match number the of layers.");
            }
            palette.Add(color, blocks);
        }


        public Block[] FindBestMatch(RgbColor color) {
            LabColor pixelColor = RgbToLab( color );
            double closestDistance = double.MaxValue;
            Block[] bestMatch = null;
            foreach( var pair in palette ) {
                double distance = ColorDifference( pixelColor, pair.Key );
                if( distance < closestDistance ) {
                    bestMatch = pair.Value;
                    closestDistance = distance;
                }
            }
            return bestMatch;
        }


        // CIE76 formula for Delta-E, over CIELAB color space
        static double ColorDifference( LabColor color1, LabColor color2 ) {
            return
                Math.Sqrt( ( color2.L - color1.L )*( color2.L - color1.L ) +
                           ( color2.a - color1.a )*( color2.a - color1.a ) +
                           ( color2.b - color1.b )*( color2.b - color1.b ) );
        }


        // Conversion from RGB to CIELAB, using illuminant D65.
        static LabColor RgbToLab(RgbColor color) {
            // RGB are assumed to be in [0...255] range
            // CIEXYZ coordinates are normalized to [0...1]
            double x = 0.00161746*color.R + 0.00140227*color.G + 0.000707541*color.B;
            double y = 0.000834004*color.R + 0.00280455*color.G + 0.000283016*color.B;
            double z = 0.0000758196*color.R + 0.000467424*color.G + 0.00372638*color.B;

            double xRatio = x/XN;
            double yRatio = y/YN;
            double zRatio = z/ZN;

            return new LabColor {
                // L is normalized to [0...100]
                L = 116*XyzToLab( yRatio ) - 16,
                a = 500*( XyzToLab( xRatio ) - XyzToLab( yRatio ) ),
                b = 200*( XyzToLab( yRatio ) - XyzToLab( zRatio ) )
            };
        }


        static double XyzToLab( double ratio ) {
            if( ratio > LinearThreshold ) {
                return Math.Pow( ratio, 1/3d );
            } else {
                return LinearMultiplier*ratio + LinearConstant;
            }
        }


        // we implement IEnumerable to be able to use the collection initializers
        IEnumerator IEnumerable.GetEnumerator() {
            return palette.GetEnumerator();
        }


        #region Standard Patterns
        // lazy initialization ftw

        [NotNull]
        public static BlockPalette Light {
            get {
                if( lightPalette == null ) {
                    lightPalette = DefineLight();
                }
                return lightPalette;
            }
        }
        static BlockPalette lightPalette;


        [NotNull]
        public static BlockPalette Dark {
            get {
                if( darkPalette == null ) {
                    darkPalette = DefineDark();
                }
                return darkPalette;
            }
        }
        static BlockPalette darkPalette;


        [NotNull]
        public static BlockPalette Layered {
            get {
                if( layeredPalette == null ) {
                    layeredPalette = DefineLayered();
                }
                return layeredPalette;
            }
        }
        static BlockPalette layeredPalette;


        [NotNull]
        public static BlockPalette GetPalette( StandardBlockPalettes palette ) {
            switch( palette ) {
                case StandardBlockPalettes.Light:
                    return Light;
                case StandardBlockPalettes.Dark:
                    return Dark;
                case StandardBlockPalettes.Layered:
                    return Layered;
                default:
                    throw new ArgumentOutOfRangeException( "palette" );
            }
        }

        [NotNull]
        static BlockPalette DefineLight() {
            return new BlockPalette( "Light", 1 ) {
                {RgbColor.FromArgb( 109, 80, 57 ), new[] {Block.Dirt}},
                {RgbColor.FromArgb( 176, 170, 130 ), new[] {Block.Sand}},
                {RgbColor.FromArgb( 179, 44, 44 ), new[] {Block.Red}},
                {RgbColor.FromArgb( 179, 111, 44 ), new[] {Block.Orange}},
                {RgbColor.FromArgb( 179, 179, 44 ), new[] {Block.Yellow}},
                {RgbColor.FromArgb( 109, 179, 44 ), new[] {Block.Lime}},
                {RgbColor.FromArgb( 44, 179, 44 ), new[] {Block.Green}},
                {RgbColor.FromArgb( 44, 179, 111 ), new[] {Block.Teal}},
                {RgbColor.FromArgb( 44, 179, 179 ), new[] {Block.Aqua}},
                {RgbColor.FromArgb( 86, 132, 179 ), new[] {Block.Cyan}},
                {RgbColor.FromArgb( 99, 99, 180 ), new[] {Block.Blue}},
                {RgbColor.FromArgb( 111, 44, 180 ), new[] {Block.Indigo}},
                {RgbColor.FromArgb( 141, 62, 179 ), new[] {Block.Violet}},
                {RgbColor.FromArgb( 180, 44, 180 ), new[] {Block.Magenta}},
                {RgbColor.FromArgb( 179, 44, 111 ), new[] {Block.Pink}},
                {RgbColor.FromArgb( 64, 64, 64 ), new[] {Block.Black}},
                {RgbColor.FromArgb( 118, 118, 118 ), new[] {Block.Gray}},
                {RgbColor.FromArgb( 179, 179, 179 ), new[] {Block.White}},
                {RgbColor.FromArgb( 21, 19, 29 ), new[] {Block.Obsidian}}
            };
        }

        [NotNull]
        static BlockPalette DefineDark() {
            return new BlockPalette( "Dark", 1 ) {
                {RgbColor.FromArgb( 67, 50, 37 ), new[] {Block.Dirt}},
                {RgbColor.FromArgb( 108, 104, 80 ), new[] {Block.Sand}},
                {RgbColor.FromArgb( 109, 28, 28 ), new[] {Block.Red}},
                {RgbColor.FromArgb( 110, 70, 31 ), new[] {Block.Orange}},
                {RgbColor.FromArgb( 109, 109, 29 ), new[] {Block.Yellow}},
                {RgbColor.FromArgb( 68, 109, 29 ), new[] {Block.Lime}},
                {RgbColor.FromArgb( 28, 109, 31 ), new[] {Block.Green}},
                {RgbColor.FromArgb( 28, 109, 69 ), new[] {Block.Teal}},
                {RgbColor.FromArgb( 28, 109, 108 ), new[] {Block.Aqua}},
                {RgbColor.FromArgb( 53, 81, 109 ), new[] {Block.Cyan}},
                {RgbColor.FromArgb( 61, 61, 109 ), new[] {Block.Blue}},
                {RgbColor.FromArgb( 68, 28, 109 ), new[] {Block.Indigo}},
                {RgbColor.FromArgb( 87, 40, 110 ), new[] {Block.Violet}},
                {RgbColor.FromArgb( 109, 28, 110 ), new[] {Block.Magenta}},
                {RgbColor.FromArgb( 109, 29, 69 ), new[] {Block.Pink}},
                {RgbColor.FromArgb( 41, 41, 41 ), new[] {Block.Black}},
                {RgbColor.FromArgb( 72, 72, 72 ), new[] {Block.Gray}},
                {RgbColor.FromArgb( 109, 109, 109 ), new[] {Block.White}},
                {RgbColor.FromArgb( 15, 14, 20 ), new[] {Block.Obsidian}}
            };
        }

        [NotNull]
        static BlockPalette DefineLayered() {
            BlockPalette palette = new BlockPalette( "Layered", 2 );
            foreach( var pair in Light.palette ) {
                palette.Add( pair.Key, new[] {pair.Value[0], Block.Undefined} );
            }
            foreach( var pair in Dark.palette ) {
                palette.Add( pair.Key, new[] {Block.Air, pair.Value[0]} );
            }
            return palette;
        }

        #endregion


        protected struct LabColor {
            public double L, a, b;
        }
    }


    public enum StandardBlockPalettes {
        Light, // 1-layer standard blocks, lit
        Dark, // 1-layer standard blocks, shadowed
        Layered // 2-layer standard blocks
        // TODO: CPE patterns
    }
}