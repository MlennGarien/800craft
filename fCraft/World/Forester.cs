// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
// Based on Forester script by dudecon, ported with permission.
// Original: http://www.minecraftforum.net/viewtopic.php?f=25&t=9426
using System;
using System.Collections.Generic;
using System.Linq;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft.Events {
    public sealed class ForesterBlockPlacingEventArgs : EventArgs {
        internal ForesterBlockPlacingEventArgs( Vector3I coordinate, Block block ) {
            Coordinate = coordinate;
            Block = block;
        }
        public Vector3I Coordinate { get; private set; }
        public Block Block { get; private set; }
    }
}

namespace fCraft {
    /// <summary> Vegetation generator for MapGenerator. </summary>
    public static class Forester {
        const int MaxTries = 1000;

        public static void Generate( [NotNull] ForesterArgs args ) {
            if( args == null ) throw new ArgumentNullException( "args" );
            args.Validate();
            List<Tree> treeList = new List<Tree>();

            if( args.Operation == ForesterOperation.Conserve ) {
                FindTrees( args, treeList );
            }

            if( args.TreeCount > 0 && treeList.Count > args.TreeCount ) {
                treeList = treeList.Take( args.TreeCount ).ToList();
            }

            if( args.Operation == ForesterOperation.Replant || args.Operation == ForesterOperation.Add ) {
                switch( args.Shape ) {
                    case TreeShape.Rainforest:
                        PlantRainForestTrees( args, treeList );
                        break;
                    case TreeShape.Mangrove:
                        PlantMangroves( args, treeList );
                        break;
                    default:
                        PlantTrees( args, treeList );
                        break;
                }
            }

            if( args.Operation != ForesterOperation.ClearCut ) {
                ProcessTrees( args, treeList );
                if( args.Foliage ) {
                    foreach( Tree tree in treeList ) {
                        tree.MakeFoliage();
                    }
                }
                if( args.Wood ) {
                    foreach( Tree tree in treeList ) {
                        tree.MakeTrunk();
                    }
                }
            }
        }


        public static void Plant( [NotNull] ForesterArgs args, Vector3I treeCoordinate ) {
            List<Tree> treeList = new List<Tree> {
                new Tree {
                    Args = args,
                    Height = args.Height,
                    Pos = treeCoordinate
                }
            };
            switch( args.Shape ) {
                case TreeShape.Rainforest:
                    PlantRainForestTrees( args, treeList );
                    break;
                case TreeShape.Mangrove:
                    PlantMangroves( args, treeList );
                    break;
                default:
                    PlantTrees( args, treeList );
                    break;
            }
            ProcessTrees( args, treeList );
            if( args.Foliage ) {
                foreach( Tree tree in treeList ) {
                    tree.MakeFoliage();
                }
            }
            if( args.Wood ) {
                foreach( Tree tree in treeList ) {
                    tree.MakeTrunk();
                }
            }
        }

        public static void SexyPlant([NotNull] ForesterArgs args, Vector3I treeCoordinate)
        {
            List<Tree> treeList = new List<Tree> {
                new Tree {
                    Args = args,
                    Height = args.Height,
                    Pos = new Vector3I(treeCoordinate.X, treeCoordinate.Z, treeCoordinate.Y)
                }
            };
            switch (args.Shape)
            {
                case TreeShape.Rainforest:
                    PlantRainForestTrees(args, treeList);
                    break;
                case TreeShape.Mangrove:
                    PlantMangroves(args, treeList);
                    break;
                default:
                    PlantTrees(args, treeList);
                    break;
            }
            ProcessTrees(args, treeList);
            if (args.Foliage)
            {
                foreach (Tree tree in treeList)
                {
                    tree.MakeFoliage();
                }
            }
            if (args.Wood)
            {
                foreach (Tree tree in treeList)
                {
                    tree.MakeTrunk();
                }
            }
        }


