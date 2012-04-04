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

namespace FuncPlugin
{
	//draws volume, defined by an inequality 
	public class InequalityDrawOperation : DrawOperation
	{
		private Expression _expression;
		private Scaler _scaler;
		private int _count;

		public InequalityDrawOperation(Player player, Command cmd)
			: base(player)
		{
			string strFunc = cmd.Next();
			if (string.IsNullOrWhiteSpace(strFunc))
				throw new ArgumentException("empty inequality expression");
			if (strFunc.Length < 3)
				throw new ArgumentException("expression is too short (should be like f(x,y,z)>g(x,y,z))");
			
			strFunc = strFunc.ToLower();

			_expression = SimpleParser.Parse(strFunc, new string[] { "x", "y", "z" });
			if (!_expression.IsInEquality())
				throw new ArgumentException("the expression given is not an inequality (should be like f(x,y,z)>g(x,y,z))");

			Player.Message("Expression parsed as " + _expression.Print());
			string scalingStr = cmd.Next();
			_scaler = new Scaler(scalingStr);
		}

		public override int DrawBatch(int maxBlocksToDraw)
		{
			//ignoring maxBlocksToDraw
			_count = 0;
			int exCount = 0;

			for (Coords.X = Bounds.XMin; Coords.X <= Bounds.XMax && Init.MaxCalculationExceptions >= exCount; ++Coords.X)
			{
				for (Coords.Y = Bounds.YMin; Coords.Y <= Bounds.YMax && Init.MaxCalculationExceptions >= exCount; ++Coords.Y)
				{
					for (Coords.Z = Bounds.ZMin; Coords.Z <= Bounds.ZMax; ++Coords.Z)
					{
						try
						{
							if (_expression.Evaluate(_scaler.ToFuncParam(Coords.X, Bounds.XMin, Bounds.XMax),
									                 _scaler.ToFuncParam(Coords.Y, Bounds.YMin, Bounds.YMax),
													 _scaler.ToFuncParam(Coords.Z, Bounds.ZMin, Bounds.ZMax))>0) //1.0 means true
							{
								if (DrawOneBlock())
									++_count;
							}
							if (TimeToEndBatch)
								return _count;
						}
						catch (Exception e)
						{
							//the exception here is kinda of normal, for functions (especially interesting ones)
							//may have eg punctured points; we just have to keep an eye on the number, since producing 10000
							//exceptions in the multiclient application is not the best idea
							if (++exCount > Init.MaxCalculationExceptions)
							{
								Player.Message("Drawing is interrupted: too many (>" + Init.MaxCalculationExceptions +
								               ") calculation exceptions.");
								break;
							}
						}
					}
				}
			}

			IsDone = true;
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
				return "Inequality";
			}
		}
	}
}
