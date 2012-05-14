//Copyright (C) <2012>  <Jon Baker, Glenn Mariën and Lao Tszy>

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.

//Copyright (C) 2012 Lao Tszy (lao_tszy@yahoo.co.uk)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft;
using fCraft.Drawing;

namespace RandomMaze
{
    internal class RandomMazeDrawOperation : DrawOpWithBrush
    {
        public const int DefaultXSize = 5;
        public const int DefaultYSize = 5;
        public const int DefaultZSize = 3;
        public const int DefaultCellSize = 3;
		public const double HintProbability = 0.3;

        public override string Name
        {
            get { return "RandomMaze"; }
        }

        public override string Description
        {
            get { return Name; }
        }

        public override int ExpectedMarks { get { return 1; } }

        private Maze _maze;
        private int _cellSize;
        private int _count = 0;
        private bool _drawingWall = false;
        private bool _drawingElevator = false;
        private int _wallPatternCoordX;
        private int _wallPatternCoordY;
        private int _patternIdx;
        private Random _r = new Random();
        private IBrushInstance _playersBrush;
		private bool _drawElevators = true;
		private bool _drawHints = false;
		private bool _needHint = false;

        public RandomMazeDrawOperation(Player player, Command cmd)
            : base(player)
        {
            int xSize = CommandOrDefault(cmd, DefaultXSize);
            int ySize = CommandOrDefault(cmd, DefaultYSize);
            int zSize = CommandOrDefault(cmd, DefaultZSize);
			ReadFlags(cmd);
            _cellSize = DefaultCellSize; 
            _playersBrush = player.Brush.MakeInstance(player, cmd, this);

            _maze = new Maze(xSize, ySize, zSize);
        }

        private static int CommandOrDefault(Command cmd, int defVal)
        {
            string s = cmd.Next();
            if (!string.IsNullOrWhiteSpace(s))
            {
                int n;
                if (int.TryParse(s, out n))
                    return n;
            }
            return defVal;
        }

		private void ReadFlags(Command cmd)
		{
			for (;;)
			{
				string s = cmd.Next();
				if (null==s)
					break;
				s.ToLower();
				if (s=="noelevators" || s=="nolifts")
					_drawElevators = false;
				else if (s=="hint" || s=="hints")
					_drawHints = true;
				else
					Player.Message("Unknown option: "+s+", ignored");
			}
		}

        public override bool Prepare(Vector3I[] marks)
        {
            if (marks == null)
                throw new ArgumentNullException("marks");
            if (marks.Length < 1)
                throw new ArgumentException("At least one mark needed.", "marks");

            Vector3I mark2 = new Vector3I((_cellSize + 1) * _maze.XSize + 1 + marks[0].X,
                (_cellSize + 1) * _maze.YSize + 1 + marks[0].Y,
                (_cellSize + 1) * _maze.ZSize + 1 + marks[0].Z);

            Marks = marks;

            // Warn if paste will be cut off
            if (mark2.X >= Map.Width)
            {
                Player.Message("Error: Not enough room horizontally (X)");
                return false;
            }
            if (mark2.Y >= Map.Length)
            {
                Player.Message("Error: Not enough room horizontally (Y)");
                return false;
            }
            if (mark2.Z >= Map.Height)
            {
                Player.Message("Error: Not enough room vertically (Z)");
                return false;
            }

            Bounds = new BoundingBox(marks[0], mark2);

            Brush = this;
            Coords = Bounds.MinVertex;

            StartTime = DateTime.UtcNow;
            Context = BlockChangeContext.Drawn;
            BlocksTotalEstimate = Bounds.Volume;
            return true;
        }


