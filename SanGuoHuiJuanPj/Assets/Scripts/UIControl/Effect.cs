﻿using System;
using Assets.System.WarModule;

public static class Effect
{
    public const string GetGold = "GetGold";
    public const string DropBlood = "dropBlood";
    public const string SpellTextH = "spellTextH";
    public const string SpellTextV = "spellTextV";

    #region 特效 Spark
    #region 旧特效
    public const string Basic0A = "0A";
    public const string Blademail7A = "7A";
    public const string Explode = "201A";
    public const string Dropping = "209A";
    public const string Bow20A = "20A";
    public const string FeiJia3A = "3A";
    public const string Shield4A = "4A";
    public const string SuckBlood6A = "6A";
    public const string Cavalry9A = "9A";
    public const string Cavalry60A = "60A";
    public const string Cavalry16A = "16A";
    public const string Blade17A = "17A";
    public const string CrossBow19A = "19A";
    public const string Crossbow49A = "49A";
    public const string Scribe50A = "50A";
    public const string TengJia57A = "57A";
    public const string Stimulate12A = "12A";
    public const string Guard13A = "13A";
    public const string YellowBand65A = "65A";
    public const string YellowBand65B = "65B";
    public const string HeavyCavalry58A = "58A";
    public const string Barbarians56A = "56A";
    public const string FireShip55A0 = "55A0";
    public const string FireShip55A = "55A";
    public const string FemaleRider44A = "44A";
    public const string Mechanical40A = "40A";
    public const string Debate34A = "34A";
    public const string Controversy35A = "35A";
    public const string Persuade47A = "47A";
    public const string Convince48A = "48A";
    public const string ThrowRocks24A = "24A";
    public const string SiegeMachine23A = "23A";
    public const string Support39A = "39A";
    public const string StateAffairs38A = "38A";
    public const string Heal42A = "42A";
    public const string Heal43A = "43A";
    public const string Assassin25A = "25A";
    public const string Warship21A = "21A";
    public const string Chariot22A = "22A";
    public const string Axe18A = "18A";
    public const string Halberd15A = "15A";
    public const string Knight11A = "11A";
    public const string Daredevil10A = "10A";
    public const string Elephant8A = "8A";
    public const string Spear59A = "59A";
    public const string LongSpear14A = "14A";
    public const string Advisor26A = "26A";
    public const string Warlock28A = "28A";
    public const string Warlock29A = "29A";
    public const string PoisonMaster30A = "30A";
    public const string PoisonMaster31A = "31A";
    public const string FlagBearer32A = "32A";
    public const string FlagBearer33A = "33A";
    public const string Counselor36A= "36A";
    public const string Counselor37A= "37A";
    public const string Lady45A = "45A";
    public const string Lady46A = "46A";
    public const string CrossBow51A="51A";
    public const string LongBow52A = "52A";
    public const string Anchorite53A= "53A";
    public const string Anchorite54A= "54A";
    #endregion