        static void FindTrees( ForesterArgs args, ICollection<Tree> treelist ) {
            int treeheight = args.Height;

            for( int x = 0; x < args.Map.Width; x++ ) {
                for( int z = 0; z < args.Map.Length; z++ ) {
                    int y = args.Map.Height - 1;
                    while( true ) {
                        int foliagetop = args.Map.SearchColumn( x, z, args.FoliageBlock, y );
                        if( foliagetop < 0 ) break;
                        y = foliagetop;
                        Vector3I trunktop = new Vector3I( x, y - 1, z );
                        int height = DistanceToBlock( args.Map, new Vector3F( trunktop ), Vector3F.Down, args.TrunkBlock, true );
                        if( height == 0 ) {
                            y--;
                            continue;
                        }
                        y -= height;
                        if( args.Height > 0 ) {
                            height = args.Rand.Next( treeheight - args.HeightVariation,
                                                     treeheight + args.HeightVariation + 1 );
                        }
                        treelist.Add( new Tree {
                            Args = args,
                            Pos = new Vector3I( x, y, z ),
                            Height = height
                        } );
                        y--;
                    }
                }
            }
        }


        static void PlantTrees( ForesterArgs args, ICollection<Tree> treelist ) {
            int treeheight = args.Height;

            int attempts = 0;
            while( treelist.Count < args.TreeCount && attempts < MaxTries ) {
                attempts++;
                int height = args.Rand.Next( treeheight - args.HeightVariation,
                                             treeheight + args.HeightVariation + 1 );

                Vector3I treeLoc = FindRandomTreeLocation( args, height );
                if( treeLoc.Y < 0 ) continue;
                else treeLoc.Y++;
                treelist.Add( new Tree {
                    Args = args,
                    Height = height,
                    Pos = treeLoc
                } );
            }
        }


        static Vector3I FindRandomTreeLocation( ForesterArgs args, int height ) {
            int padding = (int)(height / 3f + 1);
            int mindim = Math.Min( args.Map.Width, args.Map.Length );
            if( padding > mindim / 2.2 ) {
                padding = (int)(mindim / 2.2);
            }
            int x = args.Rand.Next( padding, args.Map.Width - padding - 1 );
            int z = args.Rand.Next( padding, args.Map.Length - padding - 1 );
            int y = args.Map.SearchColumn( x, z, args.PlantOn );
            return new Vector3I( x, y, z);
        }


        static void PlantRainForestTrees( ForesterArgs args, ICollection<Tree> treelist ) {
            int treeHeight = args.Height;

            int existingTreeNum = treelist.Count;
            int remainingTrees = args.TreeCount - existingTreeNum;

            const int shortTreeFraction = 6;
            int attempts = 0;
            for( int i = 0; i < remainingTrees && attempts < MaxTries; attempts++ ) {
                float randomfac =
                    (float)( ( Math.Sqrt( args.Rand.NextDouble() ) * 1.618 - .618 ) * args.HeightVariation + .5 );

                int height;
                if( i % shortTreeFraction == 0 ) {
                    height = (int)( treeHeight + randomfac );
                } else {
                    height = (int)( treeHeight - randomfac );
                }
                Vector3I xyz = FindRandomTreeLocation( args, height );
                if( xyz.Y < 0 ) continue;

                xyz.Y++;

                bool displaced = false;
                // ReSharper disable LoopCanBeConvertedToQuery
                foreach( Tree otherTree in treelist ) {
                    Vector3I otherLoc = otherTree.Pos;
                    float otherheight = otherTree.Height;
                    int tallx = otherLoc[0];
                    int tallz = otherLoc[2];
                    float dist = (float)Math.Sqrt( Sqr( tallx - xyz.X + .5 ) + Sqr( tallz - xyz.Z + .5 ) );
                    float threshold = ( otherheight + height ) * .193f;
                    if( dist < threshold ) {
                        displaced = true;
                        break;
                    }
                }
                // ReSharper restore LoopCanBeConvertedToQuery
                if( displaced ) continue;
                treelist.Add( new RainforestTree {
                    Args = args,
                    Pos = xyz,
                    Height = height
                } );
                i++;
            }
        }


