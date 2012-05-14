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
