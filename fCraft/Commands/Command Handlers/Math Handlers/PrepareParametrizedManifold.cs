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

namespace fCraft {

    public static class PrepareParametrizedManifold {

        public static void SetParametrization( Player p, Command cmd ) {
            string strFunc = cmd.Next();
            if ( string.IsNullOrWhiteSpace( strFunc ) ) {
                p.Message( "Error: empty parametrization expression" );
                return;
            }
            if ( strFunc.Length < 3 ) {
                p.Message( "Error: expression is too short (should be like x=f(t,u,v))" );
                return;
            }

            strFunc = strFunc.ToLower();

            try {
                string coordVar = SimpleParser.PreparseAssignment( ref strFunc );
                CheckCoordVar( coordVar );

                Expression expression = SimpleParser.Parse( strFunc, new string[] { "t", "u", "v" } );

                p.Message( "Expression parsed as " + coordVar + "=" + expression.Print() );

                GetPlayerParametrizationCoordsStorage( p )[VarNameToIdx( coordVar[0] )] = expression;
            } catch ( Exception e ) {
                p.Message( "Error: " + e.Message );
            }
        }

        public static void SetParamIteration( Player p, Command cmd ) {
            string strParam = cmd.Next();
            if ( string.IsNullOrWhiteSpace( strParam ) ) {
                p.Message( "Error: missing param variable name" );
                return;
            }

            strParam = strParam.ToLower();

            try {
                CheckParamVar( strParam );

                double from = ReadDoubleParam( cmd, "lower bound" );
                double to = ReadDoubleParam( cmd, "upper bound" );
                double step = ReadDoubleParam( cmd, "step" );

                if ( step == 0 ||
                    ( to - from ) / step < 0 )
                    throw new ArgumentException( "wrong iteration bounds/step combination" );

                p.Message( "Iteration for " + strParam + " from " + from + " to " + to + " with step " + step + ". " +
                          ( ( to - from ) / step + 1 ) + " steps." );

                GetPlayerParametrizationParamsStorage( p )[VarNameToIdx( strParam[0] )] = new double[] { from, to, step };
            } catch ( Exception e ) {
                p.Message( "Error: " + e.Message );
            }
        }

        public static void ClearParametrization( Player p, Command cmd ) {
            p.PublicAuxStateObjects.Remove( CoordsStorageName );
            p.PublicAuxStateObjects.Remove( ParamsStorageName );
            p.Message( "Prepared parametrization data cleared" );
        }

        private static double ReadDoubleParam( Command cmd, string msgParamParamName ) {
            string s = cmd.Next();
            if ( string.IsNullOrWhiteSpace( s ) )
                throw new ArgumentException( "missing param variable " + msgParamParamName );
            double d;
            if ( !double.TryParse( s, out d ) )
                throw new ArgumentException( "cannot parse param variable " + msgParamParamName );
            return d;
        }

        public static Expression[] GetPlayerParametrizationCoordsStorage( Player p ) {
            Object o;
            if ( !p.PublicAuxStateObjects.TryGetValue( CoordsStorageName, out o ) ) {
                o = new Expression[3];
                p.PublicAuxStateObjects.Add( CoordsStorageName, o );
            }
            return ( Expression[] )o;
        }

        public static double[][] GetPlayerParametrizationParamsStorage( Player p ) {
            Object o;
            if ( !p.PublicAuxStateObjects.TryGetValue( ParamsStorageName, out o ) ) {
                o = new double[3][];
                p.PublicAuxStateObjects.Add( ParamsStorageName, o );
            }
            return ( double[][] )o;
        }

        private static string CoordsStorageName { get { return typeof( PrepareParametrizedManifold ).Name + "Coords"; } }

        private static string ParamsStorageName { get { return typeof( PrepareParametrizedManifold ).Name + "Params"; } }

        private static void CheckCoordVar( string s ) {
            if ( string.IsNullOrWhiteSpace( s ) || ( s != "x" && s != "y" && s != "z" ) )
                throw new ArgumentException( "expected assignment of x, y, or z (e.g. x=2*t)" );
        }

        private static void CheckParamVar( string s ) {
            if ( string.IsNullOrWhiteSpace( s ) || ( s != "t" && s != "u" && s != "v" ) )
                throw new ArgumentException( "expected parametrization variable name is t, u, or v" );
        }

        public static int VarNameToIdx( char varName ) {
            switch ( varName ) {
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
                    throw new ArgumentException( "unknown variable " + varName );
            }
        }
    }
}