        static void PlantMangroves( ForesterArgs args, ICollection<Tree> treelist ) {
            int treeheight = args.Height;

            int attempts = 0;
            while( treelist.Count < args.TreeCount && attempts < MaxTries ) {
                attempts++;
                int height = args.Rand.Next( treeheight - args.HeightVariation,
                                             treeheight + args.HeightVariation + 1 );
                int padding = (int)(height / 3f + 1);
                int mindim = Math.Min( args.Map.Width, args.Map.Length );
                if( padding > mindim / 2.2 ) {
                    padding = (int)(mindim / 2.2);
                }
                int x = args.Rand.Next( padding, args.Map.Width - padding - 1 );
                int z = args.Rand.Next( padding, args.Map.Length - padding - 1 );
                int top = args.Map.Height - 1;

                int y = top - DistanceToBlock( args.Map, new Vector3F( x, z, top ), Vector3F.Down, Block.Air, true );
                int dist = DistanceToBlock( args.Map, new Vector3F( x, z, y ), Vector3F.Down, Block.Water, true );

                if( dist > height * .618 || dist == 0 ) {
                    continue;
                }

                y += (int)Math.Sqrt( height - dist ) + 2;
                treelist.Add( new Tree {
                    Args = args,
                    Height = height,
                    Pos = new Vector3I( x, y, z )
                } );
            }
        }


        static void ProcessTrees( ForesterArgs args, IList<Tree> treelist ) {
            TreeShape[] shapeChoices;
            switch( args.Shape ) {
                case TreeShape.Stickly:
                    shapeChoices = new[]{ TreeShape.Normal,
                                          TreeShape.Bamboo,
                                          TreeShape.Palm };
                    break;
                case TreeShape.Procedural:
                    shapeChoices = new[]{ TreeShape.Round,
                                          TreeShape.Cone };
                    break;
                default:
                    shapeChoices = new[] { args.Shape };
                    break;
            }

            for( int i = 0; i < treelist.Count; i++ ) {
                TreeShape newshape = shapeChoices[args.Rand.Next( 0, shapeChoices.Length )];
                Tree newTree;
                switch( newshape ) {
                    case TreeShape.Normal:
                        newTree = new NormalTree();
                        break;
                    case TreeShape.Bamboo:
                        newTree = new BambooTree();
                        break;
                    case TreeShape.Palm:
                        newTree = new PalmTree();
                        break;
                    case TreeShape.Round:
                        newTree = new RoundTree();
                        break;
                    case TreeShape.Cone:
                        newTree = new ConeTree();
                        break;
                    case TreeShape.Rainforest:
                        newTree = new RainforestTree();
                        break;
                    case TreeShape.Mangrove:
                        newTree = new MangroveTree();
                        break;
                    default:
                        throw new ArgumentException( "Unknown tree shape type" );
                }
                newTree.Copy( treelist[i] );

                if( args.MapHeightLimit ) {
                    int height = newTree.Height;
                    int ybase = newTree.Pos[1];
                    int mapHeight = args.Map.Height;
                    int foliageHeight;
                    if( args.Shape == TreeShape.Rainforest ) {
                        foliageHeight = 2;
                    } else {
                        foliageHeight = 4;
                    }
                    if( ybase + height + foliageHeight > mapHeight ) {
                        newTree.Height = mapHeight - ybase - foliageHeight;
                    }
                }

                if( newTree.Height < 1 ) newTree.Height = 1;
                newTree.Prepare();
                treelist[i] = newTree;
            }
        }


        #region Trees

        class Tree {
            public Vector3I Pos;
            public int Height = 1;
            public ForesterArgs Args;

            public virtual void Prepare() { }

            public virtual void MakeTrunk() { }

            public virtual void MakeFoliage() { }

            public void Copy( Tree other ) {
                Args = other.Args;
                Pos = other.Pos;
                Height = other.Height;
            }
        }


        class StickTree : Tree {
            public override void MakeTrunk() {
                for( int i = 0; i < Height; i++ ) {
                    Args.PlaceBlock( Pos.X, Pos.Z, Pos.Y + i, Args.TrunkBlock );
                }
            }
        }


        sealed class NormalTree : StickTree {
            public override void MakeFoliage() {
                int topy = Pos[1] + Height - 1;
                int start = topy - 2;
                int end = topy + 2;

                for( int y = start; y < end; y++ ) {
                    int rad;
                    if( y > start + 1 ) {
                        rad = 1;
                    } else {
                        rad = 2;
                    }
                    for( int xoff = -rad; xoff < rad + 1; xoff++ ) {
                        for( int zoff = -rad; zoff < rad + 1; zoff++ ) {
                            if( Args.Rand.NextDouble() > .618 &&
                                Math.Abs( xoff ) == Math.Abs( zoff ) &&
                                Math.Abs( xoff ) == rad ) {
                                continue;
                            }
                            Args.PlaceBlock( Pos[0] + xoff, Pos[2] + zoff, y, Args.FoliageBlock );
                        }
                    }
                }
            }
        }


