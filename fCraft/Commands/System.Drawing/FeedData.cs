
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using fCraft;
using System.Collections.Concurrent;
using System.Drawing;
//Copyright (C) <2012> <Jon Baker, Glenn Mariën and Lao Tszy>

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
namespace fCraft
{
    public sealed class FeedData
    {
        public bool started = false;
        public Vector3I StartPos; //the start position
        public Vector3I EndPos; //the moving position
        public Vector3I FinishPos; //the end position, start -> end
        public Vector3I Pos; //the position of the blockupdate
        public List<byte> last = new List<byte>();
        public List<byte> originalMap = new List<byte>();
        public byte textType; //font color
        public byte bgType; //background color

        public int SPACE_UNIT = 4; //width of space character in blocks
        public List<bool>[] chars = new List<bool>[126 - 32];
        public World world;
        public string Sentence;
        public int MessageCount;
        public ConcurrentDictionary<string, Vector3I> Blocks; //the blocks currently on the feed
        public readonly int Id;
        public static List<String> Messages;
        public Direction direction;
        public SchedulerTask task;

        public FeedData(Block _textType, Vector3I _pos, string Image, World world, Direction direction_)
        {
            direction = direction_;
            Blocks = new ConcurrentDictionary<string, Vector3I>();
            Init(Image, world);
            Pos = _pos;
            textType = (byte)_textType;
            bgType = (byte)Block.Air;
            FeedData.AddMessages();
            MessageCount = 0;
            Sentence = FeedData.Messages[MessageCount];
            Id = System.Threading.Interlocked.Increment(ref feedCounter);
            AddFeedToList(this);
            Start();
        }

        public void Start()
        {
            started = true;
            task = Scheduler.NewTask(StartFeed);
            task.RunForever(TimeSpan.FromMilliseconds(600));
        }

        public static int feedCounter;
        static readonly object FeedListLock = new object();
        static readonly Dictionary<int, FeedData> Feeds = new Dictionary<int, FeedData>();

        static void AddFeedToList([NotNull] FeedData data)
        {
            if (data == null) throw new ArgumentNullException("Feed");
            lock (FeedListLock)
            {
                Feeds.Add(data.Id, data);
            }
        }


        public static void RemoveFeedFromList([NotNull] FeedData data)
        {
            if (data == null) throw new ArgumentNullException("feed");
            lock (FeedListLock)
            {
                Feeds.Remove(data.Id);
            }
        }


        public static FeedData[] FeedList
        {
            get
            {
                lock (FeedListLock)
                {
                    return Feeds.Values.ToArray();
                }
            }
        }

