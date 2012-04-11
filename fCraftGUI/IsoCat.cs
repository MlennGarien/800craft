// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using fCraftGUI.Properties;
using JetBrains.Annotations;

namespace fCraft.GUI {

    /// <summary> Drawing/clipping mode of IsoCat map renderer. </summary>
    public enum IsoCatMode {
        /// <summary> Normal isometric view. </summary>
        Normal,

        /// <summary> Isometric view with the outermost layer of blocks stripped (useful for boxed maps). </summary>
        Peeled,

        /// <summary> Isometric view with a front-facing quarter of the map cut out (to show map cross-section). </summary>
        Cut,

        /// <summary> Only a specified chunk of the map is drawn. </summary>
        Chunk
    }


    /// <summary> Isometric map renderer, tightly integrated with BackgroundWorker.
    /// Creates a bitmap of the map. Every IsoCat instance is single-use. </summary>
    unsafe public sealed class IsoCat {
        static readonly byte[] Tiles, ShadowTiles;
        static readonly int TileX, TileY;
        static readonly int MaxTileDim, TileStride;

        static IsoCat() {
            using( Bitmap tilesBmp = Resources.Tileset ) {
                TileX = tilesBmp.Width / 50;
                TileY = tilesBmp.Height;
                TileStride = TileX * TileY * 4;
                Tiles = new byte[50 * TileStride];

                MaxTileDim = Math.Max( TileX, TileY );

                for( int i = 0; i < 50; i++ ) {
                    for( int y = 0; y < TileY; y++ ) {
                        for( int x = 0; x < TileX; x++ ) {
                            int p = i * TileStride + (y * TileX + x) * 4;
                            System.Drawing.Color c = tilesBmp.GetPixel( x + i * TileX, y );
                            Tiles[p] = c.B;
                            Tiles[p + 1] = c.G;
                            Tiles[p + 2] = c.R;
                            Tiles[p + 3] = c.A;
                        }
                    }
                }
            }

            using( Bitmap stilesBmp = Resources.TilesetShadowed ) {

                ShadowTiles = new byte[50 * TileStride];

                for( int i = 0; i < 50; i++ ) {
                    for( int y = 0; y < TileY; y++ ) {
                        for( int x = 0; x < TileX; x++ ) {
                            int p = i * TileStride + (y * TileX + x) * 4;
                            System.Drawing.Color c = stilesBmp.GetPixel( x + i * TileX, y );
                            ShadowTiles[p] = c.B;
                            ShadowTiles[p + 1] = c.G;
                            ShadowTiles[p + 2] = c.R;
                            ShadowTiles[p + 3] = c.A;
                        }
                    }
                }
            }
        }


        int x, y, z;
        byte block;
        public readonly int[] ChunkCoords = new int[6];

        readonly byte* image;
        readonly Bitmap imageBmp;
        readonly BitmapData imageData;
        readonly int imageWidth, imageHeight;

        readonly int dimX, dimY, dimX1, dimY1, dimX2, dimY2;
        readonly int offsetX, offsetY;
        readonly int isoOffset, isoX, isoY, isoH;
        readonly int imageStride;

        public readonly int Rot;
        public readonly IsoCatMode Mode;
        public readonly Map Map;


