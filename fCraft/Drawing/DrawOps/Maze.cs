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
using System.Runtime.CompilerServices;
using System.Text;


namespace RandomMaze
{
    internal static class MazeUtil
    {
        static private Random _r = new Random();
        public static void RndPermutate<T>(this IList<T> a)
        {
            for (int i = a.Count - 1; i > 0; --i)
            {
                int idx = _r.Next(i + 1);
                T t = a[idx];
                a[idx] = a[i];
                a[i] = t;
            }
        }
    }
    internal struct Direction
    {
        private int _d;

        public int MoveX(int x)
        {
            return x + _dx[_d];
        }
        public int MoveY(int y)
        {
            return y + _dy[_d];
        }
        public int MoveZ(int z)
        {
            return z + _dz[_d];
        }
        public Direction Invert()
        {
            return _d < 4 ? new Direction() { _d = (_d + 2) % 4 } : new Direction() { _d = 9 - _d };
        }

        public static Direction[] GetRndPermutation()
        {
            Direction[] a = new Direction[6];
            for (int i = 0; i < a.Length; ++i)
                a[i]._d = i;
            a.RndPermutate();
            return a;
        }

        public override string ToString()
        {
            return _names[_d];
        }

        private static string[] _names = { "E", "N", "W", "S", "Down", "Up" };
        private static int[] _dx = { 1, 0, -1, 0, 0, 0 };
        private static int[] _dy = { 0, 1, 0, -1, 0, 0 };
        private static int[] _dz = { 0, 0, 0, 0, -1, 1 };

        public const int AllMask = 0x3F;
        public static readonly Direction[] All = { new Direction() { _d = 0 }, new Direction() { _d = 1 }, 
													new Direction() { _d = 2 }, new Direction() { _d = 3 }, 
													new Direction() { _d = 4 }, new Direction() { _d = 5 } };
        public int Mask()
        {
            return 0x1 << _d;
        }

        public delegate void WallCallbackDelegate(
            ref int longSide1, ref int longSide2, int long1From, int long2From);

        internal void ArrangeCoords(ref int x, ref int y, ref int z, int xFrom, int yFrom, int zFrom, int cellSize, WallCallbackDelegate fun)
        {
            switch (_d)
            {
                case 0:
                    x = xFrom + cellSize;
                    fun(ref y, ref z, yFrom, zFrom);
                    break;
                case 1:
                    y = yFrom + cellSize;
                    fun(ref x, ref z, xFrom, zFrom);
                    break;
                case 2:
                    x = xFrom - 1;
                    fun(ref y, ref z, yFrom, zFrom);
                    break;
                case 3:
                    y = yFrom - 1;
                    fun(ref x, ref z, xFrom, zFrom);
                    break;
                case 4:
                    z = zFrom - 1;
                    fun(ref x, ref y, xFrom, yFrom);
                    break;
                case 5:
                    z = zFrom + cellSize;
                    fun(ref x, ref y, xFrom, yFrom);
                    break;
            }
        }
        public delegate void StickCallbackDelegate(
            ref int coord, int coordFrom);
        internal void ArrangeCoords(ref int x, ref int y, ref int z, int xFrom, int yFrom, int zFrom, int cellSize, StickCallbackDelegate fun)
        {
            switch (_d)
            {
                case 0:
                case 2:
                    y = yFrom - 1;
                    z = zFrom - 1;
                    fun(ref x, xFrom);
                    break;
                case 1:
                case 3:
                    x = xFrom - 1;
                    z = zFrom - 1;
                    fun(ref y, yFrom);
                    break;
                case 4:
                case 5:
                    x = xFrom - 1;
                    y = yFrom - 1;
                    fun(ref z, zFrom);
                    break;
            }
        }
    }

    internal class Cell
    {
        public Path Path = null;
        public bool Used() { return null != Path; }
        public int X;
        public int Y;
        public int Z;
        public bool IsDestination = false;
        public int IndexInPath = -1;

