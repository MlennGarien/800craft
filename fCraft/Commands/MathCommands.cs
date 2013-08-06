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

using System;
using fCraft.Drawing;

namespace fCraft {

    public class MathCommands {
        public const int MaxCalculationExceptions = 100;

        public static void Init() {
            CommandManager.RegisterCommand( CdFunc );
            CommandManager.RegisterCommand( CdFuncSurf );
            CommandManager.RegisterCommand( CdFuncFill );
            CommandManager.RegisterCommand( CdIneq );
            CommandManager.RegisterCommand( CdEq );
            CommandManager.RegisterCommand( CdSetCoord );
            CommandManager.RegisterCommand( CdSetParam );
            CommandManager.RegisterCommand( CdStartParam );
            CommandManager.RegisterCommand( CdClearParam );
        }

        private const string commonFuncHelp = "Can also be x=f(y, z) or y=f(x, z). ";

        private const string commonHelp =
            "Allowed operators: +, -, *, /, %, ^. Comparison and logical operators: >, <, =, &, |, !." +
            "Constants: e, pi. " +
            "Functions: sqrt, sq, exp, lg, ln, log(num, base), abs, sign, sin, cos, tan, sinh, cosh, tanh. Example: 1-exp(-1/sq(x)). " +
            "'sq' stands for 'square', i.e. sq(x) is x*x. ";

        private const string commonScalingHelp =
            "Select 2 points to define a volume (same as e.g. for cuboid), where the function will be drawn. " +
            "Coords are whole numbers from 0 to the corresponding cuboid dimension length. " +
            "Using 'u' as a scaling switch coords to be [0, 1] along the corresponding cuboid axis. " +
            "'uu' switches coords to be [-1, 1] along the corresponding cuboid axis.";

        private const string copyright = "\n(C) 2012 Lao Tszy";

        private static readonly CommandDescriptor CdFunc = new CommandDescriptor {
            Name = "Func",
            Aliases = new string[] { "fu" },
            Category = CommandCategory.Math,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help =
                "Draws a 3d function values grid, i.e. this variant doest care to fill gaps between too distinct values. " +
                commonFuncHelp + commonHelp + commonScalingHelp + copyright,
            Usage = "/fu z=<f(x, y) expression> [scaling]",
            Handler = FuncHandler,
        };

        private static readonly CommandDescriptor CdFuncSurf = new CommandDescriptor {
            Name = "FuncSurf",
            Aliases = new string[] { "fus" },
            Category = CommandCategory.Math,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help =
                "Draws a 3d function surface. " +
                commonFuncHelp + commonHelp + commonScalingHelp + copyright,
            Usage = "/fus <f(x, y) expression> [scaling]",
            Handler = FuncSHandler,
        };

        private static readonly CommandDescriptor CdFuncFill = new CommandDescriptor {
            Name = "FuncFill",
            Aliases = new string[] { "fuf" },
            Category = CommandCategory.Math,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help =
                "Draws a 3d function filling the area under it. " +
                commonFuncHelp + commonHelp + commonScalingHelp + copyright,
            Usage = "/fuf <f(x, y) expression> [scaling]",
            Handler = FuncFHandler,
        };

        //inequality
        private static readonly CommandDescriptor CdIneq = new CommandDescriptor {
            Name = "Ineq",
            Aliases = new string[] { "ie" },
            Category = CommandCategory.Math,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help =
                "Draws a volume where the specified inequality holds. " +
                commonHelp + commonScalingHelp + copyright,
            Usage = "/ineq f(x, y, z)>g(x, y, z) [scaling]",
            Handler = InequalityHandler,
        };

        //equality
        private static readonly CommandDescriptor CdEq = new CommandDescriptor {
            Name = "Eq",
            Aliases = new string[] { },
            Category = CommandCategory.Math,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help =
                "Draws a volume where the specified equality holds. " +
                commonHelp + commonScalingHelp + copyright,
            Usage = "/eq f(x, y, z)=g(x, y, z) [scaling]",
            Handler = EqualityHandler,
        };

        //parametrized manifold
        private static readonly CommandDescriptor CdSetCoord = new CommandDescriptor {
            Name = "SetCoordParm",
            Aliases = new string[] { "SetCP", "scp" },
            Category = CommandCategory.Math,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help =
                "Sets the parametrization function for a coordinate (x, y, or z). " +
                "Example: /scp x=2*t+sin(u*v). " +
                commonHelp + copyright,
            Usage = "/scp <coord variable>=<expression of f(t, u, v)>",
            Handler = PrepareParametrizedManifold.SetParametrization,
        };

