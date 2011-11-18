using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using fCraft.MapConversion;
using JetBrains.Annotations;
using fCraft.Drawing;
using fCraft.Portals;
using System.Collections;
using System.Text;
using System.Threading;

namespace fCraft
{
    class RealmHandler
    {
        public static void RealmLoad(Player player, Command cmd, string fileName, string worldName)
        {
            if (worldName == null && player.World == null)
            {
                player.Message("When using /WLoad from console, you must specify the world name.");
                return;
            }

            if (fileName == null)
            {
                // No params given at all
                
                return;
            }

            string fullFileName = WorldManager.FindMapFile(player, fileName);
            if (fullFileName == null) return;

            // Loading map into current world
            if (worldName == null)
            {
                if (!cmd.IsConfirmed)
                {
                    player.Confirm(cmd, "About to replace THIS MAP with \"{0}\".", fileName);
                    return;
                }
                Map map;
                try
                {
                    map = MapUtility.Load(fullFileName);
                }
                catch (Exception ex)
                {
                    player.MessageNow("Could not load specified file: {0}: {1}", ex.GetType().Name, ex.Message);
                    return;
                }
                World world = player.World;

                // Loading to current world
                world.MapChangedBy = player.Name;
                world.ChangeMap(map);

                world.Players.Message(player, "{0}&S loaded a new map for this world.",
                                              player.ClassyName);
                player.MessageNow("New map loaded for the world {0}", world.ClassyName);

                Logger.Log(LogType.UserActivity,
                            "{0} loaded new map for world \"{1}\" from {2}",
                            player.Name, world.Name, fileName);
                world.IsRealm = true;


            }
            else
            {
                // Loading to some other (or new) world
                if (!World.IsValidName(worldName))
                {
                    player.MessageInvalidWorldName(worldName);
                    return;
                }

                string buildRankName = cmd.Next();
                string accessRankName = cmd.Next();
                Rank buildRank = RankManager.DefaultBuildRank;
                Rank accessRank = null;
                if (buildRankName != null)
                {
                    buildRank = RankManager.FindRank(buildRankName);
                    if (buildRank == null)
                    {
                        player.MessageNoRank(buildRankName);
                        return;
                    }
                    if (accessRankName != null)
                    {
                        accessRank = RankManager.FindRank(accessRankName);
                        if (accessRank == null)
                        {
                            player.MessageNoRank(accessRankName);
                            return;
                        }
                    }
                }

                // Retype world name, if needed
                if (worldName == "-")
                {
                    if (player.LastUsedWorldName != null)
                    {
                        worldName = player.LastUsedWorldName;
                    }
                    else
                    {
                        player.Message("Cannot repeat world name: you haven't used any names yet.");
                        return;
                    }
                }

                lock (WorldManager.SyncRoot)
                {
                    World world = WorldManager.FindWorldExact(worldName);
                    if (world != null)
                    {
                        player.LastUsedWorldName = world.Name;
                        // Replacing existing world's map
                        if (!cmd.IsConfirmed)
                        {
                            player.Confirm(cmd, "About to replace map for {0}&S with \"{1}\".",
                                            world.ClassyName, fileName);
                            return;
                        }

                        Map map;
                        try
                        {
                            map = MapUtility.Load(fullFileName);
                            world.IsRealm = true;
                        }
                        catch (Exception ex)
                        {
                            player.MessageNow("Could not load specified file: {0}: {1}", ex.GetType().Name, ex.Message);
                            return;
                        }

                        try
                        {
                            world.MapChangedBy = player.Name;
                            world.ChangeMap(map);
                            world.IsRealm = true;
                        }
                        catch (WorldOpException ex)
                        {
                            Logger.Log(LogType.Error,
                                        "Could not complete WorldLoad operation: {0}", ex.Message);
                            player.Message("&WWLoad: {0}", ex.Message);
                            return;
                        }

                        world.Players.Message(player, "{0}&S loaded a new map for the world {1}",
                                               player.ClassyName, world.ClassyName);
                        player.MessageNow("New map for the world {0}&S has been loaded.", world.ClassyName);
                        Logger.Log(LogType.UserActivity,
                                    "{0} loaded new map for world \"{1}\" from {2}",
                                    player.Name, world.Name, fullFileName);

                    }
                    else
                    {
                        // Adding a new world
                        string targetFullFileName = Path.Combine(Paths.MapPath, worldName + ".fcm");
                        if (!cmd.IsConfirmed &&
                            File.Exists(targetFullFileName) && // target file already exists
                            !Paths.Compare(targetFullFileName, fullFileName))
                        {
                            // and is different from sourceFile
                            player.Confirm(cmd,
                                            "A map named \"{0}\" already exists, and will be overwritten with \"{1}\".",
                                            Path.GetFileName(targetFullFileName), Path.GetFileName(fullFileName));
                            return;
                        }

                        Map map;
                        try
                        {
                            map = MapUtility.Load(fullFileName);
                            world.IsRealm = true;
                        }
                        catch (Exception ex)
                        {
                            player.MessageNow("Could not load \"{0}\": {1}: {2}",
                                               fileName, ex.GetType().Name, ex.Message);
                            return;
                        }

                        World newWorld;
                        try
                        {
                            newWorld = WorldManager.AddWorld(player, worldName, map, false);
                            world.IsRealm = true;
                        }
                        catch (WorldOpException ex)
                        {
                            player.Message("WLoad: {0}", ex.Message);
                            return;
                        }

                        if (newWorld == null)
                        {
                            player.MessageNow("Failed to create a new world.");
                            return;
                        }

                        player.LastUsedWorldName = worldName;
                        newWorld.BuildSecurity.MinRank = buildRank;
                        if (accessRank == null)
                        {
                            newWorld.AccessSecurity.ResetMinRank();
                        }
                        else
                        {
                            newWorld.AccessSecurity.MinRank = accessRank;
                        }
                        newWorld.BlockDB.AutoToggleIfNeeded();
                        if (BlockDB.IsEnabledGlobally && newWorld.BlockDB.IsEnabled)
                        {
                            player.Message("BlockDB is now auto-enabled on world {0}", newWorld.ClassyName);
                        }
                        newWorld.LoadedBy = player.Name;
                        newWorld.LoadedOn = DateTime.UtcNow;
                        Server.Message("{0}&S created a new world named {1}",
                                        player.ClassyName, newWorld.ClassyName);
                        Logger.Log(LogType.UserActivity,
                                    "{0} created a new world named \"{1}\" (loaded from \"{2}\")",
                                    player.Name, worldName, fileName);
                        WorldManager.SaveWorldList();
                        player.MessageNow("Access permission is {0}+&S, and build permission is {1}+",
                                           newWorld.AccessSecurity.MinRank.ClassyName,
                                           newWorld.BuildSecurity.MinRank.ClassyName);
                    }
                }
            }

            Server.RequestGC();
        }
        public static void RealmCreate(Player player, Command cmd, string themeName, string templateName)
        {
            MapGenTemplate template;
            MapGenTheme theme;

            int wx, wy, height = 128;
            if (!(cmd.NextInt(out wx) && cmd.NextInt(out wy) && cmd.NextInt(out height)))
            {
                if (player.World != null)
                {
                    wx = 128;
                    wy = 128;
                    height = 128;
                }
                else
                {
                    player.Message("When used from console, /gen requires map dimensions.");
                    
                    return;
                }
                cmd.Rewind();
                cmd.Next();
                cmd.Next();
            }

            if (!Map.IsValidDimension(wx))
            {
                player.Message("Cannot make map with width {0}: dimensions must be multiples of 16.", wx);
                return;
            }
            else if (!Map.IsValidDimension(wy))
            {
                player.Message("Cannot make map with length {0}: dimensions must be multiples of 16.", wy);
                return;
            }
            else if (!Map.IsValidDimension(height))
            {
                player.Message("Cannot make map2 with height {0}: dimensions must be multiples of 16.", height);
                return;
            }

            string fileName = player.Name;
            string fullFileName = null;

            if (fileName == null)
            {
                if (player.World == null)
                {
                    player.Message("When used from console, /gen requires FileName.");
                    
                    return;
                }
                if (!cmd.IsConfirmed)
                {
                    player.Confirm(cmd, "Replace this world's map with a generated one?");
                    return;
                }
            }
            else
            {
                fileName = fileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                if (!fileName.EndsWith(".fcm", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".fcm";
                }
                fullFileName = Path.Combine(Paths.MapPath, fileName);
                if (!Paths.IsValidPath(fullFileName))
                {
                    player.Message("Invalid filename.");
                    return;
                }
                if (!Paths.Contains(Paths.MapPath, fullFileName))
                {
                    player.MessageUnsafePath();
                    return;
                }
                string dirName = fullFileName.Substring(0, fullFileName.LastIndexOf(Path.DirectorySeparatorChar));
                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }
                if (!cmd.IsConfirmed && File.Exists(fullFileName))
                {
                    player.Confirm(cmd, "The mapfile \"{0}\" already exists. Overwrite?", fileName);
                    return;
                }
            }

            bool noTrees;
            if (themeName.Equals("grass", StringComparison.OrdinalIgnoreCase))
            {
                theme = MapGenTheme.Forest;
                noTrees = true;
            }
            else
            {
                try
                {
                    theme = (MapGenTheme)Enum.Parse(typeof(MapGenTheme), themeName, true);
                    noTrees = (theme != MapGenTheme.Forest);
                }
                catch (Exception)
                {
                    player.MessageNow("Unrecognized theme \"{0}\". Available themes are: Grass, {1}",
                                       themeName,
                                       String.Join(", ", Enum.GetNames(typeof(MapGenTheme))));
                    return;
                }
            }

            try
            {
                template = (MapGenTemplate)Enum.Parse(typeof(MapGenTemplate), templateName, true);
            }
            catch (Exception)
            {
                player.Message("Unrecognized template \"{0}\". Available templates are: {1}",
                                templateName,
                                String.Join(", ", Enum.GetNames(typeof(MapGenTemplate))));
                return;
            }

            if (!Enum.IsDefined(typeof(MapGenTheme), theme) || !Enum.IsDefined(typeof(MapGenTemplate), template))
            {
                
                return;
            }

            MapGeneratorArgs args = MapGenerator.MakeTemplate(template);
            args.MapWidth = wx;
            args.MapLength = wy;
            args.MapHeight = height;
            args.MaxHeight = (int)(args.MaxHeight / 80d * height);
            args.MaxDepth = (int)(args.MaxDepth / 80d * height);
            args.Theme = theme;
            args.AddTrees = !noTrees;

            Map map2;
            try
            {
                if (theme == MapGenTheme.Forest && noTrees)
                {
                    player.MessageNow("Generating Grass {0}...", template);
                }
                else
                {
                    player.MessageNow("Generating {0} {1}...", theme, template);
                }
                if (theme == MapGenTheme.Forest && noTrees && template == MapGenTemplate.Flat)
                {
                    map2 = MapGenerator.GenerateFlatgrass(args.MapWidth, args.MapLength, args.MapHeight);
                }
                else
                {
                    MapGenerator generator = new MapGenerator(args);
                    map2 = generator.Generate();
                }

            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, "MapGenerator: Generation failed: {0}",
                            ex);
                player.MessageNow("&WAn error occured while generating the map.");
                return;
            }

            if (fileName != null)
            {
                if (map2.Save(fullFileName))
                {
                    player.MessageNow("Generation done. Saved to {0}", fileName);
                }
                else
                {
                    player.Message("&WAn error occured while saving generated map to {0}", fileName);
                }
            }
            else
            {
                player.MessageNow("Generation done. Changing map...");
                player.World.ChangeMap(map2);
            }
        }




        internal static void RealmAccess(Player player, Command cmd, string worldName, string name)
        {

            // Print information about the current world
            if (worldName == null)
            {
                if (player.World == null)
                {
                    player.Message("Error.");
                }
                else
                {
                    player.Message(player.World.AccessSecurity.GetDescription(player.World, "world", "accessed"));
                }
                return;
            }

            // Find a world by name
            World world = WorldManager.FindWorldOrPrintMatches(player, worldName);
            if (world == null) return;



            if (name == null)
            {
                player.Message(world.AccessSecurity.GetDescription(world, "world", "accessed"));
                return;
            }
            if (world == WorldManager.MainWorld)
            {
                player.Message("The main world cannot have access restrictions.");
                return;
            }

            bool changesWereMade = false;
            do
            {
                if (name.Length < 2) continue;
                // Whitelisting individuals
                if (name.StartsWith("+"))
                {
                    PlayerInfo info;
                    if (!PlayerDB.FindPlayerInfo(name.Substring(1), out info))
                    {
                        player.Message("More than one player found matching \"{0}\"", name.Substring(1));
                        continue;

                    }
                    else if (info == null)
                    {
                        player.MessageNoPlayer(name.Substring(1));
                        continue;
                    }

                    // prevent players from whitelisting themselves to bypass protection


                    if (world.AccessSecurity.CheckDetailed(info) == SecurityCheckResult.Allowed)
                    {
                        player.Message("{0}&S is already allowed to access {1}&S (by rank)",
                                        info.ClassyName, world.ClassyName);
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if (target == player) target = null; // to avoid duplicate messages

                    switch (world.AccessSecurity.Include(info))
                    {
                        case PermissionOverride.Deny:
                            if (world.AccessSecurity.Check(info))
                            {
                                player.Message("{0}&S is unbanned from Realm {1}",
                                                info.ClassyName, world.ClassyName);
                                if (target != null)
                                {
                                    target.Message("You are now unbanned from Realm {0}&S (removed from blacklist by {1}&S).",
                                                    world.ClassyName, player.ClassyName);
                                }
                            }
                            else
                            {
                                player.Message("{0}&S was unbanned from Realm {1}&S. " +
                                                "Player is still NOT allowed to join (by rank).",
                                                info.ClassyName, world.ClassyName);
                                if (target != null)
                                {
                                    target.Message("You were Unbanned from Realm {0}&S by {1}&S. " +
                                                    "You are still NOT allowed to join (by rank).",
                                                    player.ClassyName, world.ClassyName);
                                }
                            }
                            Logger.Log(LogType.UserActivity, "{0} removed {1} from the access blacklist of {2}",
                                        player.Name, info.Name, world.Name);
                            changesWereMade = true;
                            break;

                        case PermissionOverride.None:
                            player.Message("{0}&S is now allowed to access {1}",
                                            info.ClassyName, world.ClassyName);
                            if (target != null)
                            {
                                target.Message("You can now access world {0}&S (whitelisted by {1}&S).",
                                                world.ClassyName, player.ClassyName);
                            }
                            Logger.Log(LogType.UserActivity, "{0} added {1} to the access whitelist on world {2}",
                                        player.Name, info.Name, world.Name);
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            player.Message("{0}&S is already on the access whitelist of {1}",
                                            info.ClassyName, world.ClassyName);
                            break;
                    }

                    // Blacklisting individuals
                }
                else if (name.StartsWith("-"))
                {
                    PlayerInfo info;
                    if (!PlayerDB.FindPlayerInfo(name.Substring(1), out info))
                    {
                        player.Message("More than one player found matching \"{0}\"", name.Substring(1));
                        continue;
                    }
                    else if (info == null)
                    {
                        player.MessageNoPlayer(name.Substring(1));
                        continue;
                    }

                    if (world.AccessSecurity.CheckDetailed(info) == SecurityCheckResult.RankTooHigh ||
                        world.AccessSecurity.CheckDetailed(info) == SecurityCheckResult.RankTooLow)
                    {
                        player.Message("{0}&S is already barred from accessing {1}&S (by rank)",
                                        info.ClassyName, world.ClassyName);
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if (target == player) target = null; // to avoid duplicate messages

                    switch (world.AccessSecurity.Exclude(info))
                    {
                        case PermissionOverride.Deny:
                            player.Message("{0}&S is already banned from Realm {1}",
                                            info.ClassyName, world.ClassyName);
                            break;

                        case PermissionOverride.None:
                            player.Message("{0}&S is now banned from accessing {1}",
                                            info.ClassyName, world.ClassyName);
                            if (target != null)
                            {
                                target.Message("&WYou were banned by {0}&W from accessing world {1}",
                                                player.ClassyName, world.ClassyName);
                            }
                            Logger.Log(LogType.UserActivity, "{0} added {1} to the access blacklist on world {2}",
                                        player.Name, info.Name, world.Name);
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            if (world.AccessSecurity.Check(info))
                            {
                                player.Message("{0}&S is no longer on the access whitelist of {1}&S. " +
                                                "Player is still allowed to join (by rank).",
                                                info.ClassyName, world.ClassyName);
                                if (target != null)
                                {
                                    target.Message("You were banned from Realm {0}&S by {1}&S. " +
                                                    "You are still allowed to join (by rank).",
                                                    player.ClassyName, world.ClassyName);
                                }
                            }
                            else
                            {
                                player.Message("{0}&S is no longer allowed to access {1}",
                                                info.ClassyName, world.ClassyName);
                                if (target != null)
                                {
                                    target.Message("&WYou were banned from Realm {0}&W (Banned by {1}&W).",
                                                    world.ClassyName, player.ClassyName);
                                }
                            }
                            Logger.Log(LogType.UserActivity, "{0} removed {1} from the access whitelist on world {2}",
                                        player.Name, info.Name, world.Name);
                            changesWereMade = true;
                            break;
                    }

                    // Setting minimum rank
                }
                else
                {
                    Rank rank = RankManager.FindRank(name);
                    if (rank == null)
                    {
                        player.MessageNoRank(name);

                    }

                    else
                    {
                        // list players who are redundantly blacklisted
                        var exceptionList = world.AccessSecurity.ExceptionList;
                        PlayerInfo[] noLongerExcluded = exceptionList.Excluded.Where(excludedPlayer => excludedPlayer.Rank < rank).ToArray();
                        if (noLongerExcluded.Length > 0)
                        {
                            player.Message("Following players no longer need to be blacklisted to be barred from {0}&S: {1}",
                                            world.ClassyName,
                                            noLongerExcluded.JoinToClassyString());
                        }

                        // list players who are redundantly whitelisted
                        PlayerInfo[] noLongerIncluded = exceptionList.Included.Where(includedPlayer => includedPlayer.Rank >= rank).ToArray();
                        if (noLongerIncluded.Length > 0)
                        {
                            player.Message("Following players no longer need to be whitelisted to access {0}&S: {1}",
                                            world.ClassyName,
                                            noLongerIncluded.JoinToClassyString());
                        }

                        // apply changes
                        world.AccessSecurity.MinRank = rank;
                        changesWereMade = true;
                        if (world.AccessSecurity.MinRank == RankManager.LowestRank)
                        {
                            Server.Message("{0}&S made the world {1}&S accessible to everyone.",
                                              player.ClassyName, world.ClassyName);
                        }
                        else
                        {
                            Server.Message("{0}&S made the world {1}&S accessible only by {2}+",
                                              player.ClassyName, world.ClassyName,
                                              world.AccessSecurity.MinRank.ClassyName);
                        }
                        Logger.Log(LogType.UserActivity, "{0} set access rank for world {1} to {2}+",
                                    player.Name, world.Name, world.AccessSecurity.MinRank.Name);
                    }
                }
            } while ((name = cmd.Next()) != null);

            if (changesWereMade)
            {
                var playersWhoCantStay = world.Players.Where(p => !p.CanJoin(world));
                foreach (Player p in playersWhoCantStay)
                {
                    p.Message("&WYou are no longer allowed to join world {0}", world.ClassyName);
                    p.JoinWorld(WorldManager.MainWorld, WorldChangeReason.PermissionChanged);
                }
                
                WorldManager.SaveWorldList();
            }
        }


        internal static void RealmBuild(Player player, Command cmd, string worldName, string name, string NameIfRankIsName)
        {


            // Print information about the current world
            if (worldName == null)
            {
                if (player.World == null)
                {
                    player.Message("When calling /wbuild from console, you must specify a world name.");
                }
                else
                {
                    player.Message(player.World.BuildSecurity.GetDescription(player.World, "world", "modified"));
                }
                return;
            }

            // Find a world by name
            World world = WorldManager.FindWorldOrPrintMatches(player, worldName);
            if (world == null) return;


            if (name == null)
            {
                player.Message(world.BuildSecurity.GetDescription(world, "world", "modified"));
                return;
            }

            bool changesWereMade = false;
            do
            {
                if (name.Length < 2) continue;
                // Whitelisting individuals
                if (name.StartsWith("+"))
                {
                    PlayerInfo info;
                    if (!PlayerDB.FindPlayerInfo(name.Substring(1), out info))
                    {
                        player.Message("More than one player found matching \"{0}\"", name.Substring(1));
                        continue;
                    }
                    else if (info == null)
                    {
                        player.MessageNoPlayer(name.Substring(1));
                        continue;
                    }



                    if (world.BuildSecurity.CheckDetailed(info) == SecurityCheckResult.Allowed)
                    {
                        player.Message("{0}&S is already allowed to build in {1}&S (by rank)",
                                        info.ClassyName, world.ClassyName);
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if (target == player) target = null; // to avoid duplicate messages

                    switch (world.BuildSecurity.Include(info))
                    {
                        case PermissionOverride.Deny:
                            if (world.BuildSecurity.Check(info))
                            {
                                player.Message("{0}&S is no longer barred from building in {1}",
                                                info.ClassyName, world.ClassyName);
                                if (target != null)
                                {
                                    target.Message("You can now build in world {0}&S (removed from blacklist by {1}&S).",
                                                    world.ClassyName, player.ClassyName);
                                }
                            }
                            else
                            {
                                player.Message("{0}&S was removed from the build blacklist of {1}&S. " +
                                                "Player is still NOT allowed to build (by rank).",
                                                info.ClassyName, world.ClassyName);
                                if (target != null)
                                {
                                    target.Message("You were removed from the build blacklist of world {0}&S by {1}&S. " +
                                                    "You are still NOT allowed to build (by rank).",
                                                    player.ClassyName, world.ClassyName);
                                }
                            }
                            Logger.Log(LogType.UserActivity, "{0} removed {1} from the build blacklist of {2}",
                                        player.Name, info.Name, world.Name);
                            changesWereMade = true;
                            break;

                        case PermissionOverride.None:
                            player.Message("{0}&S is now allowed to build in {1}",
                                            info.ClassyName, world.ClassyName);
                            if (target != null)
                            {
                                target.Message("You can now build in world {0}&S (whitelisted by {1}&S).",
                                                world.ClassyName, player.ClassyName);
                            }
                            Logger.Log(LogType.UserActivity, "{0} added {1} to the build whitelist on world {2}",
                                        player.Name, info.Name, world.Name);
                            break;

                        case PermissionOverride.Allow:
                            player.Message("{0}&S is already on the build whitelist of {1}",
                                            info.ClassyName, world.ClassyName);
                            break;
                    }

                    // Blacklisting individuals
                }
                else if (name.StartsWith("-"))
                {
                    PlayerInfo info;
                    if (!PlayerDB.FindPlayerInfo(name.Substring(1), out info))
                    {
                        player.Message("More than one player found matching \"{0}\"", name.Substring(1));
                        continue;
                    }
                    else if (info == null)
                    {
                        player.MessageNoPlayer(name.Substring(1));
                        continue;
                    }

                    if (world.BuildSecurity.CheckDetailed(info) == SecurityCheckResult.RankTooHigh ||
                        world.BuildSecurity.CheckDetailed(info) == SecurityCheckResult.RankTooLow)
                    {
                        player.Message("{0}&S is already barred from building in {1}&S (by rank)",
                                        info.ClassyName, world.ClassyName);
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if (target == player) target = null; // to avoid duplicate messages

                    switch (world.BuildSecurity.Exclude(info))
                    {
                        case PermissionOverride.Deny:
                            player.Message("{0}&S is already on build blacklist of {1}",
                                            info.ClassyName, world.ClassyName);
                            break;

                        case PermissionOverride.None:
                            player.Message("{0}&S is now barred from building in {1}",
                                            info.ClassyName, world.ClassyName);
                            if (target != null)
                            {
                                target.Message("&WYou were barred by {0}&W from building in world {1}",
                                                player.ClassyName, world.ClassyName);
                            }
                            Logger.Log(LogType.UserActivity, "{0} added {1} to the build blacklist on world {2}",
                                        player.Name, info.Name, world.Name);
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            if (world.BuildSecurity.Check(info))
                            {
                                player.Message("{0}&S is no longer on the build whitelist of {1}&S. " +
                                                "Player is still allowed to build (by rank).",
                                                info.ClassyName, world.ClassyName);
                                if (target != null)
                                {
                                    target.Message("You were removed from the build whitelist of world {0}&S by {1}&S. " +
                                                    "You are still allowed to build (by rank).",
                                                    player.ClassyName, world.ClassyName);
                                }
                            }
                            else
                            {
                                player.Message("{0}&S is no longer allowed to build in {1}",
                                                info.ClassyName, world.ClassyName);
                                if (target != null)
                                {
                                    target.Message("&WYou can no longer build in world {0}&W (removed from whitelist by {1}&W).",
                                                    world.ClassyName, player.ClassyName);
                                }
                            }
                            Logger.Log(LogType.UserActivity, "{0} removed {1} from the build whitelist on world {2}",
                                        player.Name, info.Name, world.Name);
                            changesWereMade = true;
                            break;
                    }

                    // Setting minimum rank
                }
                else
                {
                    Rank rank = RankManager.FindRank(name);
                    if (rank == null)
                    {
                        player.MessageNoRank(name);
                    }
                    else if (!player.Info.Rank.AllowSecurityCircumvention &&
                             world.BuildSecurity.MinRank > rank &&
                             world.BuildSecurity.MinRank > player.Info.Rank)
                    {
                        player.Message("&WYou must be ranked {0}&W+ to lower build restrictions for world {1}",
                                        world.BuildSecurity.MinRank.ClassyName, world.ClassyName);
                    }
                    else
                    {
                        // list players who are redundantly blacklisted
                        var exceptionList = world.BuildSecurity.ExceptionList;
                        PlayerInfo[] noLongerExcluded = exceptionList.Excluded.Where(excludedPlayer => excludedPlayer.Rank < rank).ToArray();
                        if (noLongerExcluded.Length > 0)
                        {
                            player.Message("Following players no longer need to be blacklisted on world {0}&S: {1}",
                                            world.ClassyName,
                                            noLongerExcluded.JoinToClassyString());
                        }

                        // list players who are redundantly whitelisted
                        PlayerInfo[] noLongerIncluded = exceptionList.Included.Where(includedPlayer => includedPlayer.Rank >= rank).ToArray();
                        if (noLongerIncluded.Length > 0)
                        {
                            player.Message("Following players no longer need to be whitelisted on world {0}&S: {1}",
                                            world.ClassyName,
                                            noLongerIncluded.JoinToClassyString());
                        }

                        // apply changes
                        world.BuildSecurity.MinRank = rank;
                        changesWereMade = true;
                        if (world.BuildSecurity.MinRank == RankManager.LowestRank)
                        {
                            Server.Message("{0}&S allowed anyone to build on world {1}",
                                              player.ClassyName, world.ClassyName);
                        }
                        else
                        {
                            Server.Message("{0}&S allowed only {1}+&S to build in world {2}",
                                              player.ClassyName, world.BuildSecurity.MinRank.ClassyName, world.ClassyName);
                        }
                        Logger.Log(LogType.UserActivity, "{0} set build rank for world {1} to {2}+",
                                    player.Name, world.Name, world.BuildSecurity.MinRank.Name);
                    }
                }
            } while ((name = cmd.Next()) != null);

            if (changesWereMade)
            {
                WorldManager.SaveWorldList();
            }
        }

    }
}