using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using ServiceStack.Text;
using System.Threading;

namespace fCraft.MineQuery
{
    /// <summary>
    /// Unofficial MineQuery plugin, originally a craftbukkit plugin, written by Glenn Mariën
    /// This can be used to report various info to minestatus.net
    /// </summary>
    class MineQuery
    {
        private static MineQuery instance;
        private TcpListener listener;
        private bool started = false;

        private MineQuery()
        {
            // Empty, singleton
        }

        public static MineQuery GetInstance()
        {
            if (instance == null)
            {
                instance = new MineQuery();
            }

            return instance;
        }

        public void Start()
        {
            Thread minequeryThread = new Thread(new ThreadStart(delegate
            {
                if (!started)
                {
                    started = true;

                    listener = new TcpListener(IPAddress.Any, Server.Port + 1);
                    listener.Start();

                    Logger.Log(LogType.SystemActivity, "Started MineQuery on port " + (Server.Port + 1));

                    // Start loop
                    while (true)
                    {
                        if (listener.Pending())
                        {
                            NetworkStream clientStream = null;
                            StreamReader clientReader = null;
                            TcpClient client = null;

                            try
                            {
                                client = listener.AcceptTcpClient();

                                if (client != null)
                                {
                                    clientStream = client.GetStream();
                                    clientReader = new StreamReader(clientStream);
                                    clientStream.ReadTimeout = 10000;

                                    Logger.Log(LogType.SystemActivity, "MineQuery client connected from " + client.Client.RemoteEndPoint.ToString());

                                    String request = clientReader.ReadLine();
                                    byte[] dataSend = Encoding.UTF8.GetBytes("Invalid query");

                                    if (request.ToUpper().Replace(Environment.NewLine, String.Empty).Equals("QUERY"))
                                    {
                                        StringBuilder dataAssemble = new StringBuilder();
                                        dataAssemble.AppendLine("SERVERPORT " + Server.Port);
                                        dataAssemble.AppendLine("PLAYERCOUNT " + Server.Players.Length);
                                        dataAssemble.AppendLine("MAXPLAYERS " + ConfigKey.MaxPlayers.GetString());

                                        if (Server.Players.Length > 0)
                                        {
                                            Player[] players = Server.Players;
                                            String[] playerNames = new String[players.Length];

                                            for (int i = 0; i < players.Length; i++)
                                            {
                                                playerNames[i] = players[i].Name;
                                            }

                                            dataAssemble.AppendLine("PLAYERLIST [" + String.Join(",", playerNames) + "]");
                                        }
                                        else
                                        {
                                            dataAssemble.AppendLine("PLAYERLIST []");
                                        }

                                        dataSend = Encoding.UTF8.GetBytes(dataAssemble.ToString());
                                    }
                                    else if (request.ToUpper().Replace(Environment.NewLine, String.Empty).Equals("QUERY_JSON"))
                                    {
                                        MineQueryResponse response = new MineQueryResponse()
                                        {
                                            serverPort = Server.Port,
                                            maxPlayers = ConfigKey.MaxPlayers.GetInt(),
                                            playerCount = Server.Players.Length,
                                            playerList = new List<string>()
                                        };

                                        if (Server.Players.Length > 0)
                                        {
                                            Player[] players = Server.Players;

                                            for (int i = 0; i < players.Length; i++)
                                            {
                                                response.playerList.Add(players[i].Name);
                                            }
                                        }

                                        dataSend = Encoding.UTF8.GetBytes(JsonSerializer.SerializeToString(response));
                                    }

                                    clientStream.Write(dataSend, 0, dataSend.Length);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(LogType.Error, "Unable to execute MineQuery: " + ex);
                            }
                            finally
                            {
                                try
                                {
                                    clientStream.Close();
                                    client.Close();
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log(LogType.Error, "Unable to close MineQuery network stream: " + ex);
                                }
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception("MineQuery was already started!");
                }
            }));

            minequeryThread.Start();
        }
    }
}