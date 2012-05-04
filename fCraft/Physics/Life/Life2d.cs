using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
	public class Array2d<T> where T: IEquatable<T>
	{
		private int _xSize;
		private int _ySize;
		private T[] _a;

		public Array2d(int xSize, int ySize)
		{
			_a=new T[xSize*ySize];
			_xSize = xSize;
			_ySize = ySize;
		}

		public T Get(int x, int y)
		{
			return _a[Idx(x, y)];
		}

		public void Set(int x, int y, T t)
		{
			_a[Idx(x, y)]=t;
		}

		public int XSize { get { return _xSize; } }
		public int YSize { get { return _ySize; } }

		private int Idx(int x, int y)
		{
			return x*_ySize + y;
		}

		public bool Replace(T from, T to)
		{
			bool changed = false;
			for (int i = 0; i < _a.Length; ++i)
				if (_a[i].Equals(from))
				{
					_a[i] = to;
					changed = true;
				}
			return changed;
		}
		public void Clear()
		{
			Array.Clear(_a, 0, _a.Length);
		}
	}


	public class Life2d
	{
		public const byte Newborn = 2;
		public const byte Normal = 1;
		public const byte Dead = 0xff;
		public const byte Nothing = 0;

		private Array2d<byte> _a;

		public Life2d(int xSize, int ySize)
		{
			_a=new Array2d<byte>(xSize, ySize);
		}
		public void Clear()
		{
			_a.Clear();
		}
		public void Set(int x, int y)
		{
			_a.Set(x, y, Normal);
		}
		public byte Get(int x, int y)
		{
			return _a.Get(x, y);
		}
		public void HalfStep()
		{
			for (int i=0; i<_a.XSize; ++i)
				for (int j=0; j<_a.YSize; ++j)
				{
					if (Empty(i, j) && Neighbors(i, j)==3)
						_a.Set(i, j, Newborn);
				}

			for (int i = 0; i < _a.XSize; ++i)
				for (int j = 0; j < _a.YSize; ++j)
				{
					if (Alive(i, j))
					{
						int n = Neighbors(i, j);

						if (n > 3 || n < 2)
							_a.Set(i, j, Dead);
					}
				}
		}

		private bool Empty(int x, int y)
		{
			return _a.Get(x, y) == Nothing;
		}
		private bool Alive(int x, int y)
		{
			return _a.Get(x, y) == Normal;
		}

		private int Neighbors(int x, int y)
		{
			int s = 0;
			for (int i=Math.Max(0, x-1); i<=Math.Min(x+1, _a.XSize-1); ++i)
				for (int j=Math.Max(0, y-1); j<=Math.Min(y+1, _a.YSize-1); ++j)
					if (i!=x || j!=y)
					{
						byte b = _a.Get(i, j);
						if (b == Normal || b == Dead)
							++s;
					}
			return s;
		}

		public bool FinalizeStep()
		{
			bool changed = _a.Replace(Dead, Nothing);
			changed |= _a.Replace(Newborn, Normal);
			return changed;
		}
	}
}