    public const string VTextDodge = "A2";//闪避
    public const string VTextParry = "A3";//格挡
    public const string VTextInvincible = "A1";//无敌
    public const string VTextShield = "4";//?
    #region 旧特效
    #endregion
    public const int Basic001 = 001;
    public static int GetHeroSparkId(int military, int skill)//skill=-1(没有特效)，skill=0(默认特效001),skill>0（技能专属特效）
    {
        if (skill == 0) return Basic001;
        var value = Basic001; 
        switch (military)
        {
            case 0://武夫
                value = 001;break;

            case 49://短弓
                value = 026;break;

            case 50://文士
                value = 052;break;

            case 1:
            case 66:
            case 67: //近战
                value = 087;break;

            case 4: 
            case 68:
            case 69://大盾
                switch (skill)
                {
                    case 1://持盾 回合开始前加盾
                        value = 003;break;
                    case 2://战盾 攻击时加盾
                        value = 003;break;
                }
                break;

            case 2:
            case 70: 
            case 71://铁卫
                value = 001;break;

            case 3:
            case 72: 
            case 73://飞甲
                value = 002;break;

            case 6:
            case 74:
            case 75://虎卫
                switch (skill)
                {
                    case 1://攻击目标
                        value = 005; break;
                    case 2://自身加血
                        value = 043; break;
                }
                break;

            case 7:
            case 76:
            case 77://刺盾
                value = 006;break;

            case 78:
            case 79:
            case 80://血衣
                value = 001;break;

            case 5:
            case 81:
            case 82://陷阵
                value = 08701;break;//自身加盾

            case 41:
            case 83:
            case 84://敢死
                switch (skill)
                {
                    case 1://自身加盾
                        value = 08702; break;
                    case 2://自身加血
                        value = 043; break;
                }
                break;

            case 10:
            case 85:
            case 86://先登
                switch (skill)
                {
                    case 1://普通
                        value = 001; break;
                    case 2://血祭
                        value = 009; break;
                }
                break;

            case 87:
            case 88:
            case 89://青州
                switch (skill)
                {
                    case 1://普通
                        value = 001; break;
                    case 2://卸甲归田
                        value = 044; break;
                }
                break;

            case 90:
            case 91:
            case 92://链锁
                switch (skill)
                {
                    case 1://普通
                        value = 001; break;
                    case 2://捆缚
                        value = 101; break;
                }
                break;

            case 93:
            case 94:
            case 95://解烦
                switch (skill)
                {
                    case 1://引燃
                        value = 218; break;
                    case 2://自爆
                        value = 071; break;
                }
                break;

            case 57:
            case 96:
            case 97://藤甲
                value = 001;break;

            case 98:
            case 99:
            case 100://鬼兵
                switch (skill)
                {
                    case 1://普通
                        value = 001; break;
                    case 2://还魂
                        value = 092; break;
                    case 3://借尸
                        value = 09201; break;
                }
                break;

            case 59:
            case 14:
            case 101://长枪
                value = 015;break;

            case 15:
            case 102:
            case 103://大戟
                value = 016;break;

            case 17:
            case 104:
            case 105://大刀
                value = 021;break;

            case 18:
            case 106:
            case 107://大斧
                value = 022;break;

            case 108:
            case 109:
            case 110://狼牙棒
                value = 038;break;

            case 12:
            case 114:
            case 115://神武
                value = 013;break;

            case 116:
            case 117:
            case 118://白毦
                value = 069;break;

            case 119:
            case 120:
            case 121://朴刀
                value = 180;break;

            case 13:
            case 122:
            case 123://羽林
                value = 014; break;

            case 124:
            case 125:
            case 126://双戟
                value = 056;break;

            case 65:
            case 127:
            case 128://黄巾
                value = 147; break;//群起攻击

            case 25:
            case 129:
            case 130://刺客
                switch (skill)
                {
                    case 1://攻击建筑或陷阱
                        value = 001; break;
                    case 2://攻击武将士兵
                        value = 011; break;
                }
                break;

            case 56:
            case 131:
            case 132://蛮族
                switch (skill)
                {
                    case 1://攻击建筑或陷阱
                        value = 001; break;
                    case 2://攻击武将士兵
                        value = 012; break;
                }
                break;

            case 133:
            case 134:
            case 135://丹阳
                value = 149;break;

            case 9:
            case 60:
            case 136://飞骑
                value = 008;break;

            case 137:
            case 138:
            case 139://白马
                value = 084;break;

            case 11:
            case 140:
            case 141://虎豹骑
                value = 057;break;

            case 16:
            case 142: 
            case 143://骠骑
                value = 020;break;

            case 44:
            case 144:
            case 145://巾帼
                switch (skill)
                {
                    case 1://攻击建筑或陷阱
                        value = 001; break;
                    case 2://攻击武将士兵
                        value = 098; break;
                }
                break;

            case 146:
            case 147:
            case 148://弓骑
                value = 025;break;

            case 149:
            case 150:
            case 151://狼骑
                value = 085;break;

            case 58:
            case 152:
            case 153://铁骑
                value = 010; break;

            case 22:
            case 172:
            case 173://战车
                value = 028;break;

            case 8:
            case 174:
            case 175://战象
                switch (skill)
                {
                    case 1://普通攻击
                        value = 007; break;
                    case 2://暴击或会心
                        value = 238; break;
                }
                break;

            case 23:
            case 176:
            case 177://攻城车
                value = 062; break;//攻击建筑或陷阱

            case 24:
            case 178:
            case 179://投石车
                value = 031;break;

            case 19:
            case 51:
            case 180://连弩
                value = 024;break;

            case 181:
            case 182:
            case 183://元戎弩
                value = 100;break;

            case 20:
            case 52:
            case 184://大弓
                value = 02501;break;

            case 185:
            case 186:
            case 187://火弓
                value = 02502;break;

            case 188:
            case 189:
            case 190://重弩
                value = 01502;break;

            case 191:
            case 192:
            case 193://无当
                value = 023;break;

            case 21:
            case 194:
            case 195://艨艟
                value = 027;break;

            case 55:
            case 196:
            case 197://火船
                switch (skill)
                {
                    case 1://引燃
                        value = 030; break;
                    case 2://自爆
                        value = 07101; break;
                }
                break;

            case 26:
            case 27:
            case 203://军师
                value = 034;break;

            case 28:
            case 29:
            case 206://术士
                value = 200;break;

            case 30:
            case 31:
            case 208://毒士
                value = 039;break;

            case 32:
            case 33:
            case 209://统帅
                break;

            case 53:
            case 54:
            case 210://隐士
                value = 077;break;

            case 214:
            case 215:
            case 216://狂士
                value = 166;break;

            case 36:
            case 37:
            case 217://谋士
                value = 513;break;

            case 218:
            case 219:
            case 220://巫祝
                value = 065;break;

            case 34:
            case 35:
            case 221://辩士
                value = 096;break;

            case 47:
            case 48:
            case 222://说客
                value = 042;break;

            case 61:
            case 62:
            case 63://红颜
                value = 169;break;

            case 38:
            case 226:
            case 227://内政
                value = 091;break;

            case 45:
            case 231:
            case 232://倾城
                value = 050;break;

            case 46:
            case 233:
            case 234://倾国
                value = 051;break;

            case 39:
            case 238:
            case 239://辅佐
                value = 045;break;

            case 40:
            case 240:
            case 241://器械
                value = 046;break;

            case 42:
            case 43:
            case 242://医士
                value = 092;break;

            case 243://壮士
                value = 001;break;

            case 244://短弩
                value = 141;break;
        }
        return value;
    }
    #region 旧特效
    public static string GetTowerSpark(int towerId, int skill)
    {
        switch (towerId)
        {
            case 0: //营寨
            case 2: //奏乐台
                return Heal42A;
            case 6://轩辕台
            case 3://箭塔
                return Bow20A;
            case 1: //投石台
            default:
                return Basic0A;
        }
    }
    #endregion
    /// <summary>
    /// 建筑（塔）特效
    /// </summary>
    /// <param name="towerId"></param>
    /// <param name="skill"></param>
    /// <returns></returns>
    public static int GetTowerSparkId(int towerId, int skill)
    {
        switch (towerId)
        {
            case 0: //营寨
                switch (skill) 
                {
                    case 1:
                        return 092;
                    case 2:
                        return 092;
                }
                return 001;

            case 2: //奏乐台
                return 092;

            case 6://轩辕台
                return 003;

            case 3://箭楼
                return 025;

            case 1: //投石台
                return 061;

            default:
                return 001;
        }
    }