        sealed class BambooTree : StickTree {
            public override void MakeFoliage() {
                int start = Pos[1];
                int end = start + Height + 1;
                for( int y = start; y < end; y++ ) {
                    for( int i = 0; i < 2; i++ ) {
                        int xoff = Args.Rand.Next( 0, 2 ) * 2 - 1;
                        int zoff = Args.Rand.Next( 0, 2 ) * 2 - 1;
                        Args.PlaceBlock( Pos[0] + xoff, Pos[2] + zoff, y, Args.FoliageBlock );
                    }
                }
            }
        }


        sealed class PalmTree : StickTree {
            public override void MakeFoliage() {
                int y = Pos[1] + Height;
                for( int xoff = -2; xoff < 3; xoff++ ) {
                    for( int zoff = -2; zoff < 3; zoff++ ) {
                        if( Math.Abs( xoff ) == Math.Abs( zoff ) ) {
                            Args.PlaceBlock( Pos[0] + xoff, Pos[2] + zoff, y, Args.FoliageBlock );
                        }
                    }
                }
            }
        }


        class ProceduralTree : Tree {

            // ReSharper disable MemberCanBePrivate.Local
            // ReSharper disable MemberCanBeProtected.Local
            public float TrunkRadius { get; set; }
            public float BranchSlope { get; set; }
            public float TrunkHeight { get; set; }
            public float BranchDensity { get; set; }
            public float[] FoliageShape { get; set; }
            public Vector3I[] FoliageCoords { get; set; }
            // ReSharper restore MemberCanBeProtected.Local
            // ReSharper restore MemberCanBePrivate.Local


            void CrossSection( Vector3I center, float radius, int diraxis, Block matidx ) {
                int rad = (int)(radius + .618);
                int secidx1 = (diraxis - 1) % 3;
                int secidx2 = (diraxis + 1) % 3;

                Vector3I coord = new Vector3I();

                for( int off1 = -rad; off1 <= rad; off1++ ) {
                    for( int off2 = -rad; off2 <= rad; off2++ ) {
                        float thisdist = (float)Math.Sqrt( Sqr( Math.Abs( off1 ) + .5 ) +
                                                           Sqr( Math.Abs( off2 ) + .5 ) );
                        if( thisdist > radius ) continue;
                        int pri = center[diraxis];
                        int sec1 = center[secidx1] + off1;
                        int sec2 = center[secidx2] + off2;
                        coord[diraxis] = pri;
                        coord[secidx1] = sec1;
                        coord[secidx2] = sec2;
                        Args.PlaceBlock( coord, matidx );
                    }
                }
            }


            protected virtual float ShapeFunc( int z ) {
                if( Args.Rand.NextDouble() < 100f / Sqr( Height ) && z < TrunkHeight ) {
                    return Height * .12f;
                } else {
                    return -1;
                }
            }

            void FoliageCluster( Vector3I center ) {
                int z = center[1];
                foreach( float i in FoliageShape ) {
                    CrossSection( new Vector3I( center[0], z, center[2] ), i, 1, Args.FoliageBlock );
                    z++;
                }
            }

            void TaperedLimb( Vector3I start, Vector3I end, float startSize, float endSize ) {
                Vector3I delta = end - start;
                int primidx = (int)delta.LongestAxis;
                int maxdist = delta[primidx];
                if( maxdist == 0 ) return;
                int primsign = (maxdist > 0 ? 1 : -1);

                int secidx1 = (primidx - 1) % 3;
                int secidx2 = (primidx + 1) % 3;

                int secdelta1 = delta[secidx1];
                float secfac1 = secdelta1 / (float)delta[primidx];
                int secdelta2 = delta[secidx2];
                float secfac2 = secdelta2 / (float)delta[primidx];

                Vector3I coord = new Vector3I();
                int endoffset = delta[primidx] + primsign;

                for( int primoffset = 0; primoffset < endoffset; primoffset += primsign ) {
                    int primloc = start[primidx] + primoffset;
                    int secloc1 = (int)(start[secidx1] + primoffset * secfac1);
                    int secloc2 = (int)(start[secidx2] + primoffset * secfac2);
                    coord[primidx] = primloc;
                    coord[secidx1] = secloc1;
                    coord[secidx2] = secloc2;
                    float primdist = Math.Abs( delta[primidx] );
                    float radius = endSize + (startSize - endSize) * Math.Abs( delta[primidx] - primoffset ) / primdist;

                    CrossSection( coord, radius, primidx, Args.TrunkBlock );
                }
                return;
            }

