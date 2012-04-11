// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using JetBrains.Annotations;

namespace fCraft {

    /// <summary> Interpolation mode for perlin noise. </summary>
    public enum NoiseInterpolationMode {
        /// <summary> Bilinear (LERP) interpolation (fastest). </summary>
        Linear,

        /// <summary> Cosine interpolation (fast). </summary>
        Cosine,

        /// <summary> Bicubic interpolation (slow). </summary>
        Bicubic,

        /// <summary> Spline interpolation (slowest). </summary>
        Spline
    }


    /// <summary> Class for generating and filtering 2D noise, extensively used by MapGenerator. </summary>
    public sealed class Noise {
        public readonly int Seed;
        public readonly NoiseInterpolationMode InterpolationMode;

        public Noise( int seed, NoiseInterpolationMode interpolationMode ) {
            Seed = seed;
            InterpolationMode = interpolationMode;
        }


        public static float InterpolateLinear( float v0, float v1, float x ) {
            return v0 * (1 - x) + v1 * x;
        }


        public static float InterpolateLinear( float v00, float v01, float v10, float v11, float x, float y ) {
            return InterpolateLinear( InterpolateLinear( v00, v10, x ),
                                      InterpolateLinear( v01, v11, x ),
                                      y );
        }


        public static float InterpolateCosine( float v0, float v1, float x ) {
            double f = (1 - Math.Cos( x * Math.PI )) * .5;
            return (float)(v0 * (1 - f) + v1 * f);
        }

        public static float InterpolateCosine( float v00, float v01, float v10, float v11, float x, float y ) {
            return InterpolateCosine( InterpolateCosine( v00, v10, x ),
                                      InterpolateCosine( v01, v11, x ),
                                      y );
        }


        // Cubic and Catmull-Rom Spline interpolation methods by Paul Bourke
        // http://local.wasp.uwa.edu.au/~pbourke/miscellaneous/interpolation/
        public static float InterpolateCubic( float v0, float v1, float v2, float v3, float mu ) {
            float mu2 = mu * mu;
            float a0 = v3 - v2 - v0 + v1;
            float a1 = v0 - v1 - a0;
            float a2 = v2 - v0;
            float a3 = v1;
            return (a0 * mu * mu2 + a1 * mu2 + a2 * mu + a3);
        }


        public static float InterpolateSpline( float v0, float v1, float v2, float v3, float mu ) {
            float mu2 = mu * mu;
            float a0 = -0.5f * v0 + 1.5f * v1 - 1.5f * v2 + 0.5f * v3;
            float a1 = v0 - 2.5f * v1 + 2 * v2 - 0.5f * v3;
            float a2 = -0.5f * v0 + 0.5f * v2;
            float a3 = v1;
            return (a0 * mu * mu2 + a1 * mu2 + a2 * mu + a3);
        }


        public float StaticNoise( int x, int y ) {
            int n = Seed + x + y * short.MaxValue;
            n = (n << 13) ^ n;
            return (float)(1.0 - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7FFFFFFF) / 1073741824d);
        }

