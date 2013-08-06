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

using System.Data;
using fCraft.Drawing;

namespace fCraft {

    public class ManifoldDrawOperation : DrawOperation {
        private Scaler _scaler;
        private Expression[] _expressions;
        private double[][] _paramIterations;
        private const double MaxIterationSteps = 1000000;

        public ManifoldDrawOperation( Player p, Command cmd )
            : base( p ) {
            _expressions = PrepareParametrizedManifold.GetPlayerParametrizationCoordsStorage( p );
            if ( null == _expressions[0] )
                throw new InvalidExpressionException( "x is undefined" );
            if ( null == _expressions[1] )
                throw new InvalidExpressionException( "y is undefined" );
            if ( null == _expressions[2] )
                throw new InvalidExpressionException( "z is undefined" );

            _paramIterations = PrepareParametrizedManifold.GetPlayerParametrizationParamsStorage( p );
            if ( null == _paramIterations[0] && null == _paramIterations[1] && null == _paramIterations[2] )
                throw new InvalidExpressionException( "all parametrization variables are undefined" );

            if ( GetNumOfSteps( 0 ) * GetNumOfSteps( 1 ) * GetNumOfSteps( 2 ) > MaxIterationSteps )
                throw new InvalidExpressionException( "too many iteration steps (over " + MaxIterationSteps + ")" );

            _scaler = new Scaler( cmd.Next() );

            p.Message( "Going to draw the following parametrization:\nx=" + _expressions[0].Print() +
                "\ny=" + _expressions[1].Print() + "\nz=" + _expressions[2].Print() );
        }

        public override string Name {
            get { return "ParametrizedManifold"; }
        }

        public override int DrawBatch( int maxBlocksToDraw ) {
            int count = 0;
            double fromT, toT, stepT;
            double fromU, toU, stepU;
            double fromV, toV, stepV;

            GetIterationBounds( 0, out fromT, out toT, out stepT );
            GetIterationBounds( 1, out fromU, out toU, out stepU );
            GetIterationBounds( 2, out fromV, out toV, out stepV );

            for ( double t = fromT; t <= toT; t += stepT ) {
                for ( double u = fromU; u <= toU; u += stepU ) {
                    for ( double v = fromV; v <= toV; v += stepV ) {
                        Coords.X = _scaler.FromFuncResult( _expressions[0].Evaluate( t, u, v ), Bounds.XMin, Bounds.XMax );
                        Coords.Y = _scaler.FromFuncResult( _expressions[1].Evaluate( t, u, v ), Bounds.YMin, Bounds.YMax );
                        Coords.Z = _scaler.FromFuncResult( _expressions[2].Evaluate( t, u, v ), Bounds.ZMin, Bounds.ZMax );
                        if ( DrawOneBlock() )
                            ++count;
                        //if (TimeToEndBatch)
                        //    return count;
                    }
                }
            }
            IsDone = true;
            return count;
        }

        private double GetNumOfSteps( int idx ) {
            if ( null == _paramIterations[idx] )
                return 1;
            return ( _paramIterations[idx][1] - _paramIterations[idx][0] ) / _paramIterations[idx][2] + 1;
        }

        private void GetIterationBounds( int idx, out double from, out double to, out double step ) {
            if ( null == _paramIterations[idx] ) {
                from = 0;
                to = 0;
                step = 1;
                return;
            }
            from = _paramIterations[idx][0];
            to = _paramIterations[idx][1];
            step = _paramIterations[idx][2];
        }

        public override bool Prepare( Vector3I[] marks ) {
            if ( !base.Prepare( marks ) ) {
                return false;
            }
            BlocksTotalEstimate = Bounds.Volume;
            return true;
        }
    }
}