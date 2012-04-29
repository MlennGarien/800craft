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

namespace fCraft
{
	public static class PrepareParametrizedManifold
	{
        public static bool IsNullOrWhiteSpace(string value)
        {
            if (value == null) return true;
            return string.IsNullOrEmpty(value.Trim());
        }
		public static void SetParametrization(Player p, Command cmd)
		{
			string strFunc = cmd.Next();
			if (IsNullOrWhiteSpace(strFunc))
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
			if (IsNullOrWhiteSpace(strParam))
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
			if (IsNullOrWhiteSpace(s))
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
			if (IsNullOrWhiteSpace(s) || (s != "x" && s != "y" && s != "z"))
				throw new ArgumentException("expected assignment of x, y, or z (e.g. x=2*t)");
		}
		private static void CheckParamVar(string s)
		{
			if (IsNullOrWhiteSpace(s) || (s != "t" && s != "u" && s != "v"))
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
