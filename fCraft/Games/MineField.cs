using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.MapConversion;

namespace fCraft
{
    public class MineField
    {
        private World _world;
        private int _ground = 15;
        private Map _map;
        public MineField()
        {
            _world = WorldManager.FindWorldExact("Minefield");
        }
        public void Start()
        {
            Map map = MapGenerator.GenerateFlatgrass(128, 128, 32);
            map.Save("maps/minefield.fcm");
            if (_world != null)
            {
                WorldManager.RemoveWorld(_world);
            }
            WorldManager.AddWorld(Player.Console, "Minefield", map, false);
            _map = map;
            SetUpRed();
            SetUpGreen();
        }

        private void SetUpRed()
        {
            for (int x = 1; x <= _map.Length; x++)
            {
                for (int y = 1; y <= 10; y++)
                {
                    _map.SetBlock(x, y, _ground, Block.Red);
                }
            }
        }

        private void SetUpGreen()
        {
            for (int x = _map.Length; x >= 1; x--)
            {
                for (int y = _map.Width; y >= _map.Width - 10; y--)
                {
                    _map.SetBlock(x, y, _ground, Block.Green);
                }
            }
        }
    }
}