            public override void MakeFoliage() {
                foreach( Vector3I coord in FoliageCoords ) {
                    FoliageCluster( coord );
                }
                foreach( Vector3I coord in FoliageCoords ) {
                    Args.PlaceBlock( coord, Args.FoliageBlock );
                }
            }

            void MakeBranches() {
                int topy = Pos[1] + (int)(TrunkHeight + .5);
                float endrad = TrunkRadius * (1 - TrunkHeight / Height);
                if( endrad < 1 ) endrad = 1;

                foreach( Vector3I coord in FoliageCoords ) {
                    float dist = (float)Math.Sqrt( Sqr( coord.X - Pos.X ) + Sqr( coord.Z - Pos.Z ) );
                    float ydist = coord[1] - Pos[1];
                    float value = (BranchDensity * 220 * Height) / Cub( ydist + dist );

                    if( value < Args.Rand.NextDouble() ) continue;

                    int posy = coord[1];
                    float slope = (float)(BranchSlope + (.5 - Args.Rand.NextDouble()) * .16);

                    float branchy, basesize;
                    if( coord[1] - dist * slope > topy ) {
                        float threshold = 1 / (float)Height;
                        if( Args.Rand.NextDouble() < threshold ) continue;
                        branchy = topy;
                        basesize = endrad;
                    } else {
                        branchy = posy - dist * slope;
                        basesize = endrad + (TrunkRadius - endrad) *
                                   (topy - branchy) / TrunkHeight;
                    }

                    float startsize = (float)(basesize * (1 + Args.Rand.NextDouble()) *
                                              .618 * Math.Pow( dist / Height, .618 ));
                    float rndr = (float)(Math.Sqrt( Args.Rand.NextDouble() ) * basesize * .618);
                    float rndang = (float)(Args.Rand.NextDouble() * 2 * Math.PI);
                    int rndx = (int)(rndr * Math.Sin( rndang ) + .5);
                    int rndz = (int)(rndr * Math.Cos( rndang ) + .5);
                    Vector3I startcoord = new Vector3I {
                        X = Pos[0] + rndx,
                        Z = Pos[2] + rndz,
                        Y = (int)branchy
                    };
                    if( startsize < 1 ) startsize = 1;
                    const float endsize = 1;
                    TaperedLimb( startcoord, coord, startsize, endsize );
                }
            }

            struct RootBase {
                public int X, Z;
                public float Radius;
            }