        [CanBeNull]
        public static FeedData FindFeedById(int id)
        {
            lock (FeedListLock)
            {
                FeedData result;
                if (Feeds.TryGetValue(id, out result))
                {
                    return result;
                }
                else
                {
                    return null;
                }
            }
        }
        static Permission[] per = new Permission[] { Permission.Promote, Permission.Demote, Permission.ReadStaffChat };
        public static void AddMessages()
        {
            if (Messages != null)
            {
                Messages.Clear();
            }
            Messages = new List<string>();
            Messages.Add("Server Time: " + DateTime.Now.ToString("HH:mm:ss tt"));
            Messages.Add("Welcome to " + ConfigKey.ServerName.GetString());
            Messages.Add(Server.Players.Where(p => !p.Info.IsHidden).Count().ToString() + " players online");
            Messages.Add("Staff online: " + Server.Players.Where(p => !p.Info.IsHidden && p.Can(per)).JoinToString(r => String.Format("{0}", r.Name)));
            Messages.Add("Counting " + PlayerDB.BannedCount + " banned players (" + Math.Round(PlayerDB.BannedPercentage) + "%)");
            Messages.Add("This server runs 800Craft " + Updater.CurrentRelease.VersionString);
            Messages.Add("Type /Review to get staff to review your builds");
            Messages.Add("Griefers get banned instantly");
            Messages.Add("This scrolling feed is cool!");
            Messages.Add("Join our forums at Au70.Net");
            Messages.Add("We have an IRC at #Au70 in espernet");
            Messages.Add("This server has had " + PlayerDB.PlayerInfoList.Count() + " unique visitors");
        }
        public void Init(string image, World _world)
        {
            world = _world;
            List<List<bool>> pixels = new List<List<bool>>();
            //open up PNG file
            Bitmap bmp = new Bitmap(image);
            for (int x = 0; x < bmp.Width; x++)
            {
                pixels.Add(new List<bool>());
                for (int y = 0; y < bmp.Height; y++)
                {
                    pixels[x].Add(bmp.GetPixel(x, y).GetBrightness() > 0.5);
                }
            }
            //extract characters from PNG file
            for (int c = 33; c < 126; c++)
            {
                List<bool> charBlocks = new List<bool>();
                bool space = true;
                int bmpOffsetX = ((c - 32) % 16) * 8;
                int bmpOffsetY = ((int)((c - 32) / 16)) * 8;
                for (int X = 7; X >= 0; X--)
                {
                    for (int Y = 0; Y < 8; Y++)
                    {
                        bool type = pixels[X + bmpOffsetX][Y + bmpOffsetY];
                        if (space)
                        {
                            if (type)
                            {
                                Y = -1;
                                space = false;
                            }
                            continue;
                        }
                        else
                        {
                            charBlocks.Add(type);
                        }
                    }
                }
                charBlocks.Reverse();
                chars[c - 32] = charBlocks;
            }
            chars[0] = new List<bool>();
            for (int i = 0; i < (SPACE_UNIT - 2) * 8; i++)
            {
                chars[0].Add(false);
            }
        }

        public bool done = true; //check to see if one cycle is complete
        private void StartFeed(SchedulerTask task)
        {
            if (!started) { task.Stop(); return; }
            if (!done) return;
            try
            {
                done = false;
                RemoveText();
                if (ChangeMessage)
                {
                    switch (direction)
                    {
                        case Direction.one:
                        case Direction.two:
                            EndPos.X = FinishPos.X;
                            break;
                        case Direction.three:
                        case Direction.four:
                            EndPos.Y = FinishPos.Y;
                            break;
                    }
                    PickNewMessage();
                    ChangeMessage = false;
                }
                switch (direction)
                {
                    case Direction.one:
                        EndPos.X -= 7;
                        break;
                    case Direction.two:
                        EndPos.X += 7;
                        break;
                    case Direction.three:
                        EndPos.Y -= 7;
                        break;
                    case Direction.four:
                        EndPos.Y += 7;
                        break;
                }
                Render(Sentence);
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, e.ToString());
            }
        }

        public bool ChangeMessage = false; //a check if the previous sentence is complete
        public void RemoveText()
        {
            if (Blocks.Values.Count < 1) { ChangeMessage = true; }
            foreach (Vector3I block in Blocks.Values)
            {
                if (world.Map == null)
                {
                    started = false;
                    return;
                }
                if (world.IsLoaded)
                {
                    Vector3I removed;
                    world.Map.QueueUpdate(new BlockUpdate(null, block, Block.Black));
                    Blocks.TryRemove(block.ToString(), out removed);
                }
            }
        }

        //makes the block updates
        public void Render(string text)
        {
            List<byte> current = new List<byte>();
            for (int p = 0; p < text.Length; p++)
            {
                char c = text[p];
                List<bool> charTemp = chars[c - 32];
                for (int i = 0; i < charTemp.Count; i++)
                {
                    if (charTemp[i])
                    {
                        current.Add(textType);
                    }
                    else
                    {
                        current.Add(bgType);
                    }
                }
                if (p != text.Length - 1)
                {
                    for (int s = 0; s < 8; s++)
                    {
                        current.Add(bgType);
                    }
                }
            }
            if (current.Count < last.Count)
            {
                for (int j = current.Count; j < last.Count; j++)
                {
                    if (last[j] != originalMap[j])
                    {
                        sendWorldBlock(j, originalMap[j]);
                    }
                }
            }
            for (int k = 0; k < current.Count; k++)
            {
                if (k < last.Count && (last[k] == current[k]))
                {
                    continue;
                }
                sendWorldBlock(k, current[k]);
            }
            last = current;
            current.Clear();
            done = true;
        }

