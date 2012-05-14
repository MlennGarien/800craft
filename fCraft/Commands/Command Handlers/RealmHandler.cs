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

//Copyright (C) <2012> Jon Baker (http://au70.net)

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using fCraft.MapConversion;
using JetBrains.Annotations;
using fCraft.Drawing;
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
                player.Message("When using /realm from console, you must specify the realm name.");
                return;
            }

            if (fileName == null)
            {
                // No params given at all
                
                return;
            }

            string fullFileName = WorldManager.FindMapFile(player, fileName);
            if (fullFileName == null) return;

            // Loading map into current realm
            if (worldName == null)
            {
                if (!cmd.IsConfirmed)
                {
                    player.Confirm(cmd, "About to replace THIS REALM with \"{0}\".", fileName);
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
                World realm = player.World;

                // Loading to current realm
                realm.MapChangedBy = player.Name;
                realm.ChangeMap(map);

                realm.Players.Message(player, "{0}&S loaded a new map for this realm.",
                                              player.ClassyName);
                player.MessageNow("New map loaded for the realm {0}", realm.ClassyName);

                Logger.Log(LogType.UserActivity,
                            "{0} loaded new map for realm \"{1}\" from {2}",
                            player.Name, realm.Name, fileName);
                realm.IsHidden = false;
                realm.IsRealm = true;
                WorldManager.SaveWorldList();


            }
            else
            {
                // Loading to some other (or new) realm
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

                // Retype realm name, if needed
                if (worldName == "-")
                {
                    if (player.LastUsedWorldName != null)
                    {
                        worldName = player.LastUsedWorldName;
                    }
                    else
                    {
                        player.Message("Cannot repeat realm name: you haven't used any names yet.");
                        return;
                    }
                }

                lock (WorldManager.SyncRoot)
                {
                    World realm = WorldManager.FindWorldExact(worldName);
                    if (realm != null)
                    {
                        player.LastUsedWorldName = realm.Name;
                        // Replacing existing realm's map
                        if (!cmd.IsConfirmed)
                        {
                            player.Confirm(cmd, "About to replace realm map for {0}&S with \"{1}\".",
                                            realm.ClassyName, fileName);
                            return;
                        }

                        Map map;
                        try
                        {
                            map = MapUtility.Load(fullFileName);
                            realm.IsHidden = false;
                            realm.IsRealm = true;
                            WorldManager.SaveWorldList();
                        }
                        catch (Exception ex)
                        {
                            player.MessageNow("Could not load specified file: {0}: {1}", ex.GetType().Name, ex.Message);
                            return;
                        }

                        try
                        {
                            realm.MapChangedBy = player.Name;
                            realm.ChangeMap(map);
                            realm.IsHidden = false;
                            realm.IsRealm = true;
                            WorldManager.SaveWorldList();
                        }
                        catch (WorldOpException ex)
                        {
                            Logger.Log(LogType.Error,
                                        "Could not complete RealmLoad operation: {0}", ex.Message);
                            player.Message("&WRealmLoad: {0}", ex.Message);
                            return;
                        }

                        realm.Players.Message(player, "{0}&S loaded a new map for the realm {1}",
                                               player.ClassyName, realm.ClassyName);
                        player.MessageNow("New map for the realm {0}&S has been loaded.", realm.ClassyName);
                        Logger.Log(LogType.UserActivity,
                                    "{0} loaded new map for realm \"{1}\" from {2}",
                                    player.Name, realm.Name, fullFileName);
                        

                    }
                    else
                    {
                        // Adding a new realm
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
                            //realm.IsHidden = false;
                            //realm.IsRealm = true;
                            //WorldManager.SaveWorldList();
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
                            
                        }
                        catch (WorldOpException ex)
                        {
                            player.Message("RealmLoad: {0}", ex.Message);
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
                            player.Message("BlockDB is now auto-enabled on realm {0}", newWorld.ClassyName);
                        }
                        newWorld.LoadedBy = player.Name;
                        newWorld.LoadedOn = DateTime.UtcNow;
                        Server.Message("{0}&S created a new realm named {1}",
                                        player.ClassyName, newWorld.ClassyName);
                        Logger.Log(LogType.UserActivity,
                                    "{0} created a new realm named \"{1}\" (loaded from \"{2}\")",
                                    player.Name, worldName, fileName);
                        newWorld.IsHidden = false;
                        newWorld.IsRealm = true;
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
                player.Message("Cannot make map with height {0}: dimensions must be multiples of 16.", height);
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
                    player.Confirm(cmd, "Replace this realm's map with a generated one?");
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

            Map map;
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
                    map = MapGenerator.GenerateFlatgrass(args.MapWidth, args.MapLength, args.MapHeight);
                }
                else
                {
                    MapGenerator generator = new MapGenerator(args);
                    map = generator.Generate();
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
                if (map.Save(fullFileName))
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
                player.World.ChangeMap(map);
            }
        }




        internal static void RealmAccess(Player player, Command cmd, string worldName, string name)
        {

            // Print information about the current realm
            if (worldName == null)
            {
                if (player.World == null)
                {
                    player.Message("Error.");
                }
                else
                {
                    player.Message(player.World.AccessSecurity.GetDescription(player.World, "realm", "accessed"));
                }
                return;
            }

            // Find a realm by name
            World realm = WorldManager.FindWorldOrPrintMatches(player, worldName);
            if (realm == null) return;



            if (name == null)
            {
                player.Message(realm.AccessSecurity.GetDescription(realm, "realm", "accessed"));
                return;
            }
            if (realm == WorldManager.MainWorld)
            {
                player.Message("The main realm cannot have access restrictions.");
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


                    if (realm.AccessSecurity.CheckDetailed(info) == SecurityCheckResult.Allowed)
                    {
                        player.Message("{0}&S is already allowed to access {1}&S (by rank)",
                                        info.ClassyName, realm.ClassyName);
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if (target == player) target = null; // to avoid duplicate messages

                    switch (realm.AccessSecurity.Include(info))
                    {
                        case PermissionOverride.Deny:
                            if (realm.AccessSecurity.Check(info))
                            {
                                player.Message("{0}&S is unbanned from Realm {1}",
                                                info.ClassyName, realm.ClassyName);
                                if (target != null)
                                {
                                    target.Message("You are now unbanned from Realm {0}&S (removed from blacklist by {1}&S).",
                                                    realm.ClassyName, player.ClassyName);
                                }
                            }
                            else
                            {
                                player.Message("{0}&S was unbanned from Realm {1}&S. " +
                                                "Player is still NOT allowed to join (by rank).",
                                                info.ClassyName, realm.ClassyName);
                                if (target != null)
                                {
                                    target.Message("You were Unbanned from Realm {0}&S by {1}&S. " +
                                                    "You are still NOT allowed to join (by rank).",
                                                    player.ClassyName, realm.ClassyName);
                                }
                            }
                            Logger.Log(LogType.UserActivity, "{0} removed {1} from the access blacklist of {2}",
                                        player.Name, info.Name, realm.Name);
                            changesWereMade = true;
                            break;

                        case PermissionOverride.None:
                            player.Message("{0}&S is now allowed to access {1}",
                                            info.ClassyName, realm.ClassyName);
                            if (target != null)
                            {
                                target.Message("You can now access realm {0}&S (whitelisted by {1}&S).",
                                                realm.ClassyName, player.ClassyName);
                            }
                            Logger.Log(LogType.UserActivity, "{0} added {1} to the access whitelist on realm {2}",
                                        player.Name, info.Name, realm.Name);
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            player.Message("{0}&S is already on the access whitelist of {1}",
                                            info.ClassyName, realm.ClassyName);
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

                    if (realm.AccessSecurity.CheckDetailed(info) == SecurityCheckResult.RankTooHigh ||
                        realm.AccessSecurity.CheckDetailed(info) == SecurityCheckResult.RankTooLow)
                    {
                        player.Message("{0}&S is already barred from accessing {1}&S (by rank)",
                                        info.ClassyName, realm.ClassyName);
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if (target == player) target = null; // to avoid duplicate messages

                    switch (realm.AccessSecurity.Exclude(info))
                    {
                        case PermissionOverride.Deny:
                            player.Message("{0}&S is already banned from Realm {1}",
                                            info.ClassyName, realm.ClassyName);
                            break;

                        case PermissionOverride.None:
                            player.Message("{0}&S is now banned from accessing {1}",
                                            info.ClassyName, realm.ClassyName);
                            if (target != null)
                            {
                                target.Message("&WYou were banned by {0}&W from accessing realm {1}",
                                                player.ClassyName, realm.ClassyName);
                            }
                            Logger.Log(LogType.UserActivity, "{0} added {1} to the access blacklist on realm {2}",
                                        player.Name, info.Name, realm.Name);
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            if (realm.AccessSecurity.Check(info))
                            {
                                player.Message("{0}&S is no longer on the access whitelist of {1}&S. " +
                                                "Player is still allowed to join (by rank).",
                                                info.ClassyName, realm.ClassyName);
                                if (target != null)
                                {
                                    target.Message("You were banned from Realm {0}&S by {1}&S. " +
                                                    "You are still allowed to join (by rank).",
                                                    player.ClassyName, realm.ClassyName);
                                }
                            }
                            else
                            {
                                player.Message("{0}&S is no longer allowed to access {1}",
                                                info.ClassyName, realm.ClassyName);
                                if (target != null)
                                {
                                    target.Message("&WYou were banned from Realm {0}&W (Banned by {1}&W).",
                                                    realm.ClassyName, player.ClassyName);
                                }
                            }
                            Logger.Log(LogType.UserActivity, "{0} removed {1} from the access whitelist on realm {2}",
                                        player.Name, info.Name, realm.Name);
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
                        var exceptionList = realm.AccessSecurity.ExceptionList;
                        PlayerInfo[] noLongerExcluded = exceptionList.Excluded.Where(excludedPlayer => excludedPlayer.Rank < rank).ToArray();
                        if (noLongerExcluded.Length > 0)
                        {
                            player.Message("Following players no longer need to be blacklisted to be barred from {0}&S: {1}",
                                            realm.ClassyName,
                                            noLongerExcluded.JoinToClassyString());
                        }

                        // list players who are redundantly whitelisted
                        PlayerInfo[] noLongerIncluded = exceptionList.Included.Where(includedPlayer => includedPlayer.Rank >= rank).ToArray();
                        if (noLongerIncluded.Length > 0)
                        {
                            player.Message("Following players no longer need to be whitelisted to access {0}&S: {1}",
                                            realm.ClassyName,
                                            noLongerIncluded.JoinToClassyString());
                        }

                        // apply changes
                        realm.AccessSecurity.MinRank = rank;
                        changesWereMade = true;
                        if (realm.AccessSecurity.MinRank == RankManager.LowestRank)
                        {
                            Server.Message("{0}&S made the realm {1}&S accessible to everyone.",
                                              player.ClassyName, realm.ClassyName);
                        }
                        else
                        {
                            Server.Message("{0}&S made the realm {1}&S accessible only by {2}+",
                                              player.ClassyName, realm.ClassyName,
                                              realm.AccessSecurity.MinRank.ClassyName);
                        }
                        Logger.Log(LogType.UserActivity, "{0} set access rank for realm {1} to {2}+",
                                    player.Name, realm.Name, realm.AccessSecurity.MinRank.Name);
                    }
                }
            } while ((name = cmd.Next()) != null);

            if (changesWereMade)
            {
                var playersWhoCantStay = realm.Players.Where(p => !p.CanJoin(realm));
                foreach (Player p in playersWhoCantStay)
                {
                    p.Message("&WYou are no longer allowed to join realm {0}", realm.ClassyName);
                    p.JoinWorld(WorldManager.MainWorld, WorldChangeReason.PermissionChanged);
                }

                WorldManager.SaveWorldList();
            }
        }


        internal static void RealmBuild(Player player, Command cmd, string worldName, string name, string NameIfRankIsName)
        {


            // Print information about the current realm
            if (worldName == null)
            {
                if (player.World == null)
                {
                    player.Message("When calling /wbuild from console, you must specify a realm name.");
                }
                else
                {
                    player.Message(player.World.BuildSecurity.GetDescription(player.World, "realm", "modified"));
                }
                return;
            }

            // Find a realm by name
            World realm = WorldManager.FindWorldOrPrintMatches(player, worldName);
            if (realm == null) return;


            if (name == null)
            {
                player.Message(realm.BuildSecurity.GetDescription(realm, "realm", "modified"));
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



                    if (realm.BuildSecurity.CheckDetailed(info) == SecurityCheckResult.Allowed)
                    {
                        player.Message("{0}&S is already allowed to build in {1}&S (by rank)",
                                        info.ClassyName, realm.ClassyName);
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if (target == player) target = null; // to avoid duplicate messages

                    switch (realm.BuildSecurity.Include(info))
                    {
                        case PermissionOverride.Deny:
                            if (realm.BuildSecurity.Check(info))
                            {
                                player.Message("{0}&S is no longer barred from building in {1}",
                                                info.ClassyName, realm.ClassyName);
                                if (target != null)
                                {
                                    target.Message("You can now build in realm {0}&S (removed from blacklist by {1}&S).",
                                                    realm.ClassyName, player.ClassyName);
                                }
                            }
                            else
                            {
                                player.Message("{0}&S was removed from the build blacklist of {1}&S. " +
                                                "Player is still NOT allowed to build (by rank).",
                                                info.ClassyName, realm.ClassyName);
                                if (target != null)
                                {
                                    target.Message("You were removed from the build blacklist of realm {0}&S by {1}&S. " +
                                                    "You are still NOT allowed to build (by rank).",
                                                    player.ClassyName, realm.ClassyName);
                                }
                            }
                            Logger.Log(LogType.UserActivity, "{0} removed {1} from the build blacklist of {2}",
                                        player.Name, info.Name, realm.Name);
                            changesWereMade = true;
                            break;

                        case PermissionOverride.None:
                            player.Message("{0}&S is now allowed to build in {1}",
                                            info.ClassyName, realm.ClassyName);
                            if (target != null)
                            {
                                target.Message("You can now build in realm {0}&S (whitelisted by {1}&S).",
                                                realm.ClassyName, player.ClassyName);
                            }
                            Logger.Log(LogType.UserActivity, "{0} added {1} to the build whitelist on realm {2}",
                                        player.Name, info.Name, realm.Name);
                            break;

                        case PermissionOverride.Allow:
                            player.Message("{0}&S is already on the build whitelist of {1}",
                                            info.ClassyName, realm.ClassyName);
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

                    if (realm.BuildSecurity.CheckDetailed(info) == SecurityCheckResult.RankTooHigh ||
                        realm.BuildSecurity.CheckDetailed(info) == SecurityCheckResult.RankTooLow)
                    {
                        player.Message("{0}&S is already barred from building in {1}&S (by rank)",
                                        info.ClassyName, realm.ClassyName);
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if (target == player) target = null; // to avoid duplicate messages

                    switch (realm.BuildSecurity.Exclude(info))
                    {
                        case PermissionOverride.Deny:
                            player.Message("{0}&S is already on build blacklist of {1}",
                                            info.ClassyName, realm.ClassyName);
                            break;

                        case PermissionOverride.None:
                            player.Message("{0}&S is now barred from building in {1}",
                                            info.ClassyName, realm.ClassyName);
                            if (target != null)
                            {
                                target.Message("&WYou were barred by {0}&W from building in realm {1}",
                                                player.ClassyName, realm.ClassyName);
                            }
                            Logger.Log(LogType.UserActivity, "{0} added {1} to the build blacklist on realm {2}",
                                        player.Name, info.Name, realm.Name);
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            if (realm.BuildSecurity.Check(info))
                            {
                                player.Message("{0}&S is no longer on the build whitelist of {1}&S. " +
                                                "Player is still allowed to build (by rank).",
                                                info.ClassyName, realm.ClassyName);
                                if (target != null)
                                {
                                    target.Message("You were removed from the build whitelist of realm {0}&S by {1}&S. " +
                                                    "You are still allowed to build (by rank).",
                                                    player.ClassyName, realm.ClassyName);
                                }
                            }
                            else
                            {
                                player.Message("{0}&S is no longer allowed to build in {1}",
                                                info.ClassyName, realm.ClassyName);
                                if (target != null)
                                {
                                    target.Message("&WYou can no longer build in realm {0}&W (removed from whitelist by {1}&W).",
                                                    realm.ClassyName, player.ClassyName);
                                }
                            }
                            Logger.Log(LogType.UserActivity, "{0} removed {1} from the build whitelist on realm {2}",
                                        player.Name, info.Name, realm.Name);
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
                             realm.BuildSecurity.MinRank > rank &&
                             realm.BuildSecurity.MinRank > player.Info.Rank)
                    {
                        player.Message("&WYou must be ranked {0}&W+ to lower build restrictions for realm {1}",
                                        realm.BuildSecurity.MinRank.ClassyName, realm.ClassyName);
                    }
                    else
                    {
                        // list players who are redundantly blacklisted
                        var exceptionList = realm.BuildSecurity.ExceptionList;
                        PlayerInfo[] noLongerExcluded = exceptionList.Excluded.Where(excludedPlayer => excludedPlayer.Rank < rank).ToArray();
                        if (noLongerExcluded.Length > 0)
                        {
                            player.Message("Following players no longer need to be blacklisted on realm {0}&S: {1}",
                                            realm.ClassyName,
                                            noLongerExcluded.JoinToClassyString());
                        }

                        // list players who are redundantly whitelisted
                        PlayerInfo[] noLongerIncluded = exceptionList.Included.Where(includedPlayer => includedPlayer.Rank >= rank).ToArray();
                        if (noLongerIncluded.Length > 0)
                        {
                            player.Message("Following players no longer need to be whitelisted on realm {0}&S: {1}",
                                            realm.ClassyName,
                                            noLongerIncluded.JoinToClassyString());
                        }

                        // apply changes
                        realm.BuildSecurity.MinRank = rank;
                        changesWereMade = true;
                        if (realm.BuildSecurity.MinRank == RankManager.LowestRank)
                        {
                            Server.Message("{0}&S allowed anyone to build on realm {1}",
                                              player.ClassyName, realm.ClassyName);
                        }
                        else
                        {
                            Server.Message("{0}&S allowed only {1}+&S to build in realm {2}",
                                              player.ClassyName, realm.BuildSecurity.MinRank.ClassyName, realm.ClassyName);
                        }
                        Logger.Log(LogType.UserActivity, "{0} set build rank for realm {1} to {2}+",
                                    player.Name, realm.Name, realm.BuildSecurity.MinRank.Name);
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