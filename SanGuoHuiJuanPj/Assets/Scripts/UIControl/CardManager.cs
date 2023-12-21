using System.Collections.Generic;

public class CardManager
{
    /// <summary>
    /// 重置羁绊映像
    /// </summary>
    /// <param name="jiBanMap"></param>
    public static void ResetJiBan(Dictionary<int, JiBanActivedClass> jiBanMap)
    {
        jiBanMap.Clear();
        foreach (var jiBan in DataTable.JiBan.Values)
        {
            if (jiBan.IsOpen == 0) continue;
            JiBanActivedClass jiBanActivedClass = new JiBanActivedClass();
            jiBanActivedClass.JiBanId = jiBan.Id;
            jiBanActivedClass.IsActive = false;
            jiBanActivedClass.List = new List<JiBanCardTypeClass>();
            jiBanActivedClass.IsHadBossId = jiBan.BossCards.Length > 0;

            for (int i = 0; i < jiBan.Cards.Length; i++)
            {
                var card = jiBan.Cards[i];
                jiBanActivedClass.List.Add(new JiBanCardTypeClass
                {
                    CardId = card.CardId,
                    CardType = card.CardType,
                    Cards = new List<FightCardData>(),
                    BossId = jiBan.BossCards.Length == 0 ? 0 : jiBan.BossCards[i].CardId
                });
            }
            jiBanMap.Add(jiBan.Id, jiBanActivedClass);
        }
    }
}