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
				throw new ArgumentException("empty function expression");
			if (strFunc.Length<3)
				throw new ArgumentException("expression is too short (should be like z=f(x,y))");
			
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
						if (TimeToEndBatch)
							return _count;
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
