using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using CorrelateLib;
using UnityEngine;

public class DataTableTester : MonoBehaviour
{
    public int WarId;

    private static int[] YuanZhengIds { get; } = new[] { 121, 122, 123, 124, 125, 126, 127, 128, 129, 130 };
    private static List<CheckpointTable> YZCheckpoints { get; set; }

    public void CheckWarId()
    {
        var isYuanZheng = YuanZhengIds.Any(w => w == WarId);

        IEnumerable<CheckpointTable> checkPoints;
        //暂时处理如果远征的话，所有远征战役的宝箱都合法
        if (isYuanZheng)
        {
            if (YZCheckpoints == null)
            {
                var list = new List<CheckpointTable>();
                foreach (var id in YuanZhengIds)
                    list.AddRange(GetCheckPoints(id));
                YZCheckpoints = list.Distinct().ToList();
            }
            checkPoints = YZCheckpoints;
        }
        else
            checkPoints = GetCheckPoints(WarId);

        var battleEventIds = checkPoints.Where(c => c.BattleEventTableId > 0).Select(c => c.BattleEventTableId).Distinct().ToList();
        var beTable = DataTable.BattleEvent;
        var battleEvents = beTable.Join(battleEventIds, be => be.Key, id => id, (be, _) => be.Value).ToList();
        var battleChests = battleEvents
            .Where(b => b.WarChestTableIds != null && b.WarChestTableIds.Length > 0)
            .SelectMany(b => b.WarChestTableIds).Distinct().ToList();
        
        var enemyTable = DataTable.Enemy;
        var enemyUnitTable = DataTable.EnemyUnit;
        var temp = battleEvents.Where(b => b.EnemyTableIndexes != null && b.EnemyTableIndexes.Length > 0)
            .SelectMany(b => b.EnemyTableIndexes)
            .Distinct().ToArray();
        var enemyChests =temp.Join(enemyTable, id => id, e => e.Key, (_, e) => e.Value)
            .SelectMany(GetEnemiesByPos).Distinct()
            .Join(enemyUnitTable.Where(e => e.Value.CardType == -1 && //-1表示引用陷阱表
                                            e.Value.Rarity == 12), //12表示战役宝箱
                id => id, e => e.Key, (_, e) => e.Value)
            .Select(e => e.Star).Distinct().ToList();

        var sb = new StringBuilder();
        battleChests.Concat(enemyChests).Distinct().ToList().ForEach(c => sb.Append($"{c},"));
        Debug.Log($"WarChest[{sb}]");
    }

    private int[] GetEnemiesByPos(EnemyTable e) => new[]
    {
        e.Pos1, e.Pos11,
        e.Pos2, e.Pos12,
        e.Pos3, e.Pos13,
        e.Pos4, e.Pos14,
        e.Pos5, e.Pos15,
        e.Pos6, e.Pos16,
        e.Pos7, e.Pos17,
        e.Pos8, e.Pos18,
        e.Pos9, e.Pos19,
        e.Pos10, e.Pos20
    };

    IEnumerable<CheckpointTable> GetCheckPoints(int warId)
    {
        var war = DataTable.War[warId];
        var checkPoint = DataTable.Checkpoint[war.BeginPoint];
        var table = DataTable.Checkpoint;
        var list = new List<CheckpointTable> { checkPoint };
        var result = new List<CheckpointTable>();
        while (list.Count > 0)
        {
            var currentPoint = list.First();
            if (result.All(c => c.Id != currentPoint.Id))
                result.Add(currentPoint);
            list.Remove(currentPoint);
            var next = NextCheckPoints(table, currentPoint);
            foreach (var nCp in next)
            {
                if (list.Any(c => c.Id == nCp.Id)) continue;
                list.Add(nCp);
            }
            if (result.Count > 99999) throw new NotSupportedException($"WarId[{warId}] checkPoints > 99999!");
        }
        return result;

    }
    private static IEnumerable<CheckpointTable> NextCheckPoints(IReadOnlyDictionary<int, CheckpointTable> table, CheckpointTable checkpoint)
    {
        var nextPoint = table[checkpoint.Id].Next;
        if (nextPoint == null || nextPoint.Length == 0) return EmptyCheckPoint;
        return nextPoint.Select(id => table[id]);
    }
    private static readonly CheckpointTable[] EmptyCheckPoint = Array.Empty<CheckpointTable>();

}
