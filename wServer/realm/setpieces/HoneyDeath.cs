﻿using db.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm.setpieces
{
    internal class HoneyDeath : ISetPiece
    {
        public int Size
        {
            get { return 3; }
        }

        private byte[,] SetPiece
        {
            get
            {
                return new byte[,]
                {
                    {1, 1, 1},
                    {1, 1, 1},
                    {1, 1, 1},

                };
            }
        }

        public void RenderSetPiece(World world, IntPoint pos)
        {
            XmlData dat = world.Manager.GameData;

            IntPoint p = new IntPoint
            {
                X = pos.X - (Size / 2),
                Y = pos.Y - (Size / 2)
            };

            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    if (SetPiece[y, x] == 1)
                    {
                        WmapTile tile = world.Map[x + p.X, y + p.Y].Clone();
                        tile.TileId = dat.IdToTileType["Hive Honey"];
                        tile.ObjType = 0;
                        world.Map[x + p.X, y + p.Y] = tile;
                    }
                }
            }
        }
    }
}