        private int _walls = Direction.AllMask;

        public void RemoveWall(Direction d)
        {
            _walls &= ~d.Mask();
        }

        public bool Wall(Direction d)
        {
            return (_walls & d.Mask()) != 0;
        }

		internal bool IsOnSolutionPath()
		{
			return (Path.ReachedDestination && IndexInPath<=Path.GoesToDestinationUpTo);
		}
	}

    internal class Path
    {
        private static int _counter = 0;

        private Maze _maze;
        private List<Cell> _cells = new List<Cell>();

        public bool Extendable = true;
        public bool ReachedDestination = false;
        public int GoesToDestinationUpTo = -1;
        public int Index;

        private Path _parent = null;
        private int _forkedAtParents = -1;

        public Path(Maze maze, Cell start)
        {
            _maze = maze;
            Add(new Direction(), start); //direction doesnt matter if _cells is empty
            Index = _counter++;
        }
        public bool TryExtend() //returns true if a cell was added to the path
        {
            Cell last = _cells.Last();
            Direction[] dir = Direction.GetRndPermutation();
            for (int i = 0; i < dir.Length; ++i)
            {
                Cell next = _maze.GetCell(dir[i].MoveX(last.X), dir[i].MoveY(last.Y), dir[i].MoveZ(last.Z));
                if (null == next) //outside
                    continue;
                if (next.Used()) //taken already
                    continue;
                Add(dir[i], next);
                return true;
            }
            //can not be extended
            Extendable = false;
            return false;
        }

        public Path TryFork()
        {
            for (int j = _cells.Count - 2; j >= 0; --j) //try fork from the before the last to the very beginning
            {
                Cell cell = _cells[j];
                Direction[] dir = Direction.GetRndPermutation();
                for (int i = 0; i < dir.Length; ++i)
                {
                    Cell next = _maze.GetCell(dir[i].MoveX(cell.X), dir[i].MoveY(cell.Y), dir[i].MoveZ(cell.Z));
                    if (null == next) //outside
                        continue;
                    if (next.Used()) //taken already
                        continue;
                    ConnectCells(cell, next, dir[i]);
                    return new Path(_maze, next) { _parent = this, _forkedAtParents = j };
                }
            }
            return null;
        }

        private void Add(Direction d, Cell next)
        {
            if (_cells.Count > 0)
                ConnectCells(_cells.Last(), next, d);
            next.Path = this;
            next.IndexInPath = _cells.Count;
            _cells.Add(next);
            Extendable ^= next.IsDestination; //stop extending on reaching the destination
            ReachedDestination |= next.IsDestination;
            if (ReachedDestination)
            {
                GoesToDestinationUpTo = _cells.Count - 1;
                PropagateReachedDestination();
            }
        }

        private void PropagateReachedDestination()
        {
            Path me = this;
            Path parent = _parent;
            while (null != parent)
            {
                parent.ReachedDestination = true;
                parent.GoesToDestinationUpTo = me._forkedAtParents;
                me = parent;
                parent = parent._parent;
            }
        }

        private static void ConnectCells(Cell cell1, Cell cell2, Direction d)
        {
            cell1.RemoveWall(d);
            cell2.RemoveWall(d.Invert());
        }

        private bool _wallAtEndRemoved = false;
        public bool TryRemoveWallAtEnd()
        {
            if (_wallAtEndRemoved || _cells.Count < 5) //did already or too short
                return false;

            Cell last = _cells.Last();
            Direction[] dir = Direction.GetRndPermutation();
            for (int i = 0; i < dir.Length; ++i)
            {
                if (!last.Wall(dir[i]))
                    continue;
                Cell next = _maze.GetCell(dir[i].MoveX(last.X), dir[i].MoveY(last.Y), dir[i].MoveY(last.Z));
                if (null == next) //outside
                    continue;
                if (ReferenceEquals(next.Path, this) && Math.Abs(last.IndexInPath - next.IndexInPath) < 10) //same path only if distance >=10
                    continue;
                if (next.IndexInPath + (null == next.Path._parent ? 0 : next.Path._forkedAtParents) < 5) //only take long paths, consider the immediate parent too
                    continue;
                ConnectCells(last, next, dir[i]);
                _wallAtEndRemoved = true;
                return true;
            }

            return false;
        }
    }