        public IsoCat( Map map, IsoCatMode mode, int rot ) {
            Rot = rot;
            Mode = mode;
            Map = map;

            dimX = Map.Width;
            dimY = Map.Length;
            offsetY = Math.Max( 0, Map.Width - Map.Length );
            offsetX = Math.Max( 0, Map.Length - Map.Width );
            dimX2 = dimX / 2 - 1;
            dimY2 = dimY / 2 - 1;
            dimX1 = dimX - 1;
            dimY1 = dimY - 1;

            blendDivisor = 255 * Map.Height;

            imageWidth = TileX * Math.Max( dimX, dimY ) + TileY / 2 * Map.Height + TileX * 2;
            imageHeight = TileY / 2 * Map.Height + MaxTileDim / 2 * Math.Max( Math.Max( dimX, dimY ), Map.Height ) + TileY * 2;

            imageBmp = new Bitmap( imageWidth, imageHeight, PixelFormat.Format32bppArgb );
            imageData = imageBmp.LockBits( new Rectangle( 0, 0, imageBmp.Width, imageBmp.Height ),
                                           ImageLockMode.ReadWrite,
                                           PixelFormat.Format32bppArgb );

            image = (byte*)imageData.Scan0;
            imageStride = imageData.Stride;

            isoOffset = (Map.Height * TileY / 2 * imageStride + imageStride / 2 + TileX * 2);
            isoX = (TileX / 4 * imageStride + TileX * 2);
            isoY = (TileY / 4 * imageStride - TileY * 2);
            isoH = (-TileY / 2 * imageStride);

            mh34 = Map.Height * 3 / 4;
        }

        byte* bp, ctp;
        [CanBeNull]
        public Bitmap Draw( out Rectangle cropRectangle, BackgroundWorker worker ) {
            cropRectangle = Rectangle.Empty;
            try {
                fixed( byte* bpx = Map.Blocks ) {
                    fixed( byte* tp = Tiles ) {
                        fixed( byte* stp = ShadowTiles ) {
                            bp = bpx;
                            while( z < Map.Height ) {
                                block = GetBlock( x, y, z );
                                if( block != 0 ) {

                                    switch( Rot ) {
                                        case 0: ctp = (z >= Map.Shadows[x, y] ? tp : stp); break;
                                        case 1: ctp = (z >= Map.Shadows[dimX1 - y, x] ? tp : stp); break;
                                        case 2: ctp = (z >= Map.Shadows[dimX1 - x, dimY1 - y] ? tp : stp); break;
                                        case 3: ctp = (z >= Map.Shadows[y, dimY1 - x] ? tp : stp); break;
                                    }

                                    int blockRight, blockLeft, blockUp;

                                    if( x != (Rot == 1 || Rot == 3 ? dimY1 : dimX1) ) blockRight = GetBlock( x + 1, y, z );
                                    else blockRight = 0;
                                    if( y != (Rot == 1 || Rot == 3 ? dimX1 : dimY1) ) blockLeft = GetBlock( x, y + 1, z );
                                    else blockLeft = 0;
                                    if( z != Map.Height - 1 ) blockUp = GetBlock( x, y, z + 1 );
                                    else blockUp = 0;

                                    if( blockUp == 0 || blockLeft == 0 || blockRight == 0 || // air
                                        blockUp == 8 || blockLeft == 8 || blockRight == 8 || // water
                                        blockUp == 9 || blockLeft == 9 || blockRight == 9 || // water
                                        (block != 20 && (blockUp == 20 || blockLeft == 20 || blockRight == 20)) || // glass
                                        blockUp == 18 || blockLeft == 18 || blockRight == 18 || // foliage
                                        blockLeft == 44 || blockRight == 44 || // step

                                        blockUp == 10 || blockLeft == 10 || blockRight == 10 || // lava
                                        blockUp == 11 || blockLeft == 11 || blockRight == 11 || // lava

                                        blockUp == 37 || blockLeft == 37 || blockRight == 37 || // flower
                                        blockUp == 38 || blockLeft == 38 || blockRight == 38 || // flower
                                        blockUp == 6 || blockLeft == 6 || blockRight == 6 || // tree
                                        blockUp == 39 || blockLeft == 39 || blockRight == 39 || // mushroom
                                        blockUp == 40 || blockLeft == 40 || blockRight == 40 ) // mushroom
                                        BlendTile();
                                }

                                x++;
                                if( x == (Rot == 1 || Rot == 3 ? dimY : dimX) ) {
                                    y++;
                                    x = 0;
                                }
                                if( y == (Rot == 1 || Rot == 3 ? dimX : dimY) ) {
                                    z++;
                                    y = 0;
                                    if( worker != null && z % 4 == 0 ) {
                                        if( worker.CancellationPending ) return null;
                                        worker.ReportProgress( (z * 100) / Map.Height );
                                    }
                                }
                            }
                        }
                    }
                }

                int xMin = 0, xMax = imageWidth - 1, yMin = 0, yMax = imageHeight - 1;
                bool cont = true;
                int offset;

                // find left bound (xMin)
                for( x = 0; cont && x < imageWidth; x++ ) {
                    offset = x * 4 + 3;
                    for( y = 0; y < imageHeight; y++ ) {
                        if( image[offset] > 0 ) {
                            xMin = x;
                            cont = false;
                            break;
                        }
                        offset += imageStride;
                    }
                }

                if( worker != null && worker.CancellationPending ) return null;

                // find top bound (yMin)
                cont = true;
                for( y = 0; cont && y < imageHeight; y++ ) {
                    offset = imageStride * y + xMin * 4 + 3;
                    for( x = xMin; x < imageWidth; x++ ) {
                        if( image[offset] > 0 ) {
                            yMin = y;
                            cont = false;
                            break;
                        }
                        offset += 4;
                    }
                }

                if( worker != null && worker.CancellationPending ) return null;

                // find right bound (xMax)
                cont = true;
                for( x = imageWidth - 1; cont && x >= xMin; x-- ) {
                    offset = x * 4 + 3 + yMin * imageStride;
                    for( y = yMin; y < imageHeight; y++ ) {
                        if( image[offset] > 0 ) {
                            xMax = x + 1;
                            cont = false;
                            break;
                        }
                        offset += imageStride;
                    }
                }

                if( worker != null && worker.CancellationPending ) return null;

                // find bottom bound (yMax)
                cont = true;
                for( y = imageHeight - 1; cont && y >= yMin; y-- ) {
                    offset = imageStride * y + 3 + xMin * 4;
                    for( x = xMin; x < xMax; x++ ) {
                        if( image[offset] > 0 ) {
                            yMax = y + 1;
                            cont = false;
                            break;
                        }
                        offset += 4;
                    }
                }

                cropRectangle = new Rectangle( Math.Max( 0, xMin - 2 ),
                                               Math.Max( 0, yMin - 2 ),
                                               Math.Min( imageBmp.Width, xMax - xMin + 4 ),
                                               Math.Min( imageBmp.Height, yMax - yMin + 4 ) );
                return imageBmp;
            } finally {
                imageBmp.UnlockBits( imageData );
                if( worker != null && worker.CancellationPending && imageBmp != null ) {
                    try {
                        imageBmp.Dispose();
                    } catch( ObjectDisposedException ) { }
                }
            }
        }


