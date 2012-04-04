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
	//scales coords according to the defined possibilities
	public class Scaler
	{
		//scalings:
		//ZeroToMaxBound means that every dimension of the selected area is measured from 0 to its size in cubes minus 1
		//Normalized means that every dimension is measured from 0 to 1
		private enum Scaling
		{
			ZeroToMaxBound,
			Normalized,
			DoubleNormalized,
		}
		private Scaling _scaling;
		
		public Scaler(string scaling)
		{
			if (string.IsNullOrWhiteSpace(scaling))
				_scaling = Scaling.ZeroToMaxBound;
			else if (scaling.ToLower() == "u")
				_scaling = Scaling.Normalized;
			else if (scaling.ToLower() == "uu")
				_scaling = Scaling.DoubleNormalized;
			else
				throw new ArgumentException("unrecognized scaling "+scaling);
		}

		public double ToFuncParam(double coord, double min, double max)
		{
			switch (_scaling)
			{
				case Scaling.ZeroToMaxBound:
					return coord - min;
				case Scaling.Normalized:
					return (coord - min) / Math.Max(1, max - min);
				case Scaling.DoubleNormalized:
					return max == min ? 0 : 2.0*(coord - min)/Math.Max(1, max - min) - 1;
				default:
					throw new Exception("unknown scaling");
			}
		}

		public int FromFuncResult(double result, double min, double max)
		{
			switch (_scaling)
			{
				case Scaling.ZeroToMaxBound:
					return (int)(result + min);
				case Scaling.Normalized:
					return (int)(result * Math.Max(1, max - min) + min);
				case Scaling.DoubleNormalized:
					return (int) ((result + 1)*Math.Max(1, max - min)/2.0 + min);
				default:
					throw new Exception("unknown scaling");
			}
		}
	}
}