            void MakeRoots( IList<RootBase> rootbases ) {
                if( rootbases.Count == 0 ) return;
                foreach( Vector3I coord in FoliageCoords ) {
                    float dist = (float)Math.Sqrt( Sqr( coord[0] - Pos[0] ) + Sqr( coord[2] - Pos[2] ) );
                    float ydist = coord[1] - Pos[1];
                    float value = (BranchDensity * 220 * Height) / Cub( ydist + dist );
                    if( value < Args.Rand.NextDouble() ) continue;

                    RootBase rootbase = rootbases[Args.Rand.Next( 0, rootbases.Count )];
                    int rootx = rootbase.X;
                    int rootz = rootbase.Z;
                    float rootbaseradius = rootbase.Radius;

                    float rndr = (float)(Math.Sqrt( Args.Rand.NextDouble() ) * rootbaseradius * .618);
                    float rndang = (float)(Args.Rand.NextDouble() * 2 * Math.PI);
                    int rndx = (int)(rndr * Math.Sin( rndang ) + .5);
                    int rndz = (int)(rndr * Math.Cos( rndang ) + .5);
                    int rndy = (int)(Args.Rand.NextDouble() * rootbaseradius * .5);
                    Vector3I startcoord = new Vector3I {
                        X = rootx + rndx,
                        Z = rootz + rndz,
                        Y = Pos[1] + rndy
                    };
                    Vector3F offset = new Vector3F( startcoord - coord );

                    if( Args.Shape == TreeShape.Mangrove ) {
                        // offset = [int(val * 1.618 - 1.5) for val in offset]
                        offset = offset * 1.618f - HalfBlock * 3;
                    }

                    Vector3I endcoord = startcoord + offset.RoundDown();
                    float rootstartsize = (float)(rootbaseradius * .618 * Math.Abs( offset[1] ) / (Height * .618));

                    if( rootstartsize < 1 ) rootstartsize = 1;
                    const float endsize = 1;

                    if( Args.Roots == RootMode.ToStone ||
                        Args.Roots == RootMode.Hanging ) {
                        float offlength = offset.Length;
                        if( offlength < 1 ) continue;
                        float rootmid = endsize;
                        Vector3F vec = offset / offlength;

                        Block searchIndex = Block.Air;
                        if( Args.Roots == RootMode.ToStone ) {
                            searchIndex = Block.Stone;
                        } else if( Args.Roots == RootMode.Hanging ) {
                            searchIndex = Block.Air;
                        }

                        int startdist = (int)(Args.Rand.NextDouble() * 6 * Math.Sqrt( rootstartsize ) + 2.8);
                        Vector3I searchstart = new Vector3I( startcoord + vec * startdist );

                        dist = startdist + DistanceToBlock( Args.Map, new Vector3F( searchstart ), vec, searchIndex );

                        if( dist < offlength ) {
                            rootmid += (rootstartsize - endsize) * (1 - dist / offlength);
                            endcoord = new Vector3I( startcoord + vec * dist );
                            if( Args.Roots == RootMode.Hanging ) {
                                float remainingDist = offlength - dist;
                                Vector3I bottomcord = endcoord;
                                bottomcord[1] -= (int)remainingDist;
                                TaperedLimb( endcoord, bottomcord, rootmid, endsize );
                            }
                        }
                        TaperedLimb( startcoord, endcoord, rootstartsize, rootmid );
                    } else {
                        TaperedLimb( startcoord, endcoord, rootstartsize, endsize );
                    }
                }
            }

            public override void MakeTrunk() {
                int starty = Pos[1];
                int midy = (int)(Pos[1] + TrunkHeight * .382);
                int topy = (int)(Pos[1] + TrunkHeight + .5);

                int x = Pos[0];
                int z = Pos[2];
                float midrad = TrunkRadius * .8f;
                float endrad = TrunkRadius * (1 - TrunkHeight / Height);

                if( endrad < 1 ) endrad = 1;
                if( midrad < endrad ) midrad = endrad;

                float startrad;
                List<RootBase> rootbases = new List<RootBase>();
                if( Args.RootButtresses || Args.Shape == TreeShape.Mangrove ) {
                    startrad = TrunkRadius * .8f;
                    rootbases.Add( new RootBase {
                        X = x,
                        Z = z,
                        Radius = startrad
                    } );
                    float buttressRadius = TrunkRadius * .382f;
                    float posradius = TrunkRadius;
                    if( Args.Shape == TreeShape.Mangrove ) {
                        posradius *= 2.618f;
                    }
                    int numOfButtresss = (int)(Math.Sqrt( TrunkRadius ) + 3.5);
                    for( int i = 0; i < numOfButtresss; i++ ) {
                        float rndang = (float)(Args.Rand.NextDouble() * 2 * Math.PI);
                        float thisposradius = (float)(posradius * (.9 + Args.Rand.NextDouble() * .2));
                        int thisx = x + (int)(thisposradius * Math.Sin( rndang ));
                        int thisz = z + (int)(thisposradius * Math.Cos( rndang ));

                        float thisbuttressradius = (float)(buttressRadius * (.618 + Args.Rand.NextDouble()));
                        if( thisbuttressradius < 1 ) thisbuttressradius = 1;

                        TaperedLimb( new Vector3I( thisx, starty, thisz ), new Vector3I( x, midy, z ),
                                     thisbuttressradius, thisbuttressradius );
                        rootbases.Add( new RootBase {
                            X = thisx,
                            Z = thisz,
                            Radius = thisbuttressradius
                        } );
                    }
                } else {
                    startrad = TrunkRadius;
                    rootbases.Add( new RootBase {
                        X = x,
                        Z = z,
                        Radius = startrad
                    } );
                }
                TaperedLimb( new Vector3I( x, starty, z ), new Vector3I( x, midy, z ), startrad, midrad );
                TaperedLimb( new Vector3I( x, midy, z ), new Vector3I( x, topy, z ), midrad, endrad );
                MakeBranches();
                if( Args.Roots != RootMode.None ) {
                    MakeRoots( rootbases.ToArray() );
                }
            }

