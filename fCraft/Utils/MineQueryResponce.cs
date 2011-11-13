using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.MineQuery
{
    class MineQueryResponse
    {
        public int serverPort { get; set; }
        public int playerCount { get; set; }
        public int maxPlayers { get; set; }
        public List<String> playerList { get; set; }
    }
}