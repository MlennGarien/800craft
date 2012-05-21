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

//Copyright (C) <2012> Lao Tszy (lao_tszy@yahoo.co.uk)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft;

namespace fCraft
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
					//if (TimeToEndBatch)
					//    return;
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
							//if (TimeToEndBatch)
							//    return;
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