        public override int DrawBatch(int maxBlocksToDraw)
        {
            //general drawing
            for (int zCell = 0; zCell < _maze.ZSize; ++zCell)
            {
                for (int yCell = 0; yCell < _maze.YSize; ++yCell)
                {
                    for (int xCell = 0; xCell < _maze.XSize; ++xCell)
                    {
                        DrawCell(xCell, yCell, zCell);
                    }
                    //last cell in raw
                    DrawWall(_maze.XSize - 1, yCell, zCell, Direction.All[0]);
                    DrawColumn(_maze.XSize, yCell, zCell, Direction.All[4]);
                    DrawColumn(_maze.XSize, yCell, zCell, Direction.All[1]);
                }
                //side walls for the last raw
                for (int xCell = 0; xCell < _maze.XSize; ++xCell)
                {
                    DrawWall(xCell, _maze.YSize - 1, zCell, Direction.All[1]);
                    DrawColumn(xCell, _maze.YSize, zCell, Direction.All[4]);
                    DrawColumn(xCell, _maze.YSize, zCell, Direction.All[0]);
                }
                DrawColumn(_maze.XSize, _maze.YSize, zCell, Direction.All[4]);
            }
            //roof
            //normal
            for (int yCell = 0; yCell < _maze.YSize; ++yCell)
            {
                for (int xCell = 0; xCell < _maze.XSize; ++xCell)
                {
                    DrawWall(xCell, yCell, _maze.ZSize - 1, Direction.All[5]);
                    DrawColumn(xCell, yCell, _maze.ZSize, Direction.All[0]);
                    DrawColumn(xCell, yCell, _maze.ZSize, Direction.All[1]);
                }
                //last cell in raw
                DrawColumn(_maze.XSize, yCell, _maze.ZSize, Direction.All[1]);
            }
            //last raw
            for (int xCell = 0; xCell < _maze.XSize; ++xCell)
            {
                DrawColumn(xCell, _maze.YSize, _maze.ZSize, Direction.All[0]);
            }

            IsDone = true;
            return _count;
        }

        private void DrawCell(int xCell, int yCell, int zCell)
        {
            DrawWall(xCell, yCell, zCell, Direction.All[2]);
            DrawWall(xCell, yCell, zCell, Direction.All[3]);
			//here we always request a hint, and in DrawWall we will correct it to the necessary probability
			//only if we rally draw a wall there
			if (_drawHints && _maze.GetCell(xCell, yCell, zCell).IsOnSolutionPath())
				_needHint = true;
			DrawWall(xCell, yCell, zCell, Direction.All[4]);
			_needHint = false;
			DrawColumn(xCell, yCell, zCell, Direction.All[0]);
			DrawColumn(xCell, yCell, zCell, Direction.All[1]);
			DrawColumn(xCell, yCell, zCell, Direction.All[4]);
			//special case: elevator
			if (_drawElevators && !_maze.GetCell(xCell, yCell, zCell).Wall(Direction.All[5]))
				DrawElevator(xCell, yCell, zCell);
        }

        private void DrawWall(int xCell, int yCell, int zCell, Direction d)
        {
            if (_maze.GetCell(xCell, yCell, zCell).Wall(d))
            {
				//reduce the hint probability from 1.0 to required
				if (_needHint && _r.NextDouble() >= HintProbability)
					_needHint = false;

                d.ArrangeCoords(ref Coords.X, ref Coords.Y, ref Coords.Z,
                    xCell * (_cellSize + 1) + 1 + Marks[0].X,
                    yCell * (_cellSize + 1) + 1 + Marks[0].Y,
                    zCell * (_cellSize + 1) + 1 + Marks[0].Z,
                    _cellSize, WallCallback);
            }
        }

        private void DrawColumn(int xCell, int yCell, int zCell, Direction d)
        {
            d.ArrangeCoords(ref Coords.X, ref Coords.Y, ref Coords.Z,
                    xCell * (_cellSize + 1) + 1 + Marks[0].X,
                    yCell * (_cellSize + 1) + 1 + Marks[0].Y,
                    zCell * (_cellSize + 1) + 1 + Marks[0].Z,
                    _cellSize, StickCallback);
        }

        private void StickCallback(ref int coord, int coordFrom)
        {
            for (coord = coordFrom; coord < coordFrom + _cellSize; ++coord)
            {
                if (DrawOneBlock())
                    ++_count;
            }
        }

        private void WallCallback(ref int coord1, ref int coord2, int coord1From, int coord2From)
        {
            _drawingWall = true;
            ChoosePattern();
            for (coord1 = coord1From; coord1 < coord1From + _cellSize; ++coord1)
                for (coord2 = coord2From; coord2 < coord2From + _cellSize; ++coord2)
                {
                    _wallPatternCoordX = coord1 - coord1From;
                    _wallPatternCoordY = coord2 - coord2From;
                    if (DrawOneBlock())
                        ++_count;
                }
            _drawingWall = false;
        }