        public float StaticNoise( int x, int y, int z ) {
            int n = Seed + x + y * 1625 + z * 2642245;
            n = (n << 13) ^ n;
            return (float)(1.0 - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7FFFFFFF) / 1073741824d);
        }


        readonly float[,] points = new float[4, 4];
        public float InterpolatedNoise( float x, float y ) {
            int xInt = (int)Math.Floor( x );
            float xFloat = x - xInt;

            int yInt = (int)Math.Floor( y );
            float yFloat = y - yInt;

            float p00, p01, p10, p11;

            switch( InterpolationMode ) {
                case NoiseInterpolationMode.Linear:
                    p00 = StaticNoise( xInt, yInt );
                    p01 = StaticNoise( xInt, yInt + 1 );
                    p10 = StaticNoise( xInt + 1, yInt );
                    p11 = StaticNoise( xInt + 1, yInt + 1 );
                    return InterpolateLinear( InterpolateLinear( p00, p10, xFloat ), InterpolateLinear( p01, p11, xFloat ), yFloat );

                case NoiseInterpolationMode.Cosine:
                    p00 = StaticNoise( xInt, yInt );
                    p01 = StaticNoise( xInt, yInt + 1 );
                    p10 = StaticNoise( xInt + 1, yInt );
                    p11 = StaticNoise( xInt + 1, yInt + 1 );
                    return InterpolateCosine( InterpolateCosine( p00, p10, xFloat ), InterpolateCosine( p01, p11, xFloat ), yFloat );

                case NoiseInterpolationMode.Bicubic:
                    for( int xOffset = -1; xOffset < 3; xOffset++ ) {
                        for( int yOffset = -1; yOffset < 3; yOffset++ ) {
                            points[xOffset + 1, yOffset + 1] = StaticNoise( xInt + xOffset, yInt + yOffset );
                        }
                    }
                    p00 = InterpolateCubic( points[0, 0], points[1, 0], points[2, 0], points[3, 0], xFloat );
                    p01 = InterpolateCubic( points[0, 1], points[1, 1], points[2, 1], points[3, 1], xFloat );
                    p10 = InterpolateCubic( points[0, 2], points[1, 2], points[2, 2], points[3, 2], xFloat );
                    p11 = InterpolateCubic( points[0, 3], points[1, 3], points[2, 3], points[3, 3], xFloat );
                    return InterpolateCubic( p00, p01, p10, p11, yFloat );

                case NoiseInterpolationMode.Spline:
                    for( int xOffset = -1; xOffset < 3; xOffset++ ) {
                        for( int yOffset = -1; yOffset < 3; yOffset++ ) {
                            points[xOffset + 1, yOffset + 1] = StaticNoise( xInt + xOffset, yInt + yOffset );
                        }
                    }
                    p00 = InterpolateSpline( points[0, 0], points[1, 0], points[2, 0], points[3, 0], xFloat );
                    p01 = InterpolateSpline( points[0, 1], points[1, 1], points[2, 1], points[3, 1], xFloat );
                    p10 = InterpolateSpline( points[0, 2], points[1, 2], points[2, 2], points[3, 2], xFloat );
                    p11 = InterpolateSpline( points[0, 3], points[1, 3], points[2, 3], points[3, 3], xFloat );
                    return InterpolateSpline( p00, p01, p10, p11, yFloat );

                default:
                    throw new ArgumentException();
            }
        }

        //readonly float[, ,] points3D = new float[4, 4, 4];
        public float InterpolatedNoise( float x, float y, float z ) {
            int xInt = (int)Math.Floor( x );
            float xFloat = x - xInt;

            int yInt = (int)Math.Floor( y );
            float yFloat = y - yInt;

            int zInt = (int)Math.Floor( z );
            float zFloat = z - zInt;

            float p000, p001, p010, p011,
                  p100, p101, p110, p111;

            switch( InterpolationMode ) {
                case NoiseInterpolationMode.Linear:
                    p000 = StaticNoise( xInt, yInt, zInt );
                    p001 = StaticNoise( xInt, yInt, zInt + 1 );
                    p010 = StaticNoise( xInt, yInt + 1, zInt );
                    p011 = StaticNoise( xInt, yInt + 1, zInt + 1 );
                    p100 = StaticNoise( xInt+1, yInt, zInt );
                    p101 = StaticNoise( xInt + 1, yInt, zInt + 1 );
                    p110 = StaticNoise( xInt + 1, yInt + 1, zInt );
                    p111 = StaticNoise( xInt + 1, yInt + 1, zInt + 1 );
                    return InterpolateLinear(
                        InterpolateLinear( InterpolateLinear( p000, p100, xFloat ), InterpolateLinear( p010, p110, xFloat ), yFloat ),
                        InterpolateLinear( InterpolateLinear( p001, p101, xFloat ), InterpolateLinear( p011, p111, xFloat ), yFloat ),
                        zFloat );

                case NoiseInterpolationMode.Cosine:
                    p000 = StaticNoise( xInt, yInt, zInt );
                    p001 = StaticNoise( xInt, yInt, zInt + 1 );
                    p010 = StaticNoise( xInt, yInt + 1, zInt );
                    p011 = StaticNoise( xInt, yInt + 1, zInt + 1 );
                    p100 = StaticNoise( xInt + 1, yInt, zInt );
                    p101 = StaticNoise( xInt + 1, yInt, zInt + 1 );
                    p110 = StaticNoise( xInt + 1, yInt + 1, zInt );
                    p111 = StaticNoise( xInt + 1, yInt + 1, zInt + 1 );
                    return InterpolateCosine(
                        InterpolateCosine( InterpolateCosine( p000, p100, xFloat ), InterpolateCosine( p010, p110, xFloat ), yFloat ),
                        InterpolateCosine( InterpolateCosine( p001, p101, xFloat ), InterpolateCosine( p011, p111, xFloat ), yFloat ),
                        zFloat );
                    /*
                case NoiseInterpolationMode.Bicubic: TODO
                    for( int xOffset = -1; xOffset < 3; xOffset++ ) {
                        for( int yOffset = -1; yOffset < 3; yOffset++ ) {
                            points[xOffset + 1, yOffset + 1] = StaticNoise( xInt + xOffset, yInt + yOffset );
                        }
                    }
                    p00 = InterpolateCubic( points[0, 0], points[1, 0], points[2, 0], points[3, 0], xFloat );
                    p01 = InterpolateCubic( points[0, 1], points[1, 1], points[2, 1], points[3, 1], xFloat );
                    p10 = InterpolateCubic( points[0, 2], points[1, 2], points[2, 2], points[3, 2], xFloat );
                    p11 = InterpolateCubic( points[0, 3], points[1, 3], points[2, 3], points[3, 3], xFloat );
                    return InterpolateCubic( p00, p01, p10, p11, yFloat );

                case NoiseInterpolationMode.Spline:
                    for( int xOffset = -1; xOffset < 3; xOffset++ ) {
                        for( int yOffset = -1; yOffset < 3; yOffset++ ) {
                            points[xOffset + 1, yOffset + 1] = StaticNoise( xInt + xOffset, yInt + yOffset );
                        }
                    }
                    p00 = InterpolateSpline( points[0, 0], points[1, 0], points[2, 0], points[3, 0], xFloat );
                    p01 = InterpolateSpline( points[0, 1], points[1, 1], points[2, 1], points[3, 1], xFloat );
                    p10 = InterpolateSpline( points[0, 2], points[1, 2], points[2, 2], points[3, 2], xFloat );
                    p11 = InterpolateSpline( points[0, 3], points[1, 3], points[2, 3], points[3, 3], xFloat );
                    return InterpolateSpline( p00, p01, p10, p11, yFloat );
                    */
                default:
                    throw new ArgumentException();
            }
        }


        public float PerlinNoise( float x, float y, int startOctave, int endOctave, float decay ) {
            float total = 0;

            float frequency = (float)Math.Pow( 2, startOctave );
            float amplitude = (float)Math.Pow( decay, startOctave );

            for( int n = startOctave; n <= endOctave; n++ ) {
                total += InterpolatedNoise( x * frequency + frequency, y * frequency + frequency ) * amplitude;
                frequency *= 2;
                amplitude *= decay;
            }
            return total;
        }

        public float PerlinNoise( float x, float y, float z, int startOctave, int endOctave, float decay ) {
            float total = 0;

            float frequency = (float)Math.Pow( 2, startOctave );
            float amplitude = (float)Math.Pow( decay, startOctave );

            for( int n = startOctave; n <= endOctave; n++ ) {
                total +=
                    InterpolatedNoise( x * frequency + frequency, y * frequency + frequency, z * frequency + frequency ) *
                    amplitude;
                frequency *= 2;
                amplitude *= decay;
            }
            return total;
        }


        public void PerlinNoise( [NotNull] float[,] map, int startOctave, int endOctave, float decay, int offsetX, int offsetY ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            float maxDim = 1f / Math.Max( map.GetLength( 0 ), map.GetLength( 1 ) );
            for( int x = map.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = map.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    map[x, y] += PerlinNoise( x * maxDim + offsetX, y * maxDim + offsetY, startOctave, endOctave, decay );
                }
            }
        }


        public void PerlinNoise( [NotNull] float[, ,] map, int startOctave, int endOctave, float decay, int offsetX, int offsetY, int offsetZ ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            float maxDim = 1f / Math.Max( map.GetLength( 0 ), Math.Max( map.GetLength( 2 ), map.GetLength( 1 ) ) );
            for( int x = map.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = map.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    for( int z = map.GetLength( 2 ) - 1; z >= 0; z-- ) {
                        map[x, y, z] += PerlinNoise( x * maxDim + offsetX, y * maxDim + offsetY, z * maxDim + offsetZ,
                                                     startOctave, endOctave, decay );
                    }
                }
            }
        }


        #region Normalize

        public static void Normalize( float[,] map ) {
            Normalize( map, 0, 1 );
        }


        public static void Normalize( float[,,] map ) {
            Normalize( map, 0, 1 );
        }


        public static void Normalize( float[, ,] map, out float multiplier, out float constant ) {
            CalculateNormalizationParams( map, out multiplier, out constant, 0f, 1f );
        }

        public unsafe static void CalculateNormalizationParams( float[, ,] map, out float multiplier, out float constant, float low, float high ) {
            fixed( float* ptr = map ) {
                float min = float.MaxValue,
                      max = float.MinValue;

                for( int i = 0; i < map.Length; i++ ) {
                    min = Math.Min( min, ptr[i] );
                    max = Math.Max( max, ptr[i] );
                }

                multiplier = (high - low) / (max - min);
                constant = -min * (high - low) / (max - min) + low;

                for( int i = 0; i < map.Length; i++ ) {
                    ptr[i] = ptr[i] * multiplier + constant;
                }
            }
        }


        public unsafe static void Normalize( float[,] map, float low, float high ) {
            int length = map.GetLength( 0 ) * map.GetLength( 1 );
            fixed( float* ptr = map ) {
                Normalize( ptr, length, low, high );
            }
        }


        public unsafe static void Normalize( float[,,] map, float low, float high ) {
            int length = map.GetLength( 0 ) * map.GetLength( 1 ) * map.GetLength( 2 );
            fixed( float* ptr = map ) {
                Normalize( ptr, length, low, high );
            }
        }


        unsafe static void Normalize( float* ptr, int length, float low, float high ) {
            float min = float.MaxValue,
                  max = float.MinValue;

                for( int i = 0; i < length; i++ ) {
                    min = Math.Min( min, ptr[i] );
                    max = Math.Max( max, ptr[i] );
                }

                float multiplier = (high - low) / (max - min);
                float constant = -min * (high - low) / (max - min) + low;

                for( int i = 0; i < length; i++ ) {
                    ptr[i] = ptr[i] * multiplier + constant;
                }
        }

        #endregion


        // assumes normalized input
        public unsafe static void Marble( [NotNull] float[,] map ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            fixed( float* ptr = map ) {
                for( int i = 0; i < map.Length; i++ ) {
                    ptr[i] = Math.Abs( ptr[i] * 2 - 1 );
                }
            }
        }


        public unsafe static void Marble( [NotNull] float[,,] map ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            fixed( float* ptr = map ) {
                for( int i = 0; i < map.Length; i++ ) {
                    ptr[i] = Math.Abs( ptr[i] * 2 - 1 );
                }
            }
        }


        // assumes normalized input
        public unsafe static void Blend( [NotNull] float[,] data1, [NotNull] float[,] data2, [NotNull] float[,] blendMap ) {
            if( data1 == null ) throw new ArgumentNullException( "data1" );
            if( data2 == null ) throw new ArgumentNullException( "data2" );
            if( blendMap == null ) throw new ArgumentNullException( "blendMap" );
            fixed( float* ptr1 = data1, ptr2 = data2, ptrBlend = blendMap ) {
                for( int i = 0; i < data1.Length; i++ ) {
                    ptr1[i] += ptr1[i] * ptrBlend[i] + ptr2[i] * (1 - ptrBlend[i]);
                }
            }
        }


        public unsafe static void Add( [NotNull] float[,] data1, [NotNull] float[,] data2 ) {
            if( data1 == null ) throw new ArgumentNullException( "data1" );
            if( data2 == null ) throw new ArgumentNullException( "data2" );
            if( data1.GetLength( 0 ) != data2.GetLength( 0 ) ||
                data1.GetLength( 1 ) != data2.GetLength( 1 ) ) {
                throw new ArgumentException( "data1 and data2 dimension mismatch" );
            }
            fixed( float* ptr1 = data1, ptr2 = data2 ) {
                for( int i = 0; i < data1.Length; i++ ) {
                    ptr1[i] += ptr2[i];
                }
            }
        }


        public static void ApplyBias( [NotNull] float[,] data, float c00, float c01, float c10, float c11, float midpoint ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            float maxX = 2f / data.GetLength( 0 );
            float maxY = 2f / data.GetLength( 1 );
            int offsetX = data.GetLength( 0 ) / 2;
            int offsetY = data.GetLength( 1 ) / 2;

            for( int x = offsetX - 1; x >= 0; x-- ) {
                for( int y = offsetY - 1; y >= 0; y-- ) {
                    data[x, y] += InterpolateCosine( c00, (c00 + c01) / 2, (c00 + c10) / 2, midpoint, x * maxX, y * maxY );
                    data[x + offsetX, y] += InterpolateCosine( (c00 + c10) / 2, midpoint, c10, (c11 + c10) / 2, x * maxX, y * maxY );
                    data[x, y + offsetY] += InterpolateCosine( (c00 + c01) / 2, c01, midpoint, (c01 + c11) / 2, x * maxX, y * maxY );
                    data[x + offsetX, y + offsetY] += InterpolateCosine( midpoint, (c01 + c11) / 2, (c11 + c10) / 2, c11, x * maxX, y * maxY );
                }
            }
        }


        // assumes normalized input
        public unsafe static void ScaleAndClip( [NotNull] float[,] data, float steepness ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            fixed( float* ptr = data ) {
                for( int i = 0; i < data.Length; i++ ) {
                    ptr[i] = Math.Min( 1, Math.Max( 0, ptr[i] * steepness * 2 - steepness ) );
                }
            }
        }


        public unsafe static void Invert( [NotNull] float[,] data ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            fixed( float* ptr = data ) {
                for( int i = 0; i < data.Length; i++ ) {
                    ptr[i] = 1 - ptr[i];
                }
            }
        }


        const float BoxBlurDivisor = 1 / 23f;
        public static float[,] BoxBlur( [NotNull] float[,] heightmap ) {
            if( heightmap == null ) throw new ArgumentNullException( "heightmap" );
            float[,] output = new float[heightmap.GetLength( 0 ), heightmap.GetLength( 1 )];
            for( int x = heightmap.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = heightmap.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    if( (x == 0) || (y == 0) || (x == heightmap.GetLength( 0 ) - 1) || (y == heightmap.GetLength( 1 ) - 1) ) {
                        output[x, y] = heightmap[x, y];
                    } else {
                        output[x, y] = (heightmap[x - 1, y - 1] * 2 + heightmap[x - 1, y] * 3 + heightmap[x - 1, y + 1] * 2 +
                                        heightmap[x, y - 1] * 3 + heightmap[x, y] * 3 + heightmap[x, y + 1] * 3 +
                                        heightmap[x + 1, y - 1] * 2 + heightmap[x + 1, y] * 3 + heightmap[x + 1, y + 1] * 2) * BoxBlurDivisor;
                    }
                }
            }
            return output;
        }


        const float GaussianBlurDivisor = 1 / 273f;
        public static float[,] GaussianBlur5X5( [NotNull] float[,] heightmap ) {
            if( heightmap == null ) throw new ArgumentNullException( "heightmap" );
            float[,] output = new float[heightmap.GetLength( 0 ), heightmap.GetLength( 1 )];
            for( int x = heightmap.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = heightmap.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    if( (x < 2) || (y < 2) || (x > heightmap.GetLength( 0 ) - 3) || (y > heightmap.GetLength( 1 ) - 3) ) {
                        output[x, y] = heightmap[x, y];
                    } else {
                        output[x, y] = (heightmap[x - 2, y - 2] + heightmap[x - 1, y - 2] * 4 + heightmap[x, y - 2] * 7 + heightmap[x + 1, y - 2] * 4 + heightmap[x + 2, y - 2] +
                                        heightmap[x - 1, y - 1] * 4 + heightmap[x - 1, y - 1] * 16 + heightmap[x, y - 1] * 26 + heightmap[x + 1, y - 1] * 16 + heightmap[x + 2, y - 1] * 4 +
                                        heightmap[x - 2, y] * 7 + heightmap[x - 1, y] * 26 + heightmap[x, y] * 41 + heightmap[x + 1, y] * 26 + heightmap[x + 2, y] * 7 +
                                        heightmap[x - 2, y + 1] * 4 + heightmap[x - 1, y + 1] * 16 + heightmap[x, y + 1] * 26 + heightmap[x + 1, y + 1] * 16 + heightmap[x + 2, y + 1] * 4 +
                                        heightmap[x - 2, y + 2] + heightmap[x - 1, y + 2] * 4 + heightmap[x, y + 2] * 7 + heightmap[x + 1, y + 2] * 4 + heightmap[x + 2, y + 2]) * GaussianBlurDivisor;
                    }
                }
            }
            return output;
        }


        public static float[,] CalculateSlope( [NotNull] float[,] heightmap ) {
            if( heightmap == null ) throw new ArgumentNullException( "heightmap" );
            float[,] output = new float[heightmap.GetLength( 0 ), heightmap.GetLength( 1 )];

            for( int x = heightmap.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = heightmap.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    if( (x == 0) || (y == 0) || (x == heightmap.GetLength( 0 ) - 1) || (y == heightmap.GetLength( 1 ) - 1) ) {
                        output[x, y] = 0;
                    } else {
                        output[x, y] = (Math.Abs( heightmap[x, y - 1] - heightmap[x, y] ) * 3 +
                                        Math.Abs( heightmap[x, y + 1] - heightmap[x, y] ) * 3 +
                                        Math.Abs( heightmap[x - 1, y] - heightmap[x, y] ) * 3 +
                                        Math.Abs( heightmap[x + 1, y] - heightmap[x, y] ) * 3 +
                                        Math.Abs( heightmap[x - 1, y - 1] - heightmap[x, y] ) * 2 +
                                        Math.Abs( heightmap[x + 1, y - 1] - heightmap[x, y] ) * 2 +
                                        Math.Abs( heightmap[x - 1, y + 1] - heightmap[x, y] ) * 2 +
                                        Math.Abs( heightmap[x + 1, y + 1] - heightmap[x, y] ) * 2) / 20f;
                    }
                }
            }

            return output;
        }



        const int ThresholdSearchPasses = 10;

        public unsafe static float FindThreshold( [NotNull] float[,] data, float desiredCoverage ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            if( desiredCoverage == 0 ) return 0;
            if( desiredCoverage == 1 ) return 1;
            float threshold = 0.5f;
            fixed( float* ptr = data ) {
                for( int i = 0; i < ThresholdSearchPasses; i++ ) {
                    float coverage = CalculateCoverage( ptr, data.Length, threshold );
                    if( coverage > desiredCoverage ) {
                        threshold = threshold - 1 / (float)(4 << i);
                    } else {
                        threshold = threshold + 1 / (float)(4 << i);
                    }
                }
            }
            return threshold;
        }


        public unsafe static float FindThreshold( [NotNull] float[,,] data, float desiredCoverage ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            if( desiredCoverage == 0 ) return 0;
            if( desiredCoverage == 1 ) return 1;
            float threshold = 0.5f;
            fixed( float* ptr = data ) {
                for( int i = 0; i < ThresholdSearchPasses; i++ ) {
                    float coverage = CalculateCoverage( ptr, data.Length, threshold );
                    if( coverage > desiredCoverage ) {
                        threshold = threshold - 1 / (float)(4 << i);
                    } else {
                        threshold = threshold + 1 / (float)(4 << i);
                    }
                }
            }
            return threshold;
        }


        public unsafe static float CalculateCoverage( [NotNull] float* data, int length, float threshold ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            int coveredVoxels = 0;
            float* end = data + length;
            while( data < end ) {
                if( *data < threshold ) coveredVoxels++;
                data++;
            }
            return coveredVoxels / (float)length;
        }
    }
}