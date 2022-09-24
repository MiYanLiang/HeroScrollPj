using System.WarModule;
using Assets.System.WarModule;
using CorrelateLib;
using UnityEngine;
using UnityEngine.UI;

public class GameCardPropertyViewUi : MonoBehaviour
{
    [SerializeField] CardPropertyUi PowerPropUi;
    [SerializeField] CardPropertyUi StrPropUi;
    [SerializeField] CardPropertyUi HpPropUi;
    [SerializeField] CardPropertyUi IntPropUi;
    [SerializeField] CardPropertyUi SpeedPropUi;
    [SerializeField] CardPropertyUi DodgePropUi;
    [SerializeField] CardPropertyUi ArmorPropUi;
    [SerializeField] CardPropertyUi MagicPropUi;
    [SerializeField] CardPropertyUi CriPropUi;
    [SerializeField] CardPropertyUi RouPropUi;

    public void UpdateAttributes(GameCard c)
    {
        StrPropUi.Display(false);
        HpPropUi.Display(false);
        IntPropUi.Display(false);
        SpeedPropUi.Display(false);
        DodgePropUi.Display(false);
        ArmorPropUi.Display(false);
        MagicPropUi.Display(false);
        CriPropUi.Display(false);
        RouPropUi.Display(false);

        PowerPropUi.Set(c.Power());
        var fc = new FightCardData(c);
        if (fc.CardType == GameCardType.Hero)
        {
            var heroTable = DataTable.Hero;
            var hero = heroTable[c.CardId];
            var selfStrength = hero.GetArousedStrength(arouse: c.Arouse, level: c.Level);
            var deputyStrength = heroTable.GetDeputyStrength(
                c.Deputy1Id, c.Deputy1Level,
                c.Deputy2Id, c.Deputy2Level,
                c.Deputy3Id, c.Deputy3Level,
                c.Deputy4Id, c.Deputy4Level);
            StrPropUi.Set(selfStrength, deputyStrength);
            var selfHp = hero.GetArousedHitPoint(arouse: c.Arouse, level: c.Level);
            var deputyHp = heroTable.GetDeputyHitPoint(
                c.Deputy1Id, c.Deputy1Level,
                c.Deputy2Id, c.Deputy2Level,
                c.Deputy3Id, c.Deputy3Level,
                c.Deputy4Id, c.Deputy4Level);
            HpPropUi.Set(selfHp, deputyHp);
            var selfInt = hero.GetArousedIntelligent(c.Arouse);
            var deputyInt = heroTable.GetDeputyIntelligent(
                c.Deputy1Id, c.Deputy1Level,
                c.Deputy2Id, c.Deputy2Level,
                c.Deputy3Id, c.Deputy3Level,
                c.Deputy4Id, c.Deputy4Level);
            IntPropUi.Set(selfInt, deputyInt);
            var selfSpeed = hero.GetArousedSpeed(c.Arouse);
            var deputySpeed = heroTable.GetDeputySpeed(
                c.Deputy1Id, c.Deputy1Level,
                c.Deputy2Id, c.Deputy2Level,
                c.Deputy3Id, c.Deputy3Level,
                c.Deputy4Id, c.Deputy4Level);
            SpeedPropUi.Set(selfSpeed, deputySpeed);
            var selfDodge = hero.GetArousedDodge(c.Arouse);
            var deputyDodge = heroTable.GetDeputyDodge(
                c.Deputy1Id, c.Deputy1Level,
                c.Deputy2Id, c.Deputy2Level,
                c.Deputy3Id, c.Deputy3Level,
                c.Deputy4Id, c.Deputy4Level);
            DodgePropUi.Set(selfDodge, deputyDodge);
            var selfArmor = hero.GetArousedArmor(c.Arouse);
            var deputyArmor = heroTable.GetDeputyArmor(
                c.Deputy1Id, c.Deputy1Level,
                c.Deputy2Id, c.Deputy2Level,
                c.Deputy3Id, c.Deputy3Level,
                c.Deputy4Id, c.Deputy4Level);
            ArmorPropUi.Set(selfArmor, deputyArmor);
            var selfMagic = hero.GetArousedMagicRest(c.Arouse);
            var deputyMagic = heroTable.GetDeputyMagicRest(
                c.Deputy1Id, c.Deputy1Level,
                c.Deputy2Id, c.Deputy2Level,
                c.Deputy3Id, c.Deputy3Level,
                c.Deputy4Id, c.Deputy4Level);
            MagicPropUi.Set(selfMagic, deputyMagic);
            var info = c.GetInfo();
            CriPropUi.Set(info.CriticalRatio);
            RouPropUi.Set(info.RougeRatio);
        }
        else
        {

            StrPropUi.Set(fc.Damage);
            HpPropUi.Set(fc.HitPoint);
            if (fc.CardType != GameCardType.Trap)
                SpeedPropUi.Set(fc.Speed);
        }
        StrPropUi.SetTitle(fc.CardType == GameCardType.Hero ? "武力：" : "威力：");
    }

}