        private void DrawElevator(int xCell, int yCell, int zCell)
        {
            Coords.X = xCell * (_cellSize + 1) + 1 + _cellSize / 2 + Marks[0].X;
            Coords.Y = yCell * (_cellSize + 1) + 1 + _cellSize / 2 + Marks[0].Y;
            int zFrom = zCell * (_cellSize + 1) + 1 + Marks[0].Z;
            //water column
            _drawingElevator = true;
            for (Coords.Z = zFrom; Coords.Z < zFrom + _cellSize + 1; ++Coords.Z)
            {
                if (DrawOneBlock())
                    ++_count;
            }
            _drawingElevator = false;

            //partial floor above
            _drawingWall = true;
            ChoosePattern();
            Coords.Z = (zCell + 1) * (_cellSize + 1) + Marks[0].Z;

            Coords.Y = yCell * (_cellSize + 1) + 1 + _cellSize / 2 + Marks[0].Y;
            _wallPatternCoordY = _cellSize / 2;
            DrawPartOfPartialWall(ref Coords.X, xCell * (_cellSize + 1) + 1 + Marks[0].X, ref _wallPatternCoordX);

            Coords.X = xCell * (_cellSize + 1) + 1 + _cellSize / 2 + Marks[0].X;
            _wallPatternCoordX = _cellSize / 2;
            DrawPartOfPartialWall(ref Coords.Y, yCell * (_cellSize + 1) + 1 + Marks[0].Y, ref _wallPatternCoordY);

            _drawingWall = false;
        }

        private void DrawPartOfPartialWall(ref int coord, int coordFrom, ref int patternCoord)
        {
            for (coord = coordFrom; coord < coordFrom + _cellSize; ++coord)
            {
                patternCoord = coord - coordFrom;
                if (patternCoord == _cellSize / 2)
                    continue;
                if (DrawOneBlock())
                    ++_count;
            }
        }

        private void ChoosePattern()
        {
            _patternIdx = _r.NextDouble() < 0.24 ? _r.Next(_patterns.Length) : -1;
        }

        public override bool ReadParams(Command cmd)
        {
            //Brush = this;
            return true;
        }


        private Block[][][] _patterns = { new Block[][]
										{ new Block[]{ Block.Glass, Block.Glass, Block.Glass }, 
										  new Block[]{ Block.Glass, Block.Glass, Block.Glass }, 
										  new Block[]{ Block.Glass, Block.Glass, Block.Glass } },
										  new Block[][]
										{ new Block[]{ Block.Undefined, Block.Glass, Block.Undefined }, 
										  new Block[]{ Block.Glass, Block.Glass, Block.Glass }, 
										  new Block[]{ Block.Undefined, Block.Glass, Block.Undefined } },
										  new Block[][]
										{ new Block[]{ Block.Undefined, Block.Glass, Block.Undefined }, 
										  new Block[]{ Block.Glass, Block.Undefined, Block.Glass }, 
										  new Block[]{ Block.Undefined, Block.Glass, Block.Undefined } },
										  new Block[][]
										{ new Block[]{ Block.Glass, Block.Undefined, Block.Glass }, 
										  new Block[]{ Block.Undefined, Block.Glass, Block.Undefined }, 
										  new Block[]{ Block.Glass, Block.Undefined, Block.Glass } },
										  new Block[][]
										{ new Block[]{ Block.Leaves, Block.Leaves, Block.Leaves }, 
										  new Block[]{ Block.Leaves, Block.Glass, Block.Leaves }, 
										  new Block[]{ Block.Leaves, Block.Leaves, Block.Leaves } },
										  new Block[][]
										{ new Block[]{ Block.Glass, Block.Leaves, Block.Glass }, 
										  new Block[]{ Block.Leaves, Block.Glass, Block.Leaves }, 
										  new Block[]{ Block.Glass, Block.Leaves, Block.Glass } },
										};
        private Block[] _randomBlocks = {Block.White, Block.Blue, Block.Gold, Block.Cyan, Block.Green, 
											Block.Indigo, Block.Magenta, Block.Obsidian, Block.Orange, 
											Block.Pink,Block.Red, Block.Sponge, Block.Violet, Block.Yellow};

		private const Block HintBlock = Block.Log;

        protected override Block NextBlock()
        {
            if (_drawingElevator)
                return Block.Water;
            if (!_drawingWall)
                return Block.Wood;//_playersBrush.NextBlock(this);

            if (_patternIdx < 0)
			{
				if (_needHint)
				{
					_needHint = false;
					return HintBlock;
				}
				return _r.NextDouble() < 0.2
				       	? Block.Wood /*_playersBrush.NextBlock(this)*/
				       	: _randomBlocks[_r.Next(_randomBlocks.Length)];
			}

            Block b = _patterns[_patternIdx][_wallPatternCoordX][_wallPatternCoordY];
            if (b == Block.Undefined)
                b = Block.Wood; //_playersBrush.NextBlock(this);
			
            return b;
        }
    }
}