    /// <summary>
    /// 陷阱特效
    /// </summary>
    /// <param name="trapId"></param>
    /// <param name="skill"></param>
    /// <returns></returns>
    public static int GetTrapSparkId(int trapId, int skill)
    {
        switch (trapId)
        {
            case 0://拒马
                return 006;
            case 1://地雷
                return 064;
            case 9://滚石
                return 063;
            case 10://滚木
                return 063;
            default:
                return 001;
        }
    }

    //需要反向显示的特效
    public static bool IsInvertControl(int effectId)
    {
        switch (effectId)
        {
            case 006:
            case 015:
                return true;
            default: return false;
        }
    }
    #endregion

    #region 卡牌状态图标 StateIcon
    public static int GetStateIconId(CardState.Cons con)
    {
        /* 注意：这里的buff必须概括当前所有"状态"
         * 类似于 力量up, 护甲up 之类的状态，是基础状态。一般用于塔的附加状态。
         * 一些会影响到这些的状态如黄巾，武魂，是优先显示自身(黄巾，武魂)的状态图标。
         * 不需要显示图标的时候，return -1
         */

        switch (con)
        {
            // 眩晕
            case CardState.Cons.Stunned:
                return 31;
            // 护盾
            case CardState.Cons.Shield:
                return 02;
            // 无敌
            case CardState.Cons.Invincible:
                return 44;
            // 流血
            case CardState.Cons.Bleed:
                return 41;
            // 毒
            case CardState.Cons.Poison:
                return 09;
            // 灼烧
            case CardState.Cons.Burn:
                return 08;
            // 武魂
            case CardState.Cons.BattleSoul:
                return -1;
            // 战意
            case CardState.Cons.Stimulate:
                return -1;
            // 禁锢
            case CardState.Cons.Imprisoned:
                return 32;
            // 混乱
            case CardState.Cons.Confuse:
                return 34;
            // 怯战
            case CardState.Cons.Cowardly:
                return 33;
            // 武力Up
            case CardState.Cons.StrengthUp:
                return 11;
            // 闪避Up
            case CardState.Cons.DodgeUp:
                return 17;
            // 暴击Up
            case CardState.Cons.CriticalUp:
                return 18;
            // 会心Up
            case CardState.Cons.RouseUp:
                return 19;
            // 护甲Up
            case CardState.Cons.ArmorUp:
                return 14;
            // 血战
            case CardState.Cons.DeathFight:
                return 45;
            // 卸甲
            case CardState.Cons.Disarmed:
                return 24;
            // 内助
            case CardState.Cons.Neizhu:
                return 06;
            //神助
            case CardState.Cons.ShenZhu:
                return 07;
            // 缓冲、抵消盾
            case CardState.Cons.EaseShield:
                return -1;
            // 迷雾
            case CardState.Cons.Forge:
                return 50;
            // 杀气
            case CardState.Cons.Murderous:
                return 01;
            // 连环
            case CardState.Cons.Chained:
                return 48;
            // 黄巾
            case CardState.Cons.YellowBand:
                return 47;
            default:
                throw new ArgumentOutOfRangeException(nameof(con), con, null);
        }

    }
    #endregion

