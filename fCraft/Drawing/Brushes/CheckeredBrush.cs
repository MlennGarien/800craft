// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    public sealed class CheckeredBrushFactory : IBrushFactory {
        public static readonly CheckeredBrushFactory Instance = new CheckeredBrushFactory();

        CheckeredBrushFactory() {
            Aliases = new[] { "ch" };
        }

        public string Name {
            get { return "Checkered"; }
        }

        public string[] Aliases { get; private set; }

        const string HelpString = "Checkered brush: Fills the area with alternating checkered pattern. " +
                                  "If only one block name is given, leaves every other block untouched.";
        public string Help {
            get { return HelpString; }
        }


        public IBrush MakeBrush( [NotNull] Player player, [NotNull] Command cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            Block block = cmd.NextBlock( player );
            Block altBlock = cmd.NextBlock( player );
            return new CheckeredBrush( block, altBlock );
        }
    }


    public sealed class CheckeredBrush : IBrushInstance, IBrush {
        public Block Block1 { get; private set; }
        public Block Block2 { get; private set; }


        public CheckeredBrush( Block block1, Block block2 ) {
            Block1 = block1;
            Block2 = block2;
        }


        public CheckeredBrush( [NotNull] CheckeredBrush other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            Block1 = other.Block1;
            Block2 = other.Block2;
        }


        #region IBrush members

        public IBrushFactory Factory {
            get { return CheckeredBrushFactory.Instance; }
        }


        public string Description {
            get {
                if( Block2 != Block.Undefined ) {
                    return String.Format( "{0}({1},{2})", Factory.Name, Block1, Block2 );
                } else if( Block1 != Block.Undefined ) {
                    return String.Format( "{0}({1})", Factory.Name, Block1 );
                } else {
                    return Factory.Name;
                }
            }
        }


        [CanBeNull]
        public IBrushInstance MakeInstance( [NotNull] Player player, [NotNull] Command cmd, [NotNull] DrawOperation op ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( op == null ) throw new ArgumentNullException( "op" );

            if( cmd.HasNext ) {
                Block block = cmd.NextBlock( player );
                if( block == Block.Undefined ) return null;
                Block altBlock = cmd.NextBlock( player );
                Block1 = block;
                Block2 = altBlock;

            } else if( Block1 == Block.Undefined ) {
                player.Message( "{0}: Please specify at least one block.", Factory.Name );
                return null;
            }

            return new CheckeredBrush( this );
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
            get {
                return Description;
            }
        }


        public bool Begin( [NotNull] Player player, [NotNull] DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( state == null ) throw new ArgumentNullException( "state" );
            return true;
        }


        public Block NextBlock( [NotNull] DrawOperation state ) {
            if( state == null ) throw new ArgumentNullException( "state" );
            if( ((state.Coords.X + state.Coords.Y + state.Coords.Z) & 1) == 1 ) {
                return Block1;
            } else {
                return Block2;
            }
        }


        public void End() { }

        #endregion
    }
}