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
	public static class PrepareParametrizedManifold
	{
		public static void SetParametrization(Player p, Command cmd)
		{
			string strFunc = cmd.Next();
			if (string.IsNullOrWhiteSpace(strFunc))
			{
				p.Message("Error: empty parametrization expression");
				return;
			}
			if (strFunc.Length < 3)
			{
				p.Message("Error: expression is too short (should be like x=f(t,u,v))");
				return;
			}

			strFunc = strFunc.ToLower();

			try
			{
				string coordVar = SimpleParser.PreparseAssignment(ref strFunc);
				CheckCoordVar(coordVar);

				Expression expression = SimpleParser.Parse(strFunc, new string[] { "t", "u", "v" });

				p.Message("Expression parsed as " + coordVar + "=" + expression.Print());

				GetPlayerParametrizationCoordsStorage(p)[VarNameToIdx(coordVar[0])] = expression;
			}
			catch (Exception e)
			{
				p.Message("Error: "+e.Message);
			}
		}

		public static void SetParamIteration(Player p, Command cmd)
		{
			string strParam = cmd.Next();
			if (string.IsNullOrWhiteSpace(strParam))
			{
				p.Message("Error: missing param variable name");
				return;
			}

			strParam = strParam.ToLower();

			try
			{
				CheckParamVar(strParam);

				double from = ReadDoubleParam(cmd, "lower bound");
				double to = ReadDoubleParam(cmd, "upper bound");
				double step = ReadDoubleParam(cmd, "step");

				if (step == 0 ||
				    (to - from)/step < 0)
					throw new ArgumentException("wrong iteration bounds/step combination");

				p.Message("Iteration for " + strParam + " from " + from + " to " + to + " with step " + step + ". " +
				          ((to - from)/step + 1) + " steps.");

				GetPlayerParametrizationParamsStorage(p)[VarNameToIdx(strParam[0])] = new double[] {from, to, step};
			}
			catch (Exception e)
			{
				p.Message("Error: " + e.Message);
			}
		}

		public static void ClearParametrization(Player p, Command cmd)
		{
			p.PublicAuxStateObjects.Remove(CoordsStorageName);
			p.PublicAuxStateObjects.Remove(ParamsStorageName);
			p.Message("Prepared parametrization data cleared");
		}

		private static double ReadDoubleParam(Command cmd, string msgParamParamName)
		{
			string s = cmd.Next();
			if (string.IsNullOrWhiteSpace(s))
				throw new ArgumentException("missing param variable " + msgParamParamName);
			double d;
			if (!double.TryParse(s, out d))
				throw new ArgumentException("cannot parse param variable " + msgParamParamName);
			return d;
		}

		public static Expression[] GetPlayerParametrizationCoordsStorage(Player p)
		{
			Object o;
			if (!p.PublicAuxStateObjects.TryGetValue(CoordsStorageName, out o))
			{
				o = new Expression[3];
				p.PublicAuxStateObjects.Add(CoordsStorageName, o);
			}
			return (Expression[]) o;
		}

		public static double[][] GetPlayerParametrizationParamsStorage(Player p)
		{
			Object o;
			if (!p.PublicAuxStateObjects.TryGetValue(ParamsStorageName, out o))
			{
				o = new double[3][];
				p.PublicAuxStateObjects.Add(ParamsStorageName, o);
			}
			return (double[][])o;
		}

		private static string CoordsStorageName { get { return typeof (PrepareParametrizedManifold).Name + "Coords"; } }
		private static string ParamsStorageName { get { return typeof(PrepareParametrizedManifold).Name + "Params"; } }

		private static void CheckCoordVar(string s)
		{
			if (string.IsNullOrWhiteSpace(s) || (s != "x" && s != "y" && s != "z"))
				throw new ArgumentException("expected assignment of x, y, or z (e.g. x=2*t)");
		}
		private static void CheckParamVar(string s)
		{
			if (string.IsNullOrWhiteSpace(s) || (s != "t" && s != "u" && s != "v"))
				throw new ArgumentException("expected parametrization variable name is t, u, or v");
		}
		public static int VarNameToIdx(char varName)
		{
			switch (varName)
			{
				case 'x':
				case 't':
					return 0;
				case 'y':
				case 'u':
					return 1;
				case 'z':
				case 'v':
					return 2;
				default:
					throw new ArgumentException("unknown variable "+varName);
			}
		}
	}
}
