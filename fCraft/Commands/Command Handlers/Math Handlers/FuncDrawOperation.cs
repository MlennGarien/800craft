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
using fCraft.Drawing;

namespace fCraft
{
	//draws all func variants: any axis as value axis, points, surface, filled
	public abstract class FuncDrawOperation : DrawOperation
    {
		public enum ValueAxis
		{
			Z,
			Y,
			X,
		}
		private Expression _expression;
		private Scaler _scaler;
		private ValueAxis _vaxis;
		protected int _count;
		
		protected FuncDrawOperation(Player player, Command cmd)
			: base(player)
        {
			string strFunc = cmd.Next();
            if (string.IsNullOrWhiteSpace(strFunc))
            {
                player.Message("&WEmpty function expression");
                return;
            }
            if (strFunc.Length < 3)
            {
                player.Message("&WExpression is too short (should be like z=f(x,y))");
                return;
            }
			
			strFunc = strFunc.ToLower();

			_vaxis = GetAxis(SimpleParser.PreparseAssignment(ref strFunc));

			_expression = SimpleParser.Parse(strFunc, GetVarArray(_vaxis));
			
			Player.Message("Expression parsed as "+_expression.Print());
			string scalingStr=cmd.Next();
			_scaler = new Scaler(scalingStr);
        }

		private static string[] GetVarArray(ValueAxis axis)
		{
			switch (axis)
			{
				case ValueAxis.Z:
					return new string[] {"x", "y"};
				case ValueAxis.Y:
					return new string[] { "x", "z" };
				case ValueAxis.X:
					return new string[] { "y", "z" };
			}
			throw new ArgumentException("Unknown value axis direction "+axis+". This software is not released for use in spaces with dimension higher than three.");
		}

		private static ValueAxis GetAxis(string varName)
		{
			if (varName.Length == 1)
				switch (varName[0])
				{
					case 'x':
						return ValueAxis.X;
					case 'y':
						return ValueAxis.Y;
					case 'z':
						return ValueAxis.Z;
				}
			throw new ArgumentException("value axis " + varName + " is not valid, must be one of 'x', 'y', or 'z'");
		}

        public override int DrawBatch(int maxBlocksToDraw)
        {
			//ignoring maxBlocksToDraw
        	switch (_vaxis)
			{
				case ValueAxis.Z:
					InternalDraw(ref Coords.X, ref Coords.Y, ref Coords.Z, 
						Bounds.XMin, Bounds.XMax, Bounds.YMin, Bounds.YMax, Bounds.ZMin, Bounds.ZMax,
						maxBlocksToDraw);
					break;
				case ValueAxis.Y:
					InternalDraw(ref Coords.X, ref Coords.Z, ref Coords.Y, 
						Bounds.XMin, Bounds.XMax, Bounds.ZMin, Bounds.ZMax, Bounds.YMin, Bounds.YMax,
						maxBlocksToDraw);
					break;
				case ValueAxis.X:
					InternalDraw(ref Coords.Y, ref Coords.Z, ref Coords.X, 
						Bounds.YMin, Bounds.YMax, Bounds.ZMin, Bounds.ZMax, Bounds.XMin, Bounds.XMax,
						maxBlocksToDraw);
					break;
				default:
					throw new ArgumentException("Unknown value axis direction " + _vaxis +
					                            ". This software is not released for use in spaces with dimension higher than three.");
			}
			
            IsDone = true;
            return _count;
        }

		//this method exists to box coords nicely as ref params
		private int InternalDraw(ref int arg1, ref int arg2, ref int val, int min1, int max1, int min2, int max2, int minV, int maxV, int maxBlocksToDraw)
		{
			_count = 0;
			int exCount = 0;
			DrawFasePrepare(min1, max1, min2, max2);

			for (arg1 = min1; arg1 <= max1 && MathCommands.MaxCalculationExceptions >= exCount; ++arg1)
			{
				for (arg2 = min2; arg2 <= max2; ++arg2)
				{
					try
					{
						int fval =
							_scaler.FromFuncResult(
								_expression.Evaluate(_scaler.ToFuncParam(arg1, min1, max1),
													 _scaler.ToFuncParam(arg2, min2, max2)),
								minV, maxV);
						DrawFase1(fval, ref arg1, ref arg2, ref val, min1, max1, min2, max2, minV, maxV, maxBlocksToDraw);
						//if (TimeToEndBatch)
						//    return _count;
					}
					catch (Exception)
					{
						//the exception here is kinda of normal, for functions (especially interesting ones)
						//may have eg punctured points; we just have to keep an eye on the number, since producing 10000
						//exceptions in the multiclient application is not the best idea
                        if (++exCount > MathCommands.MaxCalculationExceptions)
						{
							Player.Message("Function drawing is interrupted: too many (>"+MathCommands.MaxCalculationExceptions+") calculation exceptions.");
							break;
						}
					}
				}
			}
			//the real drawing for the surface variant
			DrawFase2(ref arg1, ref arg2, ref val, min1, max1, min2, max2, minV, maxV, maxBlocksToDraw);
			return _count;
		}

		protected abstract void DrawFasePrepare(int min1, int max1, int min2, int max2);
		protected abstract void DrawFase1(int fval, ref int arg1, ref int arg2, ref int val, int min1, int max1, int min2, int max2,
		                                  int minV, int maxV, int maxBlocksToDraw);
		protected abstract void DrawFase2(ref int arg1, ref int arg2, ref int val, int min1, int max1, int min2, int max2,
										  int minV, int maxV, int maxBlocksToDraw);

		public override bool Prepare(Vector3I[] marks)
        {
            if (!base.Prepare(marks))
            {
                return false;
            }
            BlocksTotalEstimate = Bounds.Volume;
            return true;
        }
		public override string Name
        {
            get
            {
            	return "Func";
            }
        }
    }
}