            public override void Prepare() {
                base.Prepare();
                TrunkRadius = (float)Math.Sqrt( Height * Args.TrunkThickness );
                if( TrunkRadius < 1 ) TrunkRadius = 1;

                TrunkHeight = Height * .618f;
                BranchDensity = (Args.BranchDensity / Args.FoliageDensity);

                int ystart = Pos[1];
                int yend = (Pos[1] + Height);
                int numOfClustersPerY = (int)(1.5 + Sqr( Args.FoliageDensity * Height / 19f ));
                if( numOfClustersPerY < 1 ) numOfClustersPerY = 1;

                List<Vector3I> foliageCoords = new List<Vector3I>();
                for( int y = yend - 1; y >= ystart; y-- ) {
                    for( int i = 0; i < numOfClustersPerY; i++ ) {
                        float shapefac = ShapeFunc( y - ystart );
                        if( shapefac < 0 ) continue;
                        float r = (float)((Math.Sqrt( Args.Rand.NextDouble() ) + .328) * shapefac);
                        float theta = (float)(Args.Rand.NextDouble() * 2 * Math.PI);
                        int x = (int)(r * Math.Sin( theta )) + Pos[0];
                        int z = (int)(r * Math.Cos( theta )) + Pos[2];
                        foliageCoords.Add( new Vector3I( x, y, z ) );
                    }
                }
                FoliageCoords = foliageCoords.ToArray();
            }
        }


        class RoundTree : ProceduralTree {
            public override void Prepare() {
                base.Prepare();
                BranchSlope = .382f;
                FoliageShape = new[] { 2, 3, 3, 2.5f, 1.6f };
                TrunkRadius *= .8f;
                TrunkHeight = Args.TrunkHeight * Height;
            }


            protected override float ShapeFunc( int y ) {
                float twigs = base.ShapeFunc( y );
                if( twigs >= 0 ) return twigs;

                if( y < Height * (.282 + .1 * Math.Sqrt( Args.Rand.NextDouble() )) ) {
                    return -1;
                }

                float radius = Height / 2f;
                float adj = Height / 2f - y;
                float dist;
                if( adj == 0 ) {
                    dist = radius;
                } else if( Math.Abs( adj ) >= radius ) {
                    dist = 0;
                } else {
                    dist = (float)Math.Sqrt( radius * radius - adj * adj );
                }
                dist *= .618f;
                return dist;
            }
        }


        sealed class ConeTree : ProceduralTree {
            public override void Prepare() {
                base.Prepare();
                BranchSlope = .15f;
                FoliageShape = new[] { 3, 2.6f, 2, 1 };
                TrunkRadius *= .618f;
                TrunkHeight = Height;
            }


            protected override float ShapeFunc( int y ) {
                float twigs = base.ShapeFunc( y );
                if( twigs >= 0 ) return twigs;
                if( y < Height * (.25 + .05 * Math.Sqrt( Args.Rand.NextDouble() )) ) {
                    return -1;
                }
                float radius = (Height - y) * .382f;
                if( radius < 0 ) radius = 0;
                return radius;
            }
        }


        sealed class RainforestTree : ProceduralTree {
            public override void Prepare() {
                FoliageShape = new[] { 3.4f, 2.6f };
                base.Prepare();
                BranchSlope = 1;
                TrunkRadius *= .382f;
                TrunkHeight = Height * .9f;
            }


            protected override float ShapeFunc( int y ) {
                if( y < Height * .8 ) {
                    if( Args.Height < Height ) {
                        float twigs = base.ShapeFunc( y );
                        if( twigs >= 0 && Args.Rand.NextDouble() < .05 ) {
                            return twigs;
                        }
                    }
                    return -1;
                } else {
                    float width = Height * .382f;
                    float topdist = (Height - y) / (Height * .2f);
                    float dist = (float)(width * (.618 + topdist) * (.618 + Args.Rand.NextDouble()) * .382);
                    return dist;
                }
            }
        }


