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
using System.Data;
using System.Linq;
using System.Text;
using fCraft;
using fCraft.Drawing;

namespace FuncPlugin
{
	public class ManifoldDrawOperation : DrawOperation
	{
		private Scaler _scaler;
		private Expression[] _expressions;
		private double[][] _paramIterations;
		private const double MaxIterationSteps = 1000000;

		public ManifoldDrawOperation(Player p, Command cmd) : base (p)
		{
			_expressions = PrepareParametrizedManifold.GetPlayerParametrizationCoordsStorage(p);
			if (null == _expressions[0])
				throw new InvalidExpressionException("x is undefined");
			if (null == _expressions[1])
				throw new InvalidExpressionException("y is undefined");
			if (null == _expressions[2])
				throw new InvalidExpressionException("z is undefined");

			_paramIterations = PrepareParametrizedManifold.GetPlayerParametrizationParamsStorage(p);
			if (null==_paramIterations[0] && null==_paramIterations[1] && null==_paramIterations[2])
				throw new InvalidExpressionException("all parametrization variables are undefined");

			if (GetNumOfSteps(0) * GetNumOfSteps(1) * GetNumOfSteps(2) > MaxIterationSteps)
				throw new InvalidExpressionException("too many iteration steps (over " + MaxIterationSteps + ")");

			_scaler=new Scaler(cmd.Next());

			p.Message("Going to draw the following parametrization:\nx=" + _expressions[0].Print()+
				"\ny=" + _expressions[1].Print() + "\nz=" + _expressions[2].Print());
		}

		public override string Name
		{
			get { return "ParametrizedManifold"; }
		}

		public override int DrawBatch(int maxBlocksToDraw)
		{
			int count = 0;
			double fromT, toT, stepT;
			double fromU, toU, stepU;
			double fromV, toV, stepV;

			GetIterationBounds(0, out fromT, out toT, out stepT);
			GetIterationBounds(1, out fromU, out toU, out stepU);
			GetIterationBounds(2, out fromV, out toV, out stepV);

			for (double t=fromT; t<=toT; t+=stepT)
			{
				for (double u=fromU; u<=toU; u+=stepU)
				{
					for (double v = fromV; v <= toV; v += stepV)
					{
						Coords.X = _scaler.FromFuncResult(_expressions[0].Evaluate(t, u, v), Bounds.XMin, Bounds.XMax);
						Coords.Y = _scaler.FromFuncResult(_expressions[1].Evaluate(t, u, v), Bounds.YMin, Bounds.YMax);
						Coords.Z = _scaler.FromFuncResult(_expressions[2].Evaluate(t, u, v), Bounds.ZMin, Bounds.ZMax);
						if (DrawOneBlock())
							++count;
						if (TimeToEndBatch)
							return count;
					}
				}
			}
			IsDone = true;
			return count;
		}

		private double GetNumOfSteps(int idx)
		{
			if (null == _paramIterations[idx])
				return 1;
			return (_paramIterations[idx][1] - _paramIterations[idx][0])/_paramIterations[idx][2] + 1;
		}
		private void GetIterationBounds(int idx, out double from, out double to, out double step)
		{
			if (null==_paramIterations[idx])
			{
				from = 0;
				to = 0;
				step = 1;
				return;
			}
			from = _paramIterations[idx][0];
			to = _paramIterations[idx][1];
			step = _paramIterations[idx][2];
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
	}
}