        void BlendTile() {
            int pos = (x + (Rot == 1 || Rot == 3 ? offsetY : offsetX)) * isoX + (y + (Rot == 1 || Rot == 3 ? offsetX : offsetY)) * isoY + z * isoH + isoOffset;
            if( block > 49 ) return;
            int tileOffset = block * TileStride;
            BlendPixel( pos, tileOffset );
            BlendPixel( pos + 4, tileOffset + 4 );
            BlendPixel( pos + 8, tileOffset + 8 );
            BlendPixel( pos + 12, tileOffset + 12 );
            pos += imageStride;
            BlendPixel( pos, tileOffset + 16 );
            BlendPixel( pos + 4, tileOffset + 20 );
            BlendPixel( pos + 8, tileOffset + 24 );
            BlendPixel( pos + 12, tileOffset + 28 );
            pos += imageStride;
            BlendPixel( pos, tileOffset + 32 );
            BlendPixel( pos + 4, tileOffset + 36 );
            BlendPixel( pos + 8, tileOffset + 40 );
            BlendPixel( pos + 12, tileOffset + 44 );
            pos += imageStride;
            //BlendPixel( pos, tileOffset + 48 ); // bottom left block, always blank in current tileset
            BlendPixel( pos + 4, tileOffset + 52 );
            BlendPixel( pos + 8, tileOffset + 56 );
            //BlendPixel( pos + 12, tileOffset + 60 ); // bottom right block, always blank in current tileset
        }


