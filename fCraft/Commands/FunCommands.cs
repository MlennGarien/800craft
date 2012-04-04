using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomMaze;

namespace fCraft
{
	internal static class FunCommands
	{
		internal static void Init()
		{
			CommandManager.RegisterCustomCommand(
				new CommandDescriptor
				{
					Name = "RandomMaze",
					Aliases = new string[] { "maze" },
					Category = CommandCategory.Fun,
					Permissions = new Permission[] { Permission.DrawAdvanced },
					RepeatableSelection = true,
					Help =
						"Choose the size (width, length and height) and it will draw a random maze at the chosen point. " +
						"Optional parameters tell if the lifts are to be drawn and if hint blocks (log) are to be added. \n(C) 2012 Lao Tszy",
					Usage = "/randommaze <width> <length> <height> [nolifts] [hints]",
					Handler = MazeHandler,
				});
			CommandManager.RegisterCustomCommand(
				new CommandDescriptor
				{
					Name = "MazeCuboid",
					Aliases = new string[] { "mc", "mz" },
					Category = CommandCategory.Fun,
					Permissions = new Permission[] { Permission.DrawAdvanced },
					RepeatableSelection = true,
					Help =
						"Draws a cuboid with the current brush and with a random maze inside.\n(C) 2012 Lao Tszy",
					Usage = "/MazeCuboid [block type]",
					Handler = MazeCuboidHandler,
				});
		}

		private static void MazeHandler(Player p, Command cmd)
		{
			try
			{
				RandomMazeDrawOperation op = new RandomMazeDrawOperation(p, cmd);
				BuildingCommands.DrawOperationBegin(p, cmd, op);
			}
			catch (Exception e)
			{
				Player.Console.Message("Error: "+ e.Message);
			}
		}
		private static void MazeCuboidHandler(Player p, Command cmd)
		{
			try
			{
				MazeCuboidDrawOperation op = new MazeCuboidDrawOperation(p);
				BuildingCommands.DrawOperationBegin(p, cmd, op);
			}
			catch (Exception e)
			{
				Player.Console.Message("Error: " + e.Message);
			}
		}
	}
}
