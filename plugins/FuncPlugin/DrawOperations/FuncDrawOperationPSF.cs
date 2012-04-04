//Copyright (C) 2012 Lao Tszy (lao_tszy@yahoo.co.uk)

//fCraft Copyright (C) 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>

//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), 
//to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
//IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft;

namespace FuncPlugin
{
	public class FuncDrawOperationPoints : FuncDrawOperation
	{
		public FuncDrawOperationPoints(Player player, Command cmd)
			: base(player, cmd)
		{

		}

		protected override void DrawFasePrepare(int min1, int max1, int min2, int max2)
		{

		}
		protected override void DrawFase1(int fval, ref int arg1, ref int arg2, ref int val, int min1, int max1, int min2, int max2,
										  int minV, int maxV, int maxBlocksToDraw)
		{
			if (fval <= maxV && fval >= minV)
			{
				val = fval;
				if (DrawOneBlock())
					++_count;
			}
		}
		protected override void DrawFase2(ref int arg1, ref int arg2, ref int val, int min1, int max1, int min2, int max2,
										  int minV, int maxV, int maxBlocksToDraw)
		{

		}
		public override string Name
		{
			get
			{
				return base.Name + "Points";
			}
		}
	}



	public class FuncDrawOperationFill : FuncDrawOperation
	{
		public FuncDrawOperationFill(Player player, Command cmd)
			: base(player, cmd)
		{

		}

		protected override void DrawFasePrepare(int min1, int max1, int min2, int max2)
		{

		}
		protected override void DrawFase1(int fval, ref int arg1, ref int arg2, ref int val, int min1, int max1, int min2, int max2,
										  int minV, int maxV, int maxBlocksToDraw)
		{
			for (val = minV; val <= fval && val <= maxV; ++val)
			{
				if (DrawOneBlock())
				{
					++_count;
					if (TimeToEndBatch)
						return;
				}
			}
		}
		protected override void DrawFase2(ref int arg1, ref int arg2, ref int val, int min1, int max1, int min2, int max2,
										  int minV, int maxV, int maxBlocksToDraw)
		{

		}
		public override string Name
		{
			get
			{
				return base.Name + "Fill";
			}
		}
	}


	public class FuncDrawOperationSurface : FuncDrawOperation
	{
		private int[][] _surface;
		public FuncDrawOperationSurface(Player player, Command cmd)
			: base(player, cmd)
		{

		}

		protected override void DrawFasePrepare(int min1, int max1, int min2, int max2)
		{
			_surface = new int[max1 - min1 + 1][];
			for (int i = 0; i < _surface.Length; ++i)
			{
				_surface[i] = new int[max2 - min2 + 1];
				for (int j = 1; j < _surface[i].Length; ++j)
					_surface[i][j] = int.MaxValue;
			}
		}
		protected override void DrawFase1(int fval, ref int arg1, ref int arg2, ref int val, int min1, int max1, int min2, int max2,
										  int minV, int maxV, int maxBlocksToDraw)
		{
			_surface[arg1 - min1][arg2 - min2] = fval;
		}
		protected override void DrawFase2(ref int arg1, ref int arg2, ref int val, int min1, int max1, int min2, int max2,
										  int minV, int maxV, int maxBlocksToDraw)
		{
			int count = 0;
			for (arg1 = min1; arg1 <= max1; ++arg1)
			{
				for (arg2 = min2; arg2 <= max2; ++arg2)
				{
					int a1 = arg1 - min1, a2 = arg2 - min2;
					if (_surface[a1][a2] == int.MaxValue)
						continue;
					//find min value around
					int minVal = _surface[a1][a2];
					if (a1 - 1 >= 0)
						minVal = Math.Min(minVal, _surface[a1 - 1][a2] + 1);
					if (a1 + 1 < _surface.Length)
						minVal = Math.Min(minVal, _surface[a1 + 1][a2] + 1);
					if (a2 - 1 >= 0)
						minVal = Math.Min(minVal, _surface[a1][a2 - 1] + 1);
					if (a2 + 1 < _surface[a1].Length)
						minVal = Math.Min(minVal, _surface[a1][a2 + 1] + 1);
					minVal = Math.Max(minVal, minV);

					for (val = minVal; val <= _surface[a1][a2] && val <= maxV; ++val)
						if (DrawOneBlock())
						{
							++count;
							if (TimeToEndBatch)
								return;
						}
				}
			}
		}
		public override string Name
		{
			get
			{
				return base.Name + "Surface";
			}
		}
	}
}