    #region 卡牌Buff状态

    /// <summary>
    /// 获取卡牌buffId，返回-1表示没有buff。
    /// </summary>
    /// <param name="con"></param>
    /// <returns></returns>
    public static int GetBuffId(CardState.Cons con)
    {
        /*
         * 某种程度上来说，
         * Buff与图标一样都是表示“状态”
         * 但buff会加强画面效果更直观卡牌状态。
         * 但有个缺点是它是覆盖整张卡牌，
         * 所以一般都用在特需buff类，挂上很多buff的时候卡牌画面会很混乱。
         * 不需要显示的return-1
         */

        switch (con)
        {
            // 眩晕
            case CardState.Cons.Stunned:
                return 501;
            // 护盾
            case CardState.Cons.Shield:
                return 508;
            // 无敌
            case CardState.Cons.Invincible:
                return 509;
            // 流血
            case CardState.Cons.Bleed:
                return 506;
            // 毒
            case CardState.Cons.Poison:
                return 504;
            // 灼烧
            case CardState.Cons.Burn:
                return 505;
            // 武魂
            case CardState.Cons.BattleSoul:
                return 516;
            // 战意
            case CardState.Cons.Stimulate:
                return -1;
            // 禁锢
            case CardState.Cons.Imprisoned:
                return 521;
            // 混乱
            case CardState.Cons.Confuse:
                return 50101;
            // 怯战
            case CardState.Cons.Cowardly:
                return 511;
            // 力量Up
            case CardState.Cons.StrengthUp:
                return -1;
            // 闪避Up
            case CardState.Cons.DodgeUp:
                return -1;
            // 暴击Up
            case CardState.Cons.CriticalUp:
                return -1;
            // 会心Up
            case CardState.Cons.RouseUp:
                return -1;
            // 护甲Up
            case CardState.Cons.ArmorUp:
                return -1;
            // 血战
            case CardState.Cons.DeathFight:
                return 510;
            // 卸甲
            case CardState.Cons.Disarmed:
                return 507;
            // 内助
            case CardState.Cons.Neizhu:
                return -1;
            //神助
            case CardState.Cons.ShenZhu:
                return -1;
            // 缓冲、抵消盾
            case CardState.Cons.EaseShield:
                return 512;
            // 迷雾
            case CardState.Cons.Forge://迷雾不应该是卡牌上的状态
                return -1;
            // 杀气
            case CardState.Cons.Murderous:
                return -1;
            // 连环
            case CardState.Cons.Chained:
                return 515;
            // 黄巾
            case CardState.Cons.YellowBand:
                return 074;
            default:
                return -1;//返回-1表示没有buff特效
        }
    }
    #endregion
    #region 地块Buff状态

    /// <summary>
    /// 获取卡牌buffId，返回-1表示没有buff。
    /// 目前类型：
    /// </summary>
    /// <param name="id">
    /// 参考 <see cref="PosSprite"/> 常量
    /// </param>
    /// <returns></returns>
    public static int GetFloorBuffId(int id)
    {
        /*
         * FloorBuff(地块状态)与buff差别在于一个在地块，一个在卡牌上。
         * FloorBuff不会随着卡牌移动(攻击动作等),并且它并不由 CardState.Cons 这个枚举声明,
         * 它的类型id暂时并不明确，但已用PosSprite(地块精灵这个类)的常量规范了。
         * todo:注意：只要是针对地块，无论是Spark或是Buff都用FloorBuff控制,
         * 由于地块上的特效数量比较少，所以就不分是Spark或Buff，因为代码可控制。
         * 而 落雷 的实现其实就只是让它的Animator不循环就可以实现了。
         */

        switch (id)
        {
            case PosSprite.Forge: return 522;//迷雾
            case PosSprite.YeHuo: return 50501;//爆炸+火焰
            case PosSprite.FireFlame:return 50502;//一般火焰
            case PosSprite.Thunder: return 200;//术士落雷
            case PosSprite.Eerthquake: return 166;//狂士地震
            default:
                return -1;//返回-1表示没有buff特效
        }
    }
    #endregion
}