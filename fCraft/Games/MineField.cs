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

//Copyright (C) <2012> Jon Baker(http://au70.net)
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
        private const int _ground = 15; 
        private Map _map;
        public MineField()
        {
            _world = WorldManager.FindWorldExact("Minefield"); //can only be used when the world is loaded
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