        const byte ShadingStrength = 48;
        readonly int blendDivisor, mh34;

        // inspired by http://www.devmaster.net/wiki/Alpha_blending
        void BlendPixel( int imageOffset, int tileOffset ) {
            int sourceAlpha;
            if( ctp[tileOffset + 3] == 0 ) return;

            byte tA = ctp[tileOffset + 3];

            // Get final alpha channel.
            int finalAlpha = tA + ((255 - tA) * image[imageOffset + 3]) / 255;

            // Get percentage (out of 256) of source alpha compared to final alpha
            if( finalAlpha == 0 ) {
                sourceAlpha = 0;
            } else {
                sourceAlpha = tA * 255 / finalAlpha;
            }

            // Destination percentage is just the additive inverse.
            int destAlpha = 255 - sourceAlpha;

            if( z < (Map.Height >> 1) ) {
                int shadow = (z >> 1) + mh34;
                image[imageOffset] = (byte)((ctp[tileOffset] * sourceAlpha * shadow + image[imageOffset] * destAlpha * Map.Height) / blendDivisor);
                image[imageOffset + 1] = (byte)((ctp[tileOffset + 1] * sourceAlpha * shadow + image[imageOffset + 1] * destAlpha * Map.Height) / blendDivisor);
                image[imageOffset + 2] = (byte)((ctp[tileOffset + 2] * sourceAlpha * shadow + image[imageOffset + 2] * destAlpha * Map.Height) / blendDivisor);
            } else {
                int shadow = (z - (Map.Height >> 1)) * ShadingStrength;
                image[imageOffset] = (byte)Math.Min( 255, (ctp[tileOffset] * sourceAlpha + shadow + image[imageOffset] * destAlpha) / 255 );
                image[imageOffset + 1] = (byte)Math.Min( 255, (ctp[tileOffset + 1] * sourceAlpha + shadow + image[imageOffset + 1] * destAlpha) / 255 );
                image[imageOffset + 2] = (byte)Math.Min( 255, (ctp[tileOffset + 2] * sourceAlpha + shadow + image[imageOffset + 2] * destAlpha) / 255 );
            }

            image[imageOffset + 3] = (byte)finalAlpha;
        }

        byte GetBlock( int xx, int yy, int zz ) {
            int realx;
            int realy;
            switch( Rot ) {
                case 0:
                    realx = xx;
                    realy = yy;
                    break;
                case 1:
                    realx = dimX1 - yy;
                    realy = xx;
                    break;
                case 2:
                    realx = dimX1 - xx;
                    realy = dimY1 - yy;
                    break;
                default:
                    realx = yy;
                    realy = dimY1 - xx;
                    break;
            }
            int pos = (zz * dimY + realy) * dimX + realx;

            if( Mode == IsoCatMode.Normal ) {
                return bp[pos];
            } else if( Mode == IsoCatMode.Peeled && (xx == (Rot == 1 || Rot == 3 ? dimY1 : dimX1) || yy == (Rot == 1 || Rot == 3 ? dimX1 : dimY1) || zz == Map.Height - 1) ) {
                return 0;
            } else if( Mode == IsoCatMode.Cut && xx > (Rot == 1 || Rot == 3 ? dimY2 : dimX2) && yy > (Rot == 1 || Rot == 3 ? dimX2 : dimY2) ) {
                return 0;
            } else if( Mode == IsoCatMode.Chunk && (realx < ChunkCoords[0] || realy < ChunkCoords[1] || zz < ChunkCoords[2] || realx > ChunkCoords[3] || realy > ChunkCoords[4] || zz > ChunkCoords[5]) ) {
                return 0;
            }

            return bp[pos];
        }
    }
}