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