        public void Render(string text, Block t)
        {
            Byte temp = textType;
            textType = (byte)t;
            Render(text);
            textType = temp;
        }

        //gets the next sentence
        public void PickNewMessage()
        {
            if (FeedData.Messages.Count() > 0)
            {
                FeedData.AddMessages();
                if (MessageCount == FeedData.Messages.Count() - 1)
                {
                    MessageCount = 0;
                    Sentence = FeedData.Messages[MessageCount];
                    return;
                }
                MessageCount++;
                Sentence = FeedData.Messages[MessageCount];
            }
        }

        //processess one blockupdate
        public void sendWorldBlock(int index, byte type)
        {
            if (world.Map == null)
            {
                started = false;
                return;
            }
            if (world.IsLoaded)
            {
                int x = 0, y = 0, z = 0;
                switch (direction)
                {
                    case Direction.one:
                        x = (int)(index / 8) + EndPos.X;
                        y = StartPos.Y;
                        z = StartPos.Z + (index % 8);
                        if (world.map.InBounds(x, y, z))
                        {
                            if (x >= StartPos.X && x <= FinishPos.X)
                            {
                                if ((Block)type != Block.Air)
                                {
                                    Vector3I Pos = new Vector3I(x, y, z);
                                    world.map.QueueUpdate(new BlockUpdate(null, Pos, (Block)type));
                                    Blocks.TryAdd(Pos.ToString(), Pos);
                                }
                            }
                        }
                        break;
                    case Direction.two:
                        x = (short)(EndPos.X - (index / 8));
                        y = (short)StartPos.Y;
                        z = (short)(StartPos.Z + (index % 8));
                        if (world.map.InBounds(x, y, z))
                        {
                            if (x <= StartPos.X && x >= FinishPos.X)
                            {
                                if ((Block)type != Block.Air)
                                {
                                    Vector3I Pos = new Vector3I(x, y, z);
                                    world.map.QueueUpdate(new BlockUpdate(null, Pos, (Block)type));
                                    Blocks.TryAdd(Pos.ToString(), Pos);
                                }
                            }
                        }
                        break;
                    case Direction.three:
                        x = (short)StartPos.X;
                        y = (short)(EndPos.Y + (index / 8));
                        z = (short)(StartPos.Z + (index % 8));
                        if (world.map.InBounds(x, y, z))
                        {
                            if (y >= StartPos.Y && y <= FinishPos.Y)
                            {
                                if ((Block)type != Block.Air)
                                {
                                    Vector3I Pos = new Vector3I(x, y, z);
                                    world.map.QueueUpdate(new BlockUpdate(null, Pos, (Block)type));
                                    Blocks.TryAdd(Pos.ToString(), Pos);
                                }
                            }
                        }
                        break;
                    case Direction.four:
                        x = (short)StartPos.X;
                        y = (short)(EndPos.Y - (index / 8));
                        z = (short)(StartPos.Z + (index % 8));
                        if (world.map.InBounds(x, y, z))
                        {
                            if (y <= StartPos.Y && y >= FinishPos.Y)
                            {
                                if ((Block)type != Block.Air)
                                {
                                    Vector3I Pos = new Vector3I(x, y, z);
                                    world.map.QueueUpdate(new BlockUpdate(null, Pos, (Block)type));
                                    Blocks.TryAdd(Pos.ToString(), Pos);
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }

    public enum Direction
    {
        one,
        two,
        three,
        four,
        Null
    }
}