        sealed class MangroveTree : RoundTree {
            public override void Prepare() {
                base.Prepare();
                BranchSlope = 1;
                TrunkRadius *= .618f;
            }


            protected override float ShapeFunc( int y ) {
                float val = base.ShapeFunc( y );
                if( val < 0 ) return -1;
                val *= 1.618f;
                return val;
            }
        }

        #endregion


        #region Math Helpers

        static int DistanceToBlock( Map map, Vector3F coord, Vector3F vec, Block blockType ) {
            return DistanceToBlock( map, coord, vec, blockType, false );
        }

        static readonly Vector3F HalfBlock = new Vector3F( .5f, .5f, .5f );
        static int DistanceToBlock( Map map, Vector3F coord, Vector3F vec, Block blockType, bool invert ) {
            coord += HalfBlock;
            int iterations = 0;
            while( map.InBounds( new Vector3I( coord ) ) ) {
                Block blockAtPos = map.GetBlock( new Vector3I( coord ) );
                if( (blockAtPos == blockType && !invert) ||
                    (blockAtPos != blockType && invert) ) {
                    break;
                } else {
                    coord += vec;
                    iterations++;
                }
            }
            return iterations;
        }

        static float Sqr( float val ) {
            return val * val;
        }

        static float Cub( float val ) {
            return val * val * val;
        }

        static int Sqr( int val ) {
            return val * val;
        }

        static double Sqr( double val ) {
            return val * val;
        }

        #endregion


        #region Enumerations

        public enum ForesterOperation {
            ClearCut,
            Conserve,
            Replant,
            Add
        }

        public enum TreeShape {
            Normal,
            Bamboo,
            Palm,
            Stickly,
            Round,
            Cone,
            Procedural,
            Rainforest,
            Mangrove
        }

        public enum RootMode {
            Normal,
            ToStone,
            Hanging,
            None
        }

        #endregion
    }

    // TODO: Add a UI to ConfigGUI.AddWorldPopup to set these
    // ReSharper disable ConvertToConstant.Global
    public sealed class ForesterArgs {
        public Forester.ForesterOperation Operation = Forester.ForesterOperation.Replant;
        public int TreeCount = 15; // 0 = no limit if op=conserve/replant
        public Forester.TreeShape Shape = Forester.TreeShape.Procedural;
        public int Height = 25;
        public int HeightVariation = 15;
        public bool Wood = true;
        public float TrunkThickness = 1;
        public float TrunkHeight = .7f;
        public float BranchDensity = 1;
        public Forester.RootMode Roots = Forester.RootMode.Normal;
        public bool RootButtresses = true;
        public bool Foliage = true;
        public float FoliageDensity = 1;
        public bool MapHeightLimit = true;
        public Block PlantOn = Block.Grass;
        public Random Rand;
        public Map Map;

        public Block TrunkBlock = Block.Log;
        public Block FoliageBlock = Block.Leaves;

        public event EventHandler<ForesterBlockPlacingEventArgs> BlockPlacing;

        internal void PlaceBlock( int x, int y, int z, Block block ) {
            var h = BlockPlacing;
            if( h != null ) h( this, new ForesterBlockPlacingEventArgs( new Vector3I( x, y, z ), block ) );
        }

        internal void PlaceBlock( Vector3I coord, Block block ) {
            var h = BlockPlacing;
            if( h != null ) h( this, new ForesterBlockPlacingEventArgs( new Vector3I(coord.X,coord.Z,coord.Y), block ) ); // todo: rewrite the whole thing to use XYZ coords
        }

        internal void Validate() {
            if( TreeCount < 0 ) TreeCount = 0;
            if( Height < 1 ) Height = 1;
            if( HeightVariation > Height ) HeightVariation = Height;
            if( TrunkThickness < 0 ) TrunkThickness = 0;
            if( TrunkHeight < 0 ) TrunkHeight = 0;
            if( FoliageDensity < 0 ) FoliageDensity = 0;
            if( BranchDensity < 0 ) BranchDensity = 0;
        }
    }
}