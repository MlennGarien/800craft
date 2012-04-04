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
	public class Init : Plugin
	{
		public const int MaxCalculationExceptions = 100;

		public void Initialize()
        {
			//place the commands into math category if defined, otherwise use Building
			CommandCategory category = Enum.IsDefined(typeof (CommandCategory), CommandCategory.Math)
			                           	? CommandCategory.Math
			                           	: CommandCategory.Building;

			Logger.Log(LogType.ConsoleOutput, Name + "(v " + Version + "): Registering function commands");

			const string commonFuncHelp = "Can also be x=f(y, z) or y=f(x, z). ";
			const string commonHelp =
				"Allowed operators: +, -, *, /, %, ^. Comparison and logical operators: >, <, =, &, |, !." +
				"Constants: e, pi. " +
				"Functions: sqrt, sq, exp, lg, ln, log(num, base), abs, sign, sin, cos, tan, sinh, cosh, tanh. Example: 1-exp(-1/sq(x)). " +
				"'sq' stands for 'square', i.e. sq(x) is x*x. " +
				"Select 2 points to define a volume (same as e.g. for cuboid), where the function will be drawn. " +
				"Coords are whole numbers from 0 to the corresponding cuboid dimension length. ";
			const string commonScalingHelp =
				"Using 'u' as a scaling switch coords to be [0, 1] along the corresponding cuboid axis. " +
				"'uu' switches coords to be [-1, 1] along the corresponding cuboid axis.";
			const string copyright="\n(C) 2012 Lao Tszy";
			//f(x, y)
			CommandManager.RegisterCustomCommand(
				new CommandDescriptor
					{
						Name = "Func",
						Aliases = new string[] {"fu"},
						Category = category,
						Permissions = new Permission[] {Permission.DrawAdvanced},
						RepeatableSelection = true,
						Help =
							"Draws a 3d function values grid, i.e. this variant doest care to fill gaps between too distinct values. " +
							commonFuncHelp + commonHelp + commonScalingHelp + copyright,
						Usage = "/fu z=<f(x, y) expression> [scaling]",
						Handler = FuncHandler,
					});
			CommandManager.RegisterCustomCommand(
				new CommandDescriptor
					{
						Name = "FuncSurf",
						Aliases = new string[] { "fus" },
						Category = category,
						Permissions = new Permission[] { Permission.DrawAdvanced },
						RepeatableSelection = true,
						Help =
							"Draws a 3d function surface. " +
							commonFuncHelp + commonHelp + commonScalingHelp + copyright,
						Usage = "/fus <f(x, y) expression> [scaling]",
						Handler = FuncSHandler,
					});
			CommandManager.RegisterCustomCommand(
				new CommandDescriptor
				{
					Name = "FuncFill",
					Aliases = new string[] { "fuf" },
					Category = category,
					Permissions = new Permission[] { Permission.DrawAdvanced },
					RepeatableSelection = true,
					Help =
						"Draws a 3d function filling the area under it. " +
						commonFuncHelp + commonHelp + commonScalingHelp + copyright,
					Usage = "/fuf <f(x, y) expression> [scaling]",
					Handler = FuncFHandler,
				});

			//inequality
			CommandManager.RegisterCustomCommand(
				new CommandDescriptor
				{
					Name = "Ineq",
					Aliases = new string[] { "ie" },
					Category = category,
					Permissions = new Permission[] { Permission.DrawAdvanced },
					RepeatableSelection = true,
					Help =
						"Draws a volume where the specified inequality holds. " +
						commonHelp + commonScalingHelp + copyright,
					Usage = "/ineq f(x, y, z)>g(x, y, z) [scaling]",
					Handler = InequalityHandler,
				});

			//equality
			CommandManager.RegisterCustomCommand(
				new CommandDescriptor
				{
					Name = "Eq",
					Aliases = new string[] { },
					Category = category,
					Permissions = new Permission[] { Permission.DrawAdvanced },
					RepeatableSelection = true,
					Help =
						"Draws a volume where the specified equality holds. " +
						commonHelp + commonScalingHelp + copyright,
					Usage = "/eq f(x, y, z)=g(x, y, z) [scaling]",
					Handler = EqualityHandler,
				});

			//parametrized manifold
			CommandManager.RegisterCustomCommand(
				new CommandDescriptor
				{
					Name = "SetCoordParm",
					Aliases = new string[] { "SetCP", "scp" },
					Category = category,
					Permissions = new Permission[] { Permission.DrawAdvanced },
					RepeatableSelection = true,
					Help =
						"Sets the parametrization function for a coordinate (x, y, or z). "+
						"Example: /scp x=2*t+sin(u*v). " +
						commonHelp + copyright,
					Usage = "/scp <coord variable>=<expression of f(t, u, v)>",
					Handler = PrepareParametrizedManifold.SetParametrization,
				});

			CommandManager.RegisterCustomCommand(
				new CommandDescriptor
				{
					Name = "SetParamIter",
					Aliases = new string[] { "SetPI", "spi" },
					Category = category,
					Permissions = new Permission[] { Permission.DrawAdvanced },
					RepeatableSelection = true,
					Help =
						"Sets the loop for iteration of the parameter variable (t, u, or v). "+
						"Example: /spi t 0 3.14 0.314" +
						copyright,
					Usage = "/spi <param variable> <from> <to> <step>",
					Handler = PrepareParametrizedManifold.SetParamIteration,
				});
			CommandManager.RegisterCustomCommand(
				new CommandDescriptor
				{
					Name = "StartParmDraw",
					Aliases = new string[] { "StartPD", "spd" },
					Category = category,
					Permissions = new Permission[] { Permission.DrawAdvanced },
					RepeatableSelection = true,
					Help =
						"Usage: /spd [scaling]. Starts drawing the parametrization prepared by commands SetCoordParametrization and SetParamIteration." +
						commonScalingHelp + copyright,
					Handler = StartParametrizedDraw,
				});
			CommandManager.RegisterCustomCommand(
				new CommandDescriptor
				{
					Name = "ClearParmDraw",
					Aliases = new string[] { "ClearPD", "cpd" },
					Category = category,
					Permissions = new Permission[] { Permission.DrawAdvanced },
					RepeatableSelection = true,
					Help =
						"Deletes expressions prepared by commands SetCoordParametrization and SetParamIteration." + copyright,
					Usage = "/cpd",
					Handler = PrepareParametrizedManifold.ClearParametrization,
				});
        }

        public string Name
        {
            get
            {
                return "FuncPlugin";
            }
            set
            {
                Name = value;
            }
        }

        public string Version
        {
            get
            {
                return "1.1";
            }
            set
            {
                Version = value;
            }
        }
		
		//Those handler functions would be a template function when this <censored> c# could 
		//accept constructors with params for the template param types.
		//One still can use two-fase-construction to enable templetization here,
		//but this seems to me even uglier than copy-pasted handlers
		private static void FuncHandler(Player player, Command cmd)
		{
			try
			{
				FuncDrawOperation operation=new FuncDrawOperationPoints(player, cmd);
				DrawOperationBegin(player, cmd, operation);
			}
			catch (Exception e)
			{
				player.Message("Error: "+e.Message);
			}
		}
		private static void FuncSHandler(Player player, Command cmd)
		{
			try
			{
				FuncDrawOperation operation = new FuncDrawOperationSurface(player, cmd);
				DrawOperationBegin(player, cmd, operation);
			}
			catch (Exception e)
			{
				player.Message("Error: " + e.Message);
			}
		}
		private static void FuncFHandler(Player player, Command cmd)
		{
			try
			{
				FuncDrawOperation operation = new FuncDrawOperationFill(player, cmd);
				DrawOperationBegin(player, cmd, operation);
			}
			catch (Exception e)
			{
				player.Message("Error: " + e.Message);
			}
		}
		private static void InequalityHandler(Player player, Command cmd)
		{
			try
			{
				InequalityDrawOperation operation = new InequalityDrawOperation(player, cmd);
				DrawOperationBegin(player, cmd, operation);
			}
			catch (Exception e)
			{
				player.Message("Error: " + e.Message);
			}
		}
		private static void EqualityHandler(Player player, Command cmd)
		{
			try
			{
				EqualityDrawOperation operation = new EqualityDrawOperation(player, cmd);
				DrawOperationBegin(player, cmd, operation);
			}
			catch (Exception e)
			{
				player.Message("Error: " + e.Message);
			}
		}
		private static void StartParametrizedDraw(Player player, Command cmd)
		{
			try
			{
				ManifoldDrawOperation operation=new ManifoldDrawOperation(player, cmd);
				DrawOperationBegin(player, cmd, operation);
			}
			catch (Exception e)
			{
				player.Message("Error: " + e.Message);
			}
		}
		

		//copy-paste from BuildingCommands
		private static void DrawOperationBegin(Player player, Command cmd, DrawOperation op)
		{
			IBrushInstance instance = player.Brush.MakeInstance(player, cmd, op);
			if (instance != null)
			{
				op.Brush = instance;
				player.SelectionStart(op.ExpectedMarks, new SelectionCallback(DrawOperationCallback), op, new Permission[] { Permission.DrawAdvanced });
				player.Message("{0}: Click {1} blocks or use &H/Mark&S to make a selection.", new object[] { op.Description, op.ExpectedMarks });
			}
		}
		private static void DrawOperationCallback(Player player, Vector3I[] marks, object tag)
		{
			DrawOperation operation = (DrawOperation)tag;
			if (operation.Prepare(marks))
			{
				if (!player.CanDraw(operation.BlocksTotalEstimate))
				{
					player.Message("You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.", new object[] { player.Info.Rank.DrawLimit, operation.Bounds.Volume });
					operation.Cancel();
				}
				else
				{
					player.Message("{0}: Processing ~{1} blocks.", new object[] { operation.Description, operation.BlocksTotalEstimate });
					operation.Begin();
				}
			}
		}

	}
}
