// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    public class PasteDrawOperation : DrawOpWithBrush {
        public override string Name {
            get {
                return Not ? "PasteNotX" : "PasteX";
            }
        }

        public override string Description {
            get {
                if( Blocks == null ) {
                    return Name;
                } else {
                    return String.Format( "{0}({1})",
                                          Name,
                                          Blocks.JoinToString() );
                }
            }
        }

        // ReSharper disable MemberCanBeProtected.Global
        public bool Not { get; private set; }
        // ReSharper restore MemberCanBeProtected.Global
        public Block[] Blocks { get; private set; }
        public Vector3I Start { get; private set; }

        public CopyState CopyInfo { get; private set; }


        public PasteDrawOperation( Player player, bool not )
            : base( player ) {
            Not = not;
        }


        public override bool Prepare( Vector3I[] marks ) {
            if( marks == null ) throw new ArgumentNullException( "marks" );
            if( marks.Length < 2 ) throw new ArgumentException( "At least two marks needed.", "marks" );

            // Make sure that we have something to paste
            CopyInfo = Player.GetCopyInformation();
            if( CopyInfo == null ) {
                Player.Message( "Nothing to paste! Copy something first." );
                return false;
            }

            // Calculate the buffer orientation
            Vector3I delta = marks[1] - marks[0];
            Vector3I orientation = new Vector3I {
                X = (delta.X == 0 ? CopyInfo.Orientation.X : Math.Sign( delta.X )),
                Y = (delta.Y == 0 ? CopyInfo.Orientation.Y : Math.Sign( delta.Y )),
                Z = (delta.Z == 0 ? CopyInfo.Orientation.Z : Math.Sign( delta.Z ))
            };

            // Calculate the start/end coordinates for pasting
            marks[1] = marks[0] + new Vector3I( orientation.X * (CopyInfo.Dimensions.X - 1),
                                                orientation.Y * (CopyInfo.Dimensions.Y - 1),
                                                orientation.Z * (CopyInfo.Dimensions.Z - 1) );
            Bounds = new BoundingBox( marks[0], marks[1] );
            Marks = marks;

            // Warn if paste will be cut off
            if( Bounds.XMin < 0 || Bounds.XMax > Map.Width - 1 ) {
                Player.Message( "Warning: Not enough room horizontally (X), paste cut off." );
            }
            if( Bounds.YMin < 0 || Bounds.YMax > Map.Length - 1 ) {
                Player.Message( "Warning: Not enough room horizontally (Y), paste cut off." );
            }
            if( Bounds.ZMin < 0 || Bounds.ZMax > Map.Height - 1 ) {
                Player.Message( "Warning: Not enough room vertically, paste cut off." );
            }

            // Clip bounds to the map, to avoid unnecessary iteration beyond the map boundaries
            Start = Bounds.MinVertex;
            Bounds = Bounds.GetIntersection( Map.Bounds );

            // Set everything up for pasting
            Brush = this;
            Coords = Bounds.MinVertex;

            StartTime = DateTime.UtcNow;
            Context = BlockChangeContext.Drawn | BlockChangeContext.Pasted;
            BlocksTotalEstimate = Bounds.Volume;
            return true;
        }


        public override int DrawBatch( int maxBlocksToDraw ) {
            // basically same as CuboidDrawOp
            int blocksDone = 0;
            for( ; Coords.X <= Bounds.XMax; Coords.X++ ) {
                for( ; Coords.Y <= Bounds.YMax; Coords.Y++ ) {
                    for( ; Coords.Z <= Bounds.ZMax; Coords.Z++ ) {
                        if( !DrawOneBlock() ) continue;
                        blocksDone++;
                        if( blocksDone >= maxBlocksToDraw ) {
                            Coords.Z++;
                            return blocksDone;
                        }
                    }
                    Coords.Z = Bounds.ZMin;
                }
                Coords.Y = Bounds.YMin;
                if( TimeToEndBatch ) {
                    Coords.X++;
                    return blocksDone;
                }
            }
            IsDone = true;
            return blocksDone;
        }


        public override bool ReadParams( Command cmd ) {
            if( Player.GetCopyInformation() == null ) {
                Player.Message( "Nothing to paste! Copy something first." );
                return false;
            }
            List<Block> blocks = new List<Block>();
            while( cmd.HasNext ) {
                Block block = cmd.NextBlock( Player );
                if( block == Block.Undefined ) return false;
                blocks.Add( block );
            }
            if( blocks.Count > 0 ) {
                Blocks = blocks.ToArray();
            } else if( Not ) {
                Player.Message( "PasteNot requires at least 1 block." );
                return false;
            }
            Brush = this;
            return true;
        }


        protected override Block NextBlock() {
            // ReSharper disable LoopCanBeConvertedToQuery
            Block block = CopyInfo.Buffer[Coords.X - Start.X, Coords.Y - Start.Y, Coords.Z - Start.Z];
            if( Blocks == null ) return block;
            if( Not ) {
                for( int i = 0; i < Blocks.Length; i++ ) {
                    if( block == Blocks[i] ) return Block.Undefined;
                }
                return block;
            } else {
                for( int i = 0; i < Blocks.Length; i++ ) {
                    if( block == Blocks[i] ) return block;
                }
                return Block.Undefined;
            }
            // ReSharper restore LoopCanBeConvertedToQuery
        }
    }
}