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
using System.Tuples;

namespace fCraft
{
	//draws volume, defined by an inequality 
	public class EqualityDrawOperation : DrawOperation
	{
        public static bool IsNullOrWhiteSpace(string value)
        {
            if (value == null) return true;
            return string.IsNullOrEmpty(value.Trim());
        }
		private Expression _expression;
		private Scaler _scaler;
		private int _count;
		public EqualityDrawOperation(Player player, Command cmd)
			: base(player)
		{
			string strFunc = cmd.Next();
            if (IsNullOrWhiteSpace(strFunc))
            {
                player.Message("empty equality expression");
                return;
            }
            if (strFunc.Length < 3)
            {
                player.Message("expression is too short (should be like f(x,y,z)=g(x,y,z))");
                return;
            }

			strFunc = strFunc.ToLower();

			_expression = SimpleParser.ParseAsEquality(strFunc, new string[] { "x", "y", "z" });
			
			Player.Message("Expression parsed as " + _expression.Print());
			string scalingStr = cmd.Next();
			_scaler = new Scaler(scalingStr);
		}

		public override int DrawBatch(int maxBlocksToDraw)
		{
			//ignoring maxBlocksToDraw

			//do it 3 times, iterating axis in different order, to get to the closed surface as close as possible
			InternalDraw(ref Coords.X, ref Coords.Y, ref Coords.Z,
						Bounds.XMin, Bounds.XMax, Bounds.YMin, Bounds.YMax, Bounds.ZMin, Bounds.ZMax,
						maxBlocksToDraw);
			InternalDraw(ref Coords.X, ref Coords.Z, ref Coords.Y,
						Bounds.XMin, Bounds.XMax, Bounds.ZMin, Bounds.ZMax, Bounds.YMin, Bounds.YMax,
						maxBlocksToDraw);
			InternalDraw(ref Coords.Y, ref Coords.Z, ref Coords.X,
						Bounds.YMin, Bounds.YMax, Bounds.ZMin, Bounds.ZMax, Bounds.XMin, Bounds.XMax,
						maxBlocksToDraw);
			
			IsDone = true;
			return _count;
		}

		//this method exists to box coords nicely as ref params
		private int InternalDraw(ref int arg1, ref int arg2, ref int arg3, int min1, int max1, int min2, int max2, int min3, int max3, int maxBlocksToDraw)
		{
			_count = 0;
			int exCount = 0;

            for (arg1 = min1; arg1 <= max1 && MathCommands.MaxCalculationExceptions >= exCount; ++arg1)
			{
                for (arg2 = min2; arg2 <= max2 && MathCommands.MaxCalculationExceptions >= exCount; ++arg2)
				{
					double prevDiff = 0;
					double prevComp = 0;
					for (int arg3Iterator = min3; arg3Iterator <= max3; ++arg3Iterator)
					{
						try
						{
							Tuple<double, double> res=
								_expression.EvaluateAsEquality(_scaler.ToFuncParam(arg1, min1, max1),
									                     _scaler.ToFuncParam(arg2, min2, max2),
														 _scaler.ToFuncParam(arg3Iterator, min3, max3));
							//decision: we cant onl ytake points with 0 as comparison result as it will happen almost never.
							//We are reacting on the changes of the comparison result sign 
							arg3 = int.MaxValue;
							if (res.First == 0) //exactly equal, wow, such a surprise
								arg3 = arg3Iterator;
							else if (res.First * prevComp < 0) //i.e. different signs, but not the prev==0
								arg3 = res.Second < prevDiff ? arg3Iterator : arg3Iterator - 1; //then choose the closest to 0 difference
							
							if (DrawOneBlock())
								++_count;
							if (TimeToEndBatch)
								return _count;

							prevComp = res.First;
							prevDiff = res.Second;
						}
						catch (Exception)
						{
							//the exception here is kinda of normal, for functions (especially interesting ones)
							//may have eg punctured points; we just have to keep an eye on the number, since producing 10000
							//exceptions in the multiclient application is not the best idea
                            if (++exCount > MathCommands.MaxCalculationExceptions)
							{
                                Player.Message("Surface drawing is interrupted: too many (>" + MathCommands.MaxCalculationExceptions +
								               ") calculation exceptions.");
								break;
							}
						}
					}
				}
			}
			return _count;
		}

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
				return "Equality";
			}
		}
	}
}
