// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    public sealed class ReplaceBrushFactory : IBrushFactory {
        public static readonly ReplaceBrushFactory Instance = new ReplaceBrushFactory();

        ReplaceBrushFactory() {
            Aliases = new[] { "r" };
        }

        public string Name {
            get { return "Replace"; }
        }

        public string[] Aliases { get; private set; }

        const string HelpString = "Replace brush: Replaces blocks of a given type(s) with another type. " +
                                  "Usage similar to &H/Replace&S command.";
        public string Help {
            get { return HelpString; }
        }


        [CanBeNull]
        public IBrush MakeBrush( [NotNull] Player player, [NotNull] Command cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );

            Stack<Block> blocks = new Stack<Block>();
            while( cmd.HasNext ) {
                Block block = cmd.NextBlock( player );
                if( block == Block.Undefined ) return null;
                blocks.Push( block );
            }
            if( blocks.Count == 0 ) {
                return new ReplaceBrush();
            } else if( blocks.Count == 1 ) {
                return new ReplaceBrush( blocks.ToArray(), Block.Undefined );
            } else {
                Block replacement = blocks.Pop();
                return new ReplaceBrush( blocks.ToArray(), replacement );
            }
        }
    }


    public sealed class ReplaceBrush : IBrushInstance, IBrush {
        public Block[] Blocks { get; private set; }
        public Block Replacement { get; private set; }

        public ReplaceBrush() { }

        public ReplaceBrush( Block[] blocks, Block replacement ) {
            Blocks = blocks;
            Replacement = replacement;
        }


        public ReplaceBrush( [NotNull] ReplaceBrush other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            Blocks = other.Blocks;
            Replacement = other.Replacement;
        }


        #region IBrush members

        public IBrushFactory Factory {
            get { return ReplaceBrushFactory.Instance; }
        }


        public string Description {
            get {
                if( Blocks == null ) {
                    return Factory.Name;
                } else if( Replacement == Block.Undefined ) {
                    return String.Format( "{0}({1} -> ?)",
                                          Factory.Name,
                                          Blocks.JoinToString() );
                } else {
                    return String.Format( "{0}({1} -> {2})",
                                          Factory.Name,
                                          Blocks.JoinToString(),
                                          Replacement );
                }
            }
        }

        [CanBeNull]
        public IBrushInstance MakeInstance( [NotNull] Player player, [NotNull] Command cmd, [NotNull] DrawOperation op ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( op == null ) throw new ArgumentNullException( "op" );

            Stack<Block> blocks = new Stack<Block>();
            while( cmd.HasNext ) {
                Block block = cmd.NextBlock( player );
                if( block == Block.Undefined ) return null;
                blocks.Push( block );
            }

            if( blocks.Count == 0 && Blocks == null ) {
                player.Message( "Replace brush requires at least 1 block." );
                return null;
            }

            if( blocks.Count > 0 ) {
                if( blocks.Count > 1 ) Replacement = blocks.Pop();
                Blocks = blocks.ToArray();
            }

            return new ReplaceBrush( this );
        }

        #endregion


        #region IBrushInstance members

        public IBrush Brush {
            get { return this; }
        }


        public bool HasAlternateBlock {
            get { return false; }
        }


        public string InstanceDescription {
            get { return Description; }
        }


        public bool Begin( [NotNull] Player player, [NotNull] DrawOperation op ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( op == null ) throw new ArgumentNullException( "op" );
            if( Blocks == null || Blocks.Length == 0 ) {
                throw new InvalidOperationException( "No blocks given." );
            }
            if( Replacement == Block.Undefined ) {
                if( player.LastUsedBlockType == Block.Undefined ) {
                    player.Message( "Cannot deduce desired replacement block. Click a block or type out the block name." );
                    return false;
                } else {
                    Replacement = player.GetBind( player.LastUsedBlockType );
                }
            }
            op.Context |= BlockChangeContext.Replaced;
            return true;
        }


        public Block NextBlock( [NotNull] DrawOperation op ) {
            if( op == null ) throw new ArgumentNullException( "op" );
            Block block = op.Map.GetBlock( op.Coords );
            // ReSharper disable LoopCanBeConvertedToQuery
            for( int i = 0; i < Blocks.Length; i++ ) {
                // ReSharper restore LoopCanBeConvertedToQuery
                if( block == Blocks[i] ) {
                    return Replacement;
                }
            }
            return Block.Undefined;
        }


        public void End() { }

        #endregion
    }
}