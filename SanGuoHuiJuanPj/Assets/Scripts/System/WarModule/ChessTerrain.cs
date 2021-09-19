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
        private readonly List<PosSprite> sprites;
        public IEnumerable<PosSprite> Sprites => sprites;

        public int GetServed(CardState.Cons con,IChessOperator op) => Sprites.Sum(o => o.ServedBuff(con, op));
        public ChessTerrain()
        {
            sprites = new List<PosSprite>();
        }

        /// <summary>
        /// 添加状态
        /// </summary>

        public void AddSprite(PosSprite sprite)
        {
            if (sprites.Any(s=>s.InstanceId == sprite.InstanceId))
                throw new InvalidOperationException(
                    $"Duplicated sprite[{sprite.InstanceId}] at pos({sprite.Pos})Challenger{sprite.IsChallenger}");
            sprites.Add(sprite);
        }

        public void RemoveSprite(PosSprite sprite) => sprites.Remove(sprite);
    }
}