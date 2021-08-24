using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.System.WarModule
{
    /// <summary>
    /// 战棋地形
    /// </summary>
    public class ChessTerrain
    {
        private readonly List<TerrainSprite> sprites;
        public IEnumerable<TerrainSprite> Sprites => sprites;

        public int GetBuff(CardState.Cons con) => Sprites.Sum(s=>s.GetBuff(con));

        public int GetServed(CardState.Cons con,IChessOperator op) => Sprites.Sum(o => o.ServedBuff(con, op));
        public ChessTerrain()
        {
            sprites = new List<TerrainSprite>();
        }

        /// <summary>
        /// 添加状态
        /// </summary>

        public void AddSprite(TerrainSprite sprite)
        {
            if (sprites.Any(s=>s.InstanceId == sprite.InstanceId))
                throw new InvalidOperationException(
                    $"Duplicated sprite[{sprite.InstanceId}] at pos({sprite.Pos})Challenger{sprite.IsChallenger}");
            sprites.Add(sprite);
        }

        public void RemoveSprite(TerrainSprite sprite) => sprites.Remove(sprite);
    }
}