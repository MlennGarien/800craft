using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    // Represents a palette of blocks, allowing matching RGB colors to their closest blocks equivalents
    class BlockPalette : IEnumerable {
        // XN/YN/ZN are illuminant D65 tristimulus values
        const double XN = 95.047,
                     YN = 100.000,
                     ZN = 108.883,
                     // these constant are used in CIEXYZ -> CIELAB conversion
                     LinearThreshold = (6 / 29d) * (6 / 29d) * (6 / 29d),
                     LinearMultiplier = (1 / 3d) * (29 / 6d) * (29 / 6d),
                     LinearConstant = (4 / 29d);

        Dictionary<LabColor, Block[]> palette = new Dictionary<LabColor, Block[]>();
        public string Name { get; private set; }
        public int Layers { get; private set; }


        public BlockPalette( [NotNull] string name, int layers) {
            if( name == null )
                throw new ArgumentNullException( "name" );
            Name = name;
            Layers = layers;
        }


        public void Add(System.Drawing.Color color, [NotNull] Block[] blocks) {
            if( blocks == null ) {
                throw new ArgumentNullException( "blocks" );
            }
            if( blocks.Length != Layers ) {
                throw new ArgumentException( "Number of blocks must match number the of layers." );
            }
            palette.Add( RgbToLab( color ), blocks );
        }


        public Block[] FindBestMatch( System.Drawing.Color color ) {
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
        static LabColor RgbToLab(System.Drawing.Color color) {
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

        struct LabColor {
            public double L, a, b;
        }
    }
}