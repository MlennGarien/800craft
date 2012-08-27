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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace fCraft
{
    public class VoteHandler
    {
        public static string Usage;
        public static int VotedYes;
        public static int VotedNo;
        public static List<Player> Voted;
        public static string VoteStarter;
        public static bool VoteIsOn = false;
        public static Thread VoteThread;
        public static string VoteKickReason;
        public static string TargetName;
        public static string Question;

        public static void NewVote()
        {
            Usage = "&A/Vote Yes | No | Ask | Kick | Abort";
            VotedYes = 0;
            VotedNo = 0;
            Voted = new List<Player>();
            VoteIsOn = true;
        }

        public static void VoteParams(Player player, Command cmd)
        {
            string option = cmd.Next();
            if (option == null) { player.Message("Invalid param"); return; }
            switch (option)
            {
                default:
                    if (VoteIsOn)
                    {
                        if (VoteKickReason == null)
                        {
                            player.Message("Last Question: {0}&C asked: {1}", VoteStarter, Question);
                            player.Message(Usage);
                            return;
                        }
                        else
                            player.Message("Last VoteKick: &CA VoteKick has started for {0}&C, reason: {1}", TargetName, VoteKickReason);
                        player.Message(Usage);
                        return;
                    }
                    else
                        player.Message(option);
                    break;

                case "abort":
                case "stop":
                    if (!VoteIsOn)
                    {
                        player.Message("No vote is currently running");
                        return;
                    }

                    if (!player.Can(Permission.MakeVotes))
                    {
                        player.Message("You do not have Permission to abort votes");
                        return;
                    }
                    VoteIsOn = false;
                    foreach (Player V in Voted)
                    {
                        if (V.Info.HasVoted)
                            V.Info.HasVoted = false;
                        V.Message("Your vote was cancelled");
                    }
                    Voted.Clear();
                    TargetName = null;
                    Server.Players.Message("{0} &Saborted the vote.", player.ClassyName);
                    break;

                case "yes":
                    if (!VoteIsOn)
                    {
                        player.Message("No vote is currently running");
                        return;
                    }

                    if (player.Info.HasVoted)
                    {
                        player.Message("&CYou have already voted");
                        return;
                    }
                    Voted.Add(player);
                    VotedYes++;
                    player.Info.HasVoted = true;
                    player.Message("&8You have voted for 'Yes'");
                    break;

                case "kick":
                    string toKick = cmd.Next();
                    string Reason = cmd.NextAll();
                    VoteKickReason = Reason;
                    if (toKick == null)
                    {
                        player.Message("Target cannot be empty. " + Usage);
                        return;
                    }

                    Player target = Server.FindPlayerOrPrintMatches(player, toKick, false, true);

                    if (target == null)
                    {
                        // FIX: Target is null when no such player is online, this caused crashes
                        return;
                    }

                    if (!Player.IsValidName(target.Name))
                    {
                        return;
                    }

                    if (!player.Can(Permission.MakeVoteKicks))
                    {
                        player.Message("You do not have permissions to start a VoteKick");
                        return;
                    }

                    if (VoteIsOn)
                    {
                        player.Message("A vote has already started. Each vote lasts 1 minute.");
                        return;
                    }

                    if (VoteKickReason.Length < 3)
                    {
                        player.Message("Invalid reason");
                        return;
                    }

                    if (target == player)
                    {
                        player.Message("You cannot VoteKick yourself, lol");
                        return;
                    }

                    VoteThread = new Thread(new ThreadStart(delegate
                      {
                          TargetName = target.Name;
                          if (!Player.IsValidName(TargetName))
                          {
                              player.Message("Invalid name");
                              return;
                          }
                          NewVote();
                          VoteStarter = player.ClassyName;
                          Server.Players.Message("{0}&S started a VoteKick for player: {1}", player.ClassyName, target.ClassyName);
                          Server.Players.Message("&WReason: {0}", VoteKickReason);
                          Server.Players.Message("&9Vote now! &S/Vote &AYes &Sor /Vote &CNo");
                          VoteIsOn = true;
                          Logger.Log(LogType.SystemActivity, "{0} started a votekick on player {1} reason: {2}", player.Name, target.Name, VoteKickReason);
                          Thread.Sleep(60000);
                          VoteKickCheck();
                      })); VoteThread.Start();
                    break;

                case "no":
                    if (!VoteIsOn)
                    {
                        player.Message("No vote is currently running");
                        return;
                    }
                    if (player.Info.HasVoted)
                    {
                        player.Message("&CYou have already voted");
                        return;
                    }
                    VotedNo++;
                    Voted.Add(player);
                    player.Info.HasVoted = true;
                    player.Message("&8You have voted for 'No'");
                    break;

                case "ask":
                    string AskQuestion = cmd.NextAll();
                    Question = AskQuestion;
                    if (!player.Can(Permission.MakeVotes))
                    {
                        player.Message("You do not have permissions to ask a question");
                        return;
                    }
                    if (VoteIsOn)
                    {
                        player.Message("A vote has already started. Each vote lasts 1 minute.");
                        return;
                    }
                    if (Question.Length < 5)
                    {
                        player.Message("Invalid question");
                        return;
                    }

                    VoteThread = new Thread(new ThreadStart(delegate
                      {
                          NewVote();
                          VoteStarter = player.ClassyName;
                          Server.Players.Message("{0}&S Asked: {1}", player.ClassyName, Question);
                          Server.Players.Message("&9Vote now! &S/Vote &AYes &Sor /Vote &CNo");
                          VoteIsOn = true;
                          Thread.Sleep(60000);
                          VoteCheck();
                      })); VoteThread.Start();
                    break;
            }
        }

        static void VoteCheck()
        {
            if (VoteIsOn)
            {
                Server.Players.Message("{0}&S Asked: {1} \n&SResults are in! Yes: &A{2} &SNo: &C{3}", VoteStarter,
                                       Question, VotedYes, VotedNo);
                VoteIsOn = false;
                foreach (Player V in Voted)
                {
                    V.Info.HasVoted = false;
                }
            }
        }

        static void VoteKickCheck()
        {
            if (VoteIsOn)
            {
                Server.Players.Message("{0}&S wanted to get {1} kicked. Reason: {2} \n&SResults are in! Yes: &A{3} &SNo: &C{4}", VoteStarter,
                                      TargetName, VoteKickReason, VotedYes, VotedNo);

                Player target = null;

                try
                {
                    target = Server.FindPlayerOrPrintMatches(Player.Console, TargetName, true, true);
                }
                catch (Exception ex)
                {
                    // Log exception
                    Logger.Log(LogType.Error, ex.Message);

                    // Notify users
                    Server.Message("Unexpected error while trying to execute votekick");

                    // Unable to find target so quit.
                    VoteIsOn = false;
                    TargetName = null;
                    return;
                }

                if (target == null)
                {
                    Server.Message("{0}&S is offline", target.ClassyName);
                    return;
                }
                else if (VotedYes > VotedNo)
                {
                    Scheduler.NewTask(t => target.Kick(Player.Console, "VoteKick by: " + VoteStarter + " - " + VoteKickReason, LeaveReason.Kick, false, true, false)).RunOnce(TimeSpan.FromSeconds(3));
                    Server.Players.Message("{0}&S was kicked from the server", target.ClassyName);
                }
                else
                    Server.Players.Message("{0} &Sdid not get kicked from the server", target.ClassyName);
                VoteIsOn = false;
                TargetName = null;
                foreach (Player V in Voted)
                {
                    V.Info.HasVoted = false;
                }
            }
        }
    }
}