        private static readonly CommandDescriptor CdSetParam = new CommandDescriptor {
            Name = "SetParamIter",
            Aliases = new string[] { "SetPI", "spi" },
            Category = CommandCategory.Math,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help =
                "Sets the loop for iteration of the parameter variable (t, u, or v). " +
                "Example: /spi t 0 3.14 0.314" +
                copyright,
            Usage = "/spi <param variable> <from> <to> <step>",
            Handler = PrepareParametrizedManifold.SetParamIteration,
        };

        private static readonly CommandDescriptor CdStartParam = new CommandDescriptor {
            Name = "StartParmDraw",
            Aliases = new string[] { "StartPD", "spd" },
            Category = CommandCategory.Math,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help =
                "Usage: /spd [scaling]. Starts drawing the parametrization prepared by commands SetCoordParametrization and SetParamIteration." +
                commonScalingHelp + copyright,
            Handler = StartParametrizedDraw,
        };

        private static readonly CommandDescriptor CdClearParam = new CommandDescriptor {
            Name = "ClearParmDraw",
            Aliases = new string[] { "ClearPD", "cpd" },
            Category = CommandCategory.Math,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help =
                "Deletes expressions prepared by commands SetCoordParametrization and SetParamIteration." + copyright,
            Usage = "/cpd",
            Handler = PrepareParametrizedManifold.ClearParametrization,
        };

        //Those handler functions would be a template function when this <censored> c# could
        //accept constructors with params for the template param types.
        //One still can use two-fase-construction to enable templetization here,
        //but this seems to me even uglier than copy-pasted handlers
        private static void FuncHandler( Player player, Command cmd ) {
            FuncDrawOperation operation = new FuncDrawOperationPoints( player, cmd );
            DrawOperationBegin( player, cmd, operation );
        }

        private static void FuncSHandler( Player player, Command cmd ) {
            FuncDrawOperation operation = new FuncDrawOperationSurface( player, cmd );
            DrawOperationBegin( player, cmd, operation );
        }

        private static void FuncFHandler( Player player, Command cmd ) {
            try {
                FuncDrawOperation operation = new FuncDrawOperationFill( player, cmd );
                DrawOperationBegin( player, cmd, operation );
            } catch ( Exception e ) {
                player.Message( "Error: " + e.Message );
            }
        }

        private static void InequalityHandler( Player player, Command cmd ) {
            try {
                InequalityDrawOperation operation = new InequalityDrawOperation( player, cmd );
                DrawOperationBegin( player, cmd, operation );
            } catch ( Exception e ) {
                player.Message( "Error: " + e.Message );
            }
        }

        private static void EqualityHandler( Player player, Command cmd ) {
            try {
                EqualityDrawOperation operation = new EqualityDrawOperation( player, cmd );
                DrawOperationBegin( player, cmd, operation );
            } catch ( Exception e ) {
                player.Message( "Error: " + e.Message );
            }
        }

        private static void StartParametrizedDraw( Player player, Command cmd ) {
            try {
                ManifoldDrawOperation operation = new ManifoldDrawOperation( player, cmd );
                DrawOperationBegin( player, cmd, operation );
            } catch ( Exception e ) {
                player.Message( "Error: " + e.Message );
            }
        }

        //copy-paste from BuildingCommands
        private static void DrawOperationBegin( Player player, Command cmd, DrawOperation op ) {
            IBrushInstance instance = player.Brush.MakeInstance( player, cmd, op );
            if ( instance != null ) {
                op.Brush = instance;
                player.SelectionStart( op.ExpectedMarks, new SelectionCallback( DrawOperationCallback ), op, new Permission[] { Permission.DrawAdvanced } );
                player.Message( "{0}: Click {1} blocks or use &H/Mark&S to make a selection.", new object[] { op.Description, op.ExpectedMarks } );
            }
        }

        private static void DrawOperationCallback( Player player, Vector3I[] marks, object tag ) {
            DrawOperation operation = ( DrawOperation )tag;
            if ( operation.Prepare( marks ) ) {
                if ( !player.CanDraw( operation.BlocksTotalEstimate ) ) {
                    player.Message( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.", new object[] { player.Info.Rank.DrawLimit, operation.Bounds.Volume } );
                    operation.Cancel();
                } else {
                    player.Message( "{0}: Processing ~{1} blocks.", new object[] { operation.Description, operation.BlocksTotalEstimate } );
                    operation.Begin();
                }
            }
        }
    }
}