    internal class Maze
    {
        private Cell[][][] _cells;
        private const double ForkProbability = 0.38;

        public int XSize;
        public int YSize;
        public int ZSize;

        public List<Path> _allPaths = new List<Path>();

        public Maze(int xSize, int ySize, int zSize) //2d at the moment
        {
			if (xSize < 1 || ySize < 1 || zSize<1)
				throw new ArgumentException("maze size must be at least 1x1x1");

            XSize = xSize;
            YSize = ySize;
            ZSize = zSize;

            _cells = new Cell[xSize][][];
            for (int i = 0; i < _cells.Length; ++i)
            {
                _cells[i] = new Cell[ySize][];
                for (int j = 0; j < _cells[i].Length; ++j)
                {
                    _cells[i][j] = new Cell[zSize];
                    for (int k = 0; k < _cells[i][j].Length; ++k)
                        _cells[i][j][k] = new Cell() { X = i, Y = j, Z = k };
                }
            }
            _cells[xSize - 1][ySize - 1][zSize - 1].IsDestination = true;

            _cells[0][0][0].RemoveWall(Direction.All[3]);
            _cells[xSize - 1][ySize - 1][zSize - 1].RemoveWall(Direction.All[1]);

            Mazefy();
        }

        private void Mazefy()
        {
            int count = XSize * YSize * ZSize - 1; //-1 accounts on the first path

            List<Path> extendible = new List<Path>();
            List<Path> forkable = new List<Path>();
            Path initial = new Path(this, _cells[0][0][0]);
            _allPaths.Add(initial);
            extendible.Add(initial);
            forkable.Add(initial);

            Random r = new Random();

            while (count > 0)
            {
                //get a random extendible path and extend it
                if (extendible.Count > 0)
                {
                    int idx = r.Next(extendible.Count);
                    Path p = extendible[idx];
                    if (p.TryExtend())
                        --count;
                    if (!p.Extendable)
                        extendible.RemoveAt(idx);
                }
                if (count <= 0)
                    break;
                if (extendible.Count == 0 || r.NextDouble() < ForkProbability / (forkable.Count + 1) && forkable.Count > 0)//fork when either we are totally entangled or just randomly
                {
                    int idx = r.Next(forkable.Count);
                    Path p = forkable[idx];
                    Path fork = p.TryFork();
                    if (null == fork)
                    {
                        if (!p.Extendable)
                            forkable.RemoveAt(idx);
                    }
                    else
                    {
                        _allPaths.Add(fork);
                        forkable.Add(fork);
                        if (fork.Extendable)
                            extendible.Add(fork);
                        --count;
                    }
                }
                if (count > 0 && extendible.Count == 0 && forkable.Count == 0)//algorithm failed :^( - should never ever happen
                    throw new Exception("failed to build the maze for reason unknown");
            }
            //remove some walls to add cycles and alternative paths
            if (XSize * YSize * ZSize > 90)
                AddCycles();
        }

        private void AddCycles()
        {
            Random r = new Random();
            for (int i = 0; i < Math.Sqrt(XSize * YSize * ZSize) / 10.0 + 1; ++i)
            {
                for (int tries = 0; tries < 5; ++tries) //try some times to remove a wall somewhere, it may fail though
                {
                    Path p = _allPaths[r.Next(_allPaths.Count)];
                    if (p.TryRemoveWallAtEnd())
                        break;
                }
            }
        }

        public Cell GetCell(int x, int y, int z)
        {
            if (x < 0 || x >= XSize || y < 0 || y >= YSize || z < 0 || z >= ZSize)
                return null;
            return _cells[x][y][z];
        }
    }
}
