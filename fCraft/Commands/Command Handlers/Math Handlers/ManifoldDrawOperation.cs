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
using System.Data;
using System.Linq;
using System.Text;
using fCraft;
using fCraft.Drawing;

namespace fCraft
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
						//if (TimeToEndBatch)
						//    return count;
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
