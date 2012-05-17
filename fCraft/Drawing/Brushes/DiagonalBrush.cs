// Copyright (C) <2012> <Jon Baker> (http://au70.net)
// Copyright 2009 - 2012 Matvei Stefarov <me@matvei.org>
using System;
using JetBrains.Annotations;
using System.Collections.Generic;
namespace fCraft.Drawing
{
    public sealed class DiagonalBrushFactory : IBrushFactory
    {
        public static readonly DiagonalBrushFactory Instance = new DiagonalBrushFactory();
        DiagonalBrushFactory()
        {
            Aliases = new[] { "zigzag" }; 
        }
        public string Name
        {
            get { return "Diagonal"; }
        }
        public string[] Aliases { get; private set; }
        const string HelpString = "Diagonal brush: Fills the area with in a diagonal pattern of 2 block types. " +
        "If only one block name is given, leaves every other block untouched.";
        public string Help
        {
            get { return HelpString; }
        }

        public IBrush MakeBrush([NotNull] Player player, Command cmd)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (cmd == null) throw new ArgumentNullException("cmd");
            if (!cmd.HasNext)
            {
                if (player.LastUsedBlockType != (Block)255)
                    return new DiagonalBrush(new[] { player.LastUsedBlockType });
                else return new DiagonalBrush(new[] { Block.Stone });
            }
            Stack<Block> temp = new Stack<Block>();
            while (cmd.HasNext)
            {
                Block block = cmd.NextBlock(player);
                if (block == Block.Undefined) return null;
                temp.Push(block);
            }
            return new DiagonalBrush(temp.ToArray());
        }
    }

    public sealed class DiagonalBrush : IBrushInstance, IBrush
    {
        public Block[] Blocks { get; private set; }

        public DiagonalBrush(Block[] blocks)
        {
            Blocks = blocks;
        }

        public DiagonalBrush([NotNull] DiagonalBrush other)
        {
            if (other == null) throw new ArgumentNullException("other");
            Blocks = other.Blocks;
        }

        #region IBrush members
        public IBrushFactory Factory
        {
            get { return DiagonalBrushFactory.Instance; }
        }

        public string Description
        {
            get
            {
                if (Blocks.Length == 0)
                {
                    return String.Format("{0}({1},{2})", Factory.Name, Blocks.JoinToString());
                }
                else
                {
                    return Factory.Name;
                }
            }
        }

        [CanBeNull]
        public IBrushInstance MakeInstance([NotNull] Player player, [NotNull] Command cmd, [NotNull] DrawOperation op)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (cmd == null) throw new ArgumentNullException("cmd");
            if (op == null) throw new ArgumentNullException("op");
            Stack<Block> temp = new Stack<Block>();
            Block[] b;
            while (cmd.HasNext)
            {
                Block block = cmd.NextBlock(player);
                if (block == Block.Undefined) return null;
                temp.Push(block);
            }
            if (temp.Count > 0)
            {
                b = temp.ToArray();
            }
            else if (player.LastUsedBlockType != Block.Undefined)
            {
                b = new[] { player.LastUsedBlockType, Block.Air };
            }
            else
            {
                b = new[] { Block.Stone, Block.Air };
            }
            return new DiagonalBrush(b);
        }
        #endregion

        #region IBrushInstance members
        public IBrush Brush
        {
            get { return this; }
        }

        public bool HasAlternateBlock
        {
            get { return false; }
        }

        public string InstanceDescription
        {
            get
            {
                return Description;
            }
        }

        public bool Begin([NotNull] Player player, [NotNull] DrawOperation state)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (state == null) throw new ArgumentNullException("state");
            if (Blocks.Length == 0) { player.Message("&WError: No block types given"); return false; }
            return true;
        }

        public Block NextBlock([NotNull] DrawOperation state)
        {
            if (state == null) throw new ArgumentNullException("state");
            return Blocks[(state.Coords.X + state.Coords.Y + state.Coords.Z) % Blocks.Length];
        }

        public void End() { }
        #endregion
    }
}