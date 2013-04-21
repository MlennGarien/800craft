/*        ----
        Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com>
        All rights reserved.

        Redistribution and use in source and binary forms, with or without
        modification, are permitted provided that the following conditions are met:
         * Redistributions of source code must retain the above copyright
              notice, this list of conditions and the following disclaimer.
            * Redistributions in binary form must reproduce the above copyright
             notice, this list of conditions and the following disclaimer in the
             documentation and/or other materials provided with the distribution.
            * Neither the name of 800Craft or the names of its
             contributors may be used to endorse or promote products derived from this
             software without specific prior written permission.

        THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
        ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
        WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
        DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
        DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
        (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
        LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
        ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
        (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
        SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
        ----*/

//Copyright (C) <2011 - 2013> Lao Tszy (lao_tszy@yahoo.co.uk)

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

			player.Message("Expression parsed as " + _expression.Print());
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
