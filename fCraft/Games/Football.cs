//temp
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
using fCraft;
using fCraft.Events;
using System.Collections.Concurrent;
using System.Threading;

namespace fCraft
{
    public class Football
    {
        private World _world;
        private Vector3I _startPos;
        public Football(Player player, World world, Vector3I FootballPos)
        {
            _world = world;
            Player.Clicked += ClickedFootball;
        }

        public void ResetFootball()
        {
            if (_startPos == null)
            {
                _startPos.X = _world.Map.Bounds.XMax - _world.Map.Bounds.XMin;
                _startPos.Y = _world.Map.Bounds.YMax - _world.Map.Bounds.YMin;
                for (int z = _world.Map.Bounds.ZMax; z > 0; z--)
                {
                    if (_world.Map.GetBlock(_startPos.X, _startPos.Y, z) != Block.Air)
                    {
                        _startPos.Z = z + 1;
                        break;
                    }
                }
            }
            _world.Map.QueueUpdate(new BlockUpdate(null, _startPos, Block.White));
        }

        private FootballBehavior _footballBehavior = new FootballBehavior();
        public void ClickedFootball(object sender, PlayerClickedEventArgs e)
        {
            //replace e.coords with player.Pos.toblock() (moving event)
            if (e.Coords == _world.footballPos)
            {
                double ksi = 2.0 * Math.PI * (-e.Player.Position.L) / 256.0;
                double r = Math.Cos(ksi);
                double phi = 2.0 * Math.PI * (e.Player.Position.R - 64) / 256.0;
                Vector3F dir = new Vector3F((float)(r * Math.Cos(phi)), (float)(r * Math.Sin(phi)), (float)(Math.Sin(ksi)));
                _world.AddPhysicsTask(new Particle(_world, e.Coords, dir, e.Player, Block.White, _footballBehavior), 0);
            }
        }
    }
}
