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

            for (Coords.X = Bounds.XMin; Coords.X <= Bounds.XMax && MathCommands.MaxCalculationExceptions >= exCount; ++Coords.X)
			{
                for (Coords.Y = Bounds.YMin; Coords.Y <= Bounds.YMax && MathCommands.MaxCalculationExceptions >= exCount; ++Coords.Y)
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
                                Player.Message("Drawing is interrupted: too many (>" + MathCommands.MaxCalculationExceptions +
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
