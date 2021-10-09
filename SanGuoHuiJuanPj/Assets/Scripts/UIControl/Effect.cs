using System;
using Assets.System.WarModule;
using CorrelateLib;

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

            case 111:
            case 112:
            case 113://魔王
                value = 001;break;

            case 12:
            case 114:
            case 115://神武
                switch (skill) 
                {
                    case 1://破势
                        value = 013;break;
                    case 2://武魂
                        value = -1;break;
                }
                break;

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
                switch (skill) 
                {
                    case 1://普通攻击
                        value = 014; break;
                    case 2://反击
                        value = 014;break;
                }
                break;

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

            case 154:
            case 155:
            case 156://骁骑
                value = 018;break;

            case 157:
            case 158:
            case 159://彪骑
                value = 019;break;

            case 160:
            case 161:
            case 162://枪骑
                value = 01501;break;

            case 163:
            case 164:
            case 165://斧骑
                value = 03801;break;

            case 166:
            case 167:
            case 168://飞熊骑
                value = 179;break;

            case 169:
            case 170:
            case 171://匈奴骑
                value = 017;break;

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

            case 64:
            case 198:
            case 199://锦帆
                value = 079;break;

            case 200:
            case 201:
            case 202://蛟鳄军
                value = 049;break;

            case 26:
            case 27:
            case 203://军师
                value = 034;break;

            case 28:
            case 29:
            case 204://术士
                switch (skill ) 
                {
                    case 1://小技能
                        value = 200;break;
                    case 2://大技能
                        value = 200;break;
                }
                break;

            case 205:
            case 206:
            case 207://妖王
                value = 001;break;

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

            case 211:
            case 212:
            case 213://妖师
                value = 088;break;

            case 214:
            case 215:
            case 216://狂士
                switch (skill)
                {
                    case 1://小技能
                        value = 166; break;
                    case 2://大技能
                        value = 166; break;
                }
                break;

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

            case 223:
            case 224:
            case 225://方士
                value = 208;break;

            case 61:
            case 62:
            case 63://红颜
                value = 169;break;

            case 38:
            case 226:
            case 227://内政
                value = 091;break;

            case 228:
            case 229:
            case 230://天师
                value = 089;break;

            case 45:
            case 231:
            case 232://倾城
                value = 050;break;

            case 46:
            case 233:
            case 234://倾国
                value = 051;break;

            case 235:
            case 236:
            case 237://督军
                value = 053;break;

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

            case 245:
            case 246:
            case 247://修真
                value = 104;break;
        }
        return value;
    }

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

    #region VText
    /// <summary>
    /// 武将竖文字，null = 无
    /// </summary>
    /// <param name="military"></param>
    /// <param name="skill"></param>
    /// <returns></returns>
    public static int HeroActivityVText(int military, int skill)
    {
        if (skill == 0) return -1;
        switch (military)
        {

            case 0://武夫
                return -1;

            case 49://短弓
                return -1;

            case 50://文士
                return -1;

            case 1:
            case 66:
            case 67: //近战
                return 107;

            case 4:
            case 68:
            case 69://大盾
                switch (skill)
                {
                    case 1://持盾 回合开始前加盾
                        return 110;
                    case 2://战盾 攻击时加盾
                        return 111;
                }
                break;

            case 2:
            case 70:
            case 71://铁卫
                return -1;

            case 3:
            case 72:
            case 73://飞甲
                return -1;

            case 6:
            case 74:
            case 75://虎卫
                switch (skill)
                {
                    case 1://攻击目标
                        return -1 ;
                    case 2://自身加血
                        return 201;
                }
                break;


            case 7:
            case 76:
            case 77://刺盾
                return 202;

            case 78:
            case 79:
            case 80://血衣
                return -1;

            case 5:
            case 81:
            case 82://陷阵
                return 112;//自身加盾

            case 41:
            case 83:
            case 84://敢死
                switch (skill)
                {
                    case 1://自身加盾
                        return 223;
                    case 2://自身加血
                        return -1;
                }
                break;

            case 10:
            case 85:
            case 86://先登
                switch (skill)
                {
                    case 1://普通
                        return -1;
                    case 2://血祭
                        return 205;
                }
                break;

            case 87:
            case 88:
            case 89://青州
                switch (skill)
                {
                    case 1://普通
                        return -1;
                    case 2://卸甲归田
                        return -1;
                }
                break;

            case 90:
            case 91:
            case 92://链锁
                switch (skill)
                {
                    case 1://普通
                        return -1;
                    case 2://捆缚
                        return -1;
                }
                break;

            case 93:
            case 94:
            case 95://解烦
                switch (skill)
                {
                    case 1://引燃
                        return -1;
                    case 2://自爆
                        return -1;
                }
                break;

            case 57:
            case 96:
            case 97://藤甲
                return -1;

            case 98:
            case 99:
            case 100://鬼兵
                switch (skill)
                {
                    case 1://普通
                        return -1;
                    case 2://还魂
                        return -1;
                    case 3://借尸
                        return -1;
                }
                break;

            case 59:
            case 14:
            case 101://长枪
                return 210;

            case 15:
            case 102:
            case 103://大戟
                return 211;

            case 17:
            case 104:
            case 105://大刀
                return 213;

            case 18:
            case 106:
            case 107://大斧
                return 214;

            case 108:
            case 109:
            case 110://狼牙棒
                return -1;

            case 111:
            case 112:
            case 113://魔王
                return -1;

            case 12:
            case 114:
            case 115://神武
                switch (skill) 
                {
                    case 1://破势
                        return 208;
                    case 2://武魂
                        return 207;
                }
                break;

            case 116:
            case 117:
            case 118://白毦
                return -1;

            case 119:
            case 120:
            case 121://朴刀
                return -1;

            case 13:
            case 122:
            case 123://羽林
                switch (skill)
                {
                    case 1://普通攻击
                        return -1;
                    case 2://反击
                        return 209;
                }
                break;

            case 124:
            case 125:
            case 126://双戟
                return -1;

            case 65:
            case 127:
            case 128://黄巾
                return 231;//群起攻击

            case 25:
            case 129:
            case 130://刺客
                switch (skill)
                {
                    case 1://攻击建筑或陷阱
                        return -1;
                    case 2://攻击武将士兵
                        return 221;
                }
                break;

            case 56:
            case 131:
            case 132://蛮族
                switch (skill)
                {
                    case 1://攻击建筑或陷阱
                        return -1;
                    case 2://攻击武将士兵
                        return 228;
                }
                break;

            case 133:
            case 134:
            case 135://丹阳
                return -1;

            case 9:
            case 60:
            case 136://飞骑
                return 204;

            case 137:
            case 138:
            case 139://白马
                return 255;

            case 11:
            case 140:
            case 141://虎豹骑
                return 206;

            case 16:
            case 142:
            case 143://骠骑
                return 212;

            case 44:
            case 144:
            case 145://巾帼
                switch (skill)
                {
                    case 1://攻击建筑或陷阱
                        return -1;
                    case 2://攻击武将士兵
                        return 225;
                }
                break;

            case 146:
            case 147:
            case 148://弓骑
                return -1;

            case 149:
            case 150:
            case 151://狼骑
                return -1;

            case 58:
            case 152:
            case 153://铁骑
                return 229;

            case 154:
            case 155:
            case 156://骁骑
                return 116;

            case 157:
            case 158:
            case 159://彪骑
                return 245;

            case 160:
            case 161:
            case 162://枪骑
                return 246;

            case 163:
            case 164:
            case 165://斧骑
                return 247;

            case 166:
            case 167:
            case 168://飞熊骑
                return 248;

            case 169:
            case 170:
            case 171://匈奴骑
                return 249;

            case 22:
            case 172:
            case 173://战车
                return 218;

            case 8:
            case 174:
            case 175://战象
                switch (skill)
                {
                    case 1://普通攻击
                        return 203;
                    case 2://暴击或会心
                        return 263;
                }
                break;

            case 23:
            case 176:
            case 177://攻城车
                return 219;//攻击建筑或陷阱

            case 24:
            case 178:
            case 179://投石车
                return 220;

            case 19:
            case 51:
            case 180://连弩
                return 215;

            case 181:
            case 182:
            case 183://元戎弩
                return 250;

            case 20:
            case 52:
            case 184://大弓
                return 216;

            case 185:
            case 186:
            case 187://火弓
                return 251;

            case 188:
            case 189:
            case 190://重弩
                return 252;

            case 191:
            case 192:
            case 193://无当
                return 253;

            case 21:
            case 194:
            case 195://艨艟
                return 217;

            case 55:
            case 196:
            case 197://火船
                switch (skill)
                {
                    case 1://引燃
                        return 226;
                    case 2://自爆
                        return 227;
                }
                break;

            case 64:
            case 198:
            case 199://锦帆
                return 230;

            case 200:
            case 201:
            case 202://蛟鳄军
                return 254;

            case 26:
            case 27:
            case 203://军师
                return 301;

            case 28:
            case 29:
            case 204://术士
                switch (skill ) 
                {
                    case 1://小技能
                        return 302;
                    case 2://大技能
                        return 302;
                }
                break;

            case 205:
            case 206:
            case 207://妖王
                return -1;

            case 30:
            case 31:
            case 208://毒士
                return 303;

            case 32:
            case 33:
            case 209://统帅
                return 222;

            case 53:
            case 54:
            case 210://隐士
                return 307;

            case 211:
            case 212:
            case 213://妖师
                return  310;

            case 214:
            case 215:
            case 216://狂士
                switch (skill)
                {
                    case 1://小技能
                        return 311;
                    case 2://大技能
                        return 311;
                }
                break;

            case 36:
            case 37:
            case 217://谋士
                return 305;

            case 218:
            case 219:
            case 220://巫祝
                return 312;

            case 34:
            case 35:
            case 221://辩士
                return 304;

            case 47:
            case 48:
            case 222://说客
                return  306;

            case 223:
            case 224:
            case 225://方士
                return 313;

            case 61:
            case 62:
            case 63://红颜
                return 503;

            case 38:
            case 226:
            case 227://内政
                return 401;

            case 228:
            case 229:
            case 230://天师
                return 314;

            case 45:
            case 231:
            case 232://倾城
                return 501;

            case 46:
            case 233:
            case 234://倾国
                return 502;

            case 235:
            case 236:
            case 237://督军
                return 406;

            case 39:
            case 238:
            case 239://辅佐
                return 402;

            case 40:
            case 240:
            case 241://器械
                return 403;

            case 42:
            case 43:
            case 242://医士
                return 404;

            case 243://壮士
                return -1;

            case 244://短弩
                return -1;

            case 245:
            case 246:
            case 247://修真
                return 316;
        }
        return -1;
    }
    /// <summary>
    /// 塔竖文字，null = 无
    /// </summary>
    /// <param name="towerId"></param>
    /// <param name="skill"></param>
    /// <returns></returns>
    public static int TowerActivityVText(int towerId, int skill)
    {
        if (skill == 0) return -1;
        switch (towerId)
        {
            //营寨
            case 0:
                switch (skill ) 
                {
                    case 1://补给
                        return 408;
                    case 2://回合开始前自身恢复
                        return 409;             
                }
                break;

            //投石台
            case 1:
                return 257;

            //奏乐台
            case 2:
                return 407;

            //箭楼
            case 3:
                return 256;

            //战鼓台
            case 4:
                break;

            //风神台
            case 5:
                break;

            //轩辕台
            case 6:
                return 118;

            //铸铁炉
            case 7:
                break;

            //四方鼎
            case 8:
                break;

            //烽火台
            case 9:
                break;

            //号角台
            case 10:
                break;

            //瞭望塔
            case 11:
                break;

            //七星坛
            case 12:
                break;

            //演武场
            case 13:
                break;

            //曹魏旗
            case 14:
                break;

            //蜀汉旗
            case 15:
                break;

            //东吴旗
            case 16:
                break;

            //迷雾阵
            case 18:

            //迷雾阵 
            case 17:
                break;

            //骑兵营
            case 19:
                break;

            //弓弩营
            case 20:
                break;

            //步兵营
            case 21:
                break;

            //长持营
            case 22:
                break;

            //战船营
            case 23:
                break;
        }

        return -1;
    }
    /// <summary>
    /// 陷阱竖文字，null = 无
    /// </summary>
    /// <param name="trapId"></param>
    /// <param name="skill"></param>
    /// <returns></returns>
    public static int TrapActivityVText(int trapId, int skill)
    {
        if (skill == 0) return -1;
        switch (trapId)
        {
            case 0://拒马
                return 262;

            case 1://地雷
                return 259;

            case 2://石墙
                return -1;

            case 3://八阵图
                return -1;

            case 4://金锁阵
                return -1;

            case 5://鬼兵阵
                return -1;

            case 6://火墙
                return -1;

            case 7://毒潭
                return -1;

            case 8://刀墙
                return -1;

            case 9://滚石
                return 260;

            case 10://滚木
                return 261;
        }

        return -1;
    }

    public static int ActivityResultVText(ActivityResult result)
    {
        switch (result.Type)
        {
            case ActivityResult.Types.ChessPos://地块结果，一般为释放精灵
                break;

            case ActivityResult.Types.Suffer://承受结果
                break;

            case ActivityResult.Types.Dodge://闪避结果
                return 102;

            case ActivityResult.Types.Friendly://友军
                break;

            case ActivityResult.Types.Shield://护盾
                return 103;

            case ActivityResult.Types.Invincible://无敌
                return 101;

            case ActivityResult.Types.EaseShield://抵消盾
                return -1;

            case ActivityResult.Types.Kill://击杀
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return -1;
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
            //铁蹄

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
            //铁蹄

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

    #region 音效
    /// <summary>
    /// 武将音效, -1 = 没音效
    /// </summary>
    /// <param name="military"></param>
    /// <param name="skill"></param>
    /// <returns></returns>
    public static int GetHeroAudioId(int military,int skill)
    {
        var audioId = -1;
        if (skill == 0) return 0;//普通攻击音效为 0
        switch (military)
        {
            case 0://武夫
                audioId = 0; break;

            case 49://短弓
                audioId = 0; break;

            case 50://文士
                audioId = 0; break;

            case 1:
            case 66:
            case 67: //近战
                audioId = 61; break;

            case 4:
            case 68:
            case 69://大盾
                switch (skill)
                {
                    case 1://持盾 回合开始前加盾
                        audioId = 47; break;
                    case 2://战盾 攻击时加盾
                        audioId = 47; break;
                }
                break;

            case 2:
            case 70:
            case 71://铁卫
                audioId = 0; break;

            case 3:
            case 72:
            case 73://飞甲
                audioId = 11; break;

            case 6:
            case 74:
            case 75://虎卫
                switch (skill)
                {
                    case 1://攻击目标
                        audioId = 9; break;
                    case 2://自身加血
                        audioId = 57; break;
                }
                break;

            case 7:
            case 76:
            case 77://刺盾
                audioId = 24; break;

            case 78:
            case 79:
            case 80://血衣
                audioId = 0; break;

            case 5:
            case 81:
            case 82://陷阵
                audioId = 47; break;//自身加盾

            case 41:
            case 83:
            case 84://敢死
                switch (skill)
                {
                    case 1://自身加盾
                        audioId = 47; break;
                    case 2://自身加血
                        audioId = 57; break;
                }
                break;

            case 10:
            case 85:
            case 86://先登
                switch (skill)
                {
                    case 1://普通
                        audioId = 0; break;
                    case 2://血祭
                        audioId = 18; break;
                }
                break;

            case 87:
            case 88:
            case 89://青州
                switch (skill)
                {
                    case 1://普通
                        audioId = -1; break;
                    case 2://卸甲归田
                        audioId = -1; break;
                }
                break;

            case 90:
            case 91:
            case 92://链锁
                switch (skill)
                {
                    case 1://普通
                        audioId = -1; break;
                    case 2://捆缚
                        audioId = -1; break;
                }
                break;

            case 93:
            case 94:
            case 95://解烦
                switch (skill)
                {
                    case 1://引燃
                        audioId = -1; break;
                    case 2://自爆
                        audioId = -1; break;
                }
                break;

            case 57:
            case 96:
            case 97://藤甲
                audioId = 0; break;

            case 98:
            case 99:
            case 100://鬼兵
                switch (skill)
                {
                    case 1://普通
                        audioId = -1; break;
                    case 2://还魂
                        audioId = -1; break;
                    case 3://借尸
                        audioId = -1; break;
                }
                break;

            case 59:
            case 14:
            case 101://长枪
                audioId = 53; break;

            case 15:
            case 102:
            case 103://大戟
                audioId = 7; break;

            case 17:
            case 104:
            case 105://大刀
                audioId = 2; break;

            case 18:
            case 106:
            case 107://大斧
                audioId = 19; break;

            case 108:
            case 109:
            case 110://狼牙棒
                audioId = -1; break;

            case 111:
            case 112:
            case 113://魔王
                audioId = -1; break;

            case 12:
            case 114:
            case 115://神武
                switch (skill) 
                {
                    case 1://破势
                        audioId = 4;break;
                    case 2://武魂
                        audioId = 57;break;
                }
                break;

            case 116:
            case 117:
            case 118://白毦
                audioId = 0; break;

            case 119:
            case 120:
            case 121://朴刀
                audioId = 0; break;

            case 13:
            case 122:
            case 123://羽林
                switch (skill)
                {
                    case 1://普通攻击
                        audioId = 22; break;
                    case 2://反击
                        audioId = 22; break;
                }
                break;

            case 124:
            case 125:
            case 126://双戟
                audioId = 0; break;

            case 65:
            case 127:
            case 128://黄巾
                audioId = 33; break;//群起攻击

            case 25:
            case 129:
            case 130://刺客
                switch (skill)
                {
                    case 1://攻击建筑或陷阱
                        audioId = 0; break;
                    case 2://攻击武将士兵
                        audioId = 12; break;
                }
                break;

            case 56:
            case 131:
            case 132://蛮族
                switch (skill)
                {
                    case 1://攻击建筑或陷阱
                        audioId = 0; break;
                    case 2://攻击武将士兵
                        audioId = 54; break;
                }
                break;

            case 133:
            case 134:
            case 135://丹阳
                audioId = 0; break;

            case 9:
            case 60:
            case 136://飞骑
                audioId = 29; break;

            case 137:
            case 138:
            case 139://白马
                audioId = 28; break;

            case 11:
            case 140:
            case 141://虎豹骑
                audioId = 9; break;

            case 16:
            case 142:
            case 143://骠骑
                audioId = 27; break;

            case 44:
            case 144:
            case 145://巾帼
                switch (skill)
                {
                    case 1://攻击建筑或陷阱
                        audioId = 0; break;
                    case 2://攻击武将士兵
                        audioId = 30; break;
                }
                break;

            case 146:
            case 147:
            case 148://弓骑
                audioId = 0; break;

            case 149:
            case 150:
            case 151://狼骑
                audioId = 0; break;

            case 58:
            case 152:
            case 153://铁骑
                audioId = 27; break;

            case 154:
            case 155:
            case 156://骁骑
                audioId = 28; break;

            case 157:
            case 158:
            case 159://彪骑
                audioId = 29; break;

            case 160:
            case 161:
            case 162://枪骑
                audioId = 27; break;

            case 163:
            case 164:
            case 165://斧骑
                audioId = 28; break;

            case 166:
            case 167:
            case 168://飞熊骑
                audioId = 29; break;

            case 169:
            case 170:
            case 171://匈奴骑
                audioId = 27; break;

            case 22:
            case 172:
            case 173://战车
                audioId = 15; break;

            case 8:
            case 174:
            case 175://战象
                switch (skill)
                {
                    case 1://普通攻击
                        audioId = 8; break;
                    case 2://暴击或会心
                        audioId = 8; break;
                }
                break;

            case 23:
            case 176:
            case 177://攻城车
                audioId = 15; break;//攻击建筑或陷阱

            case 24:
            case 178:
            case 179://投石车
                audioId = 31; break;

            case 19:
            case 51:
            case 180://连弩
                audioId = 25; break;

            case 181:
            case 182:
            case 183://元戎弩
                audioId = 25; break;

            case 20:
            case 52:
            case 184://大弓
                audioId = 26; break;

            case 185:
            case 186:
            case 187://火弓
                audioId = 26; break;

            case 188:
            case 189:
            case 190://重弩
                audioId = 25; break;

            case 191:
            case 192:
            case 193://无当
                audioId = 26; break;

            case 21:
            case 194:
            case 195://艨艟
                audioId = 36; break;

            case 55:
            case 196:
            case 197://火船
                switch (skill)
                {
                    case 1://引燃
                        audioId = 16; break;
                    case 2://自爆
                        audioId = 38; break;
                }
                break;

            case 64:
            case 198:
            case 199://锦帆
                audioId = 0; break;

            case 200:
            case 201:
            case 202://蛟鳄军
                audioId = 0; break;

            case 26:
            case 27:
            case 203://军师
                audioId = 39; break;

            case 28:
            case 29:
            case 204://术士
                switch (skill)
                {
                    case 1://小技能
                        audioId = 43; break;
                    case 2://大技能
                        audioId = 42; break;
                }
                break;

            case 205:
            case 206:
            case 207://妖王
                audioId = 0; break;

            case 30:
            case 31:
            case 208://毒士
                audioId = 46; break;

            case 32:
            case 33:
            case 209://统帅
                audioId = 38;break;

            case 53:
            case 54:
            case 210://隐士
                audioId = 13; break;

            case 211:
            case 212:
            case 213://妖师
                audioId = 46; break;

            case 214:
            case 215:
            case 216://狂士
                switch (skill)
                {
                    case 1://小技能
                        audioId = 43; break;
                    case 2://大技能
                        audioId = 42; break;
                }
                break;

            case 36:
            case 37:
            case 217://谋士
                audioId = 43; break;

            case 218:
            case 219:
            case 220://巫祝
                audioId = 0; break;

            case 34:
            case 35:
            case 221://辩士
                audioId = 44; break;

            case 47:
            case 48:
            case 222://说客
                audioId = 45; break;

            case 223:
            case 224:
            case 225://方士
                audioId = 0; break;

            case 61:
            case 62:
            case 63://红颜
                audioId = 0; break;

            case 38:
            case 226:
            case 227://内政
                audioId = 57; break;

            case 228:
            case 229:
            case 230://天师
                audioId = 0; break;

            case 45:
            case 231:
            case 232://倾城
                audioId = 20; break;

            case 46:
            case 233:
            case 234://倾国
                audioId = 40; break;

            case 235:
            case 236:
            case 237://督军
                audioId = 0; break;

            case 39:
            case 238:
            case 239://辅佐
                audioId = 50; break;

            case 40:
            case 240:
            case 241://器械
                audioId = 52; break;

            case 42:
            case 43:
            case 242://医士
                audioId = 56; break;

            case 243://壮士
                audioId = 0; break;

            case 244://短弩
                audioId = 25; break;

            case 245:
            case 246:
            case 247://修真
                audioId = 0; break;
        }

        return audioId;
    }
    /// <summary>
    /// 塔音效, -1 = 没音效
    /// </summary>
    /// <param name="towerId"></param>
    /// <returns></returns>
    public static int GetTowerAudioId(int towerId,int skill)
    {
        switch (towerId)
        {
            case 0: //营寨
                switch (skill) 
                {
                    case 1://补给
                        return 56;
                    case 2://自身恢复
                        return 57;
                }break;

            case 1: //投石台
                return 31;

            case 2: //奏乐台
                return 56;

            case 3: //箭楼
                return 26;

            case 6: //轩辕台
                return 47;
        }
        return -1;
    }
    /// <summary>
    /// 陷阱音效, -1 = 没音效
    /// </summary>
    /// <param name="trapId"></param>
    /// <returns></returns>
    public static int GetTrapAudioId(int trapId)
    {
        switch (trapId)
        {
            case 0://拒马
                return 24;

            case 1://地雷
                return 38;

            case 2://石墙
            case 3://八阵图
            case 4://金锁阵
            case 5://鬼兵阵
            case 6://火墙
            case 7://毒泉
            case 8://刀墙
                break;

            case 9://滚石
                return 48;
            case 10://滚木
                return 48;

            case 11://金币宝箱
            case 12://宝箱
                return  98;
        }
        return -1;
    }

    /// <summary>
    /// 资源音效
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    public static int GetPlayerResourceEffectId(int element)
    {
        if (element == -1) return 98; //金币
        if (element >= 0) return 98; //战役宝箱
        throw new ArgumentOutOfRangeException(nameof(element), element.ToString());
    }

    /// <summary>
    /// 精灵附buff音效
    /// </summary>
    /// <param name="con"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static int GetBuffingAudioId(CardState.Cons con)
    {
        switch (con)
        {
            case CardState.Cons.Burn:
                return 17;
            case CardState.Cons.Poison:
                return 17;
            case CardState.Cons.Bleed:
                return 17;
            case CardState.Cons.Stunned:
            case CardState.Cons.Shield:
            case CardState.Cons.Invincible:
            case CardState.Cons.BattleSoul:
            case CardState.Cons.Imprisoned:
            case CardState.Cons.Cowardly:
            case CardState.Cons.StrengthUp:
            case CardState.Cons.DodgeUp:
            case CardState.Cons.CriticalUp:
            case CardState.Cons.RouseUp:
            case CardState.Cons.ArmorUp:
            case CardState.Cons.DeathFight:
            case CardState.Cons.Disarmed:
            case CardState.Cons.Neizhu:
            case CardState.Cons.ShenZhu:
            case CardState.Cons.EaseShield:
            case CardState.Cons.Forge:
            case CardState.Cons.Stimulate:
            case CardState.Cons.Confuse:
            case CardState.Cons.YellowBand:
            case CardState.Cons.Chained:
            case CardState.Cons.Murderous:
            default:
                throw new ArgumentOutOfRangeException(nameof(con), con, null);
        }
    }

    /// <summary>
    /// 不可避免类型，buff伤
    /// </summary>
    /// <param name="con"></param>
    /// <returns></returns>
    public static int GetInevitableAudioId(CardState.Cons con)
    {
        switch (con)
        {
            case CardState.Cons.Poison://毒buff伤害
                return 17;
            case CardState.Cons.Burn://火buff伤害
                return 37;
            case CardState.Cons.Bleed:
            case CardState.Cons.Stunned:
            case CardState.Cons.Shield:
            case CardState.Cons.Invincible:
            case CardState.Cons.BattleSoul:
            case CardState.Cons.Imprisoned:
            case CardState.Cons.Cowardly:
            case CardState.Cons.StrengthUp:
            case CardState.Cons.DodgeUp:
            case CardState.Cons.CriticalUp:
            case CardState.Cons.RouseUp:
            case CardState.Cons.ArmorUp:
            case CardState.Cons.DeathFight:
            case CardState.Cons.Disarmed:
            case CardState.Cons.Neizhu:
            case CardState.Cons.ShenZhu:
            case CardState.Cons.EaseShield:
            case CardState.Cons.Forge:
            case CardState.Cons.Stimulate:
            case CardState.Cons.Confuse:
            case CardState.Cons.YellowBand:
            case CardState.Cons.Chained:
            case CardState.Cons.Murderous:
            default:
                throw new ArgumentOutOfRangeException(nameof(con), con, null);
        }
    }

    /// <summary>
    /// 活动结果音效
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static int ResultAudioId(ActivityResult.Types type)
    {
        switch (type)
        {
            case ActivityResult.Types.ChessPos:
            case ActivityResult.Types.Suffer:
            case ActivityResult.Types.Friendly:
            case ActivityResult.Types.Kill:
            case ActivityResult.Types.EaseShield:
                return -1;//这些结果都是直接播放兵种音效

            case ActivityResult.Types.Dodge:
                return 5;
            case ActivityResult.Types.Shield:
            case ActivityResult.Types.Invincible:
                return 51;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    #endregion
}