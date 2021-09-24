public static class Effect
{
    public const string GetGold = "GetGold";
    public const string DropBlood = "dropBlood";
    public const string SpellTextH = "spellTextH";
    public const string SpellTextV = "spellTextV";
    
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

    public const string VTextDodge = "闪避";
    public const string VTextParry = "格挡";
    public const string VTextInvincible = "5";
    public const string VTextShield = "4";

    public static string GetHeroSpark(int military, int skill)
    {
        if (skill == 0) return Basic0A;//0 表示非武将技(普通攻击)
        var value = Basic0A; // "0A" 预设普通
        switch (military)
        {
            case 3: value = FeiJia3A; break; // "3A";飞甲
            case 1: //近战
            case 4: value = Shield4A; break; // "4A";大盾
            case 6: value = SuckBlood6A; break; // "6A";虎卫
            case 7: value = Blademail7A; break; // "7A";
            case 8: value = Elephant8A; break; // "8A";象兵
            case 9: value = Cavalry9A; break; // "9A";先锋
            case 10: value = Daredevil10A; break; // "10A";先登
            case 11: value = Knight11A; break; // "11A";白马
            case 12: value = Stimulate12A; break; // "12A";神武
            case 13: value = Guard13A; break; // "13A";禁卫
            case 15: value = Halberd15A; break; // "15A";大戟
            case 14: value = LongSpear14A; break;//"14A"长枪兵
            case 16: value = Cavalry16A; break; // "16A";骠骑
            case 17: value = Blade17A; break; // "17A";大刀
            case 18: value = Axe18A; break; // "18A";大斧
            case 19: value = CrossBow19A; break; // "19A";连弩
            case 20: value = Bow20A; break;// "20A";弓兵
            case 21: value = Warship21A; break; // "21A";战船
            case 22: value = Chariot22A; break; // "22A";战车
            case 23: value = SiegeMachine23A; break; // "23A";攻城车
            case 24: value = ThrowRocks24A; break; // "24A";投石车
            case 25: value = Assassin25A; break; // "25A";刺客
            case 26: value = Advisor26A; break;// "26A"  军师
            case 27: value = Advisor26A; break;// "27A"  大军师
            case 28:
            case 29:
                {
                    switch (skill)
                    {
                        case 1: value = Warlock29A; break; // "29A"  大术士
                        case 2: value = Warlock28A; break; // "28A"  术士
                    }
                    break;
                }
            case 30: value = PoisonMaster30A; break; // "31A"  大毒士
            case 31: value = PoisonMaster31A; break; // "30A"  毒士
            case 32: value = FlagBearer32A; break;// "32A"  统帅
            case 33: value = FlagBearer33A; break;// "33A"  大统帅
            case 34: value = Debate34A; break; // "34A";辩士
            case 35: value = Controversy35A; break; // "35A";大辩士
            case 36: value = Counselor36A; break;//"36"	谋士
            case 37: value = Counselor37A; break;//"37"  大谋士
            case 38: value = StateAffairs38A; break; // "38A";
            case 39: value = Support39A; break; // "39A";辅佐
            case 40: value = Mechanical40A; break; // "40A";器械
            case 42: value = Heal42A; break; // "42A";医师
            case 43: value = Heal43A; break; // "43A";大医师
            case 44: value = FemaleRider44A; break; // "44A";巾帼
            case 45: value = Lady45A; break;//"45"	美人
            case 46: value = Lady46A; break;//"46"  大美人
            case 47: value = Persuade47A; break; // "47A";说客
            case 48: value = Convince48A; break; // "48A";大说客
            case 49: value = Crossbow49A; break; // "49A";弩兵
            case 50: value = Scribe50A; break; // "50A";文士
            case 51: value = CrossBow51A; break;// "51A";强弩
            case 52: value = LongBow52A; break;// "52A";大弓
            case 53: value = Anchorite53A; break;//"53"	隐士
            case 54: value = Anchorite54A; break;//"54"  大隐士
            case 55:
                switch (skill)
                {
                    case 1: value = FireShip55A; break;
                    case 2: value = FireShip55A0; break;
                }
                break;
            case 56: value = Barbarians56A; break; // "56A";蛮族
            case 57: value = TengJia57A; break; // "57A";藤甲
            case 58: value = HeavyCavalry58A; break; // "58A";铁骑
            case 59: value = Spear59A; break;//"59A"枪兵
            case 60: value = Cavalry60A; break; // "60A";急先锋
            case 65:
                switch (skill)
                {
                    case 1:
                        value = YellowBand65A;
                        break; // "65A";黄巾
                               //todo 65B 换去状态文件夹
                               //case 2:
                               //    value = Effect.YellowBand65B;
                               //    break; // "65B";黄巾
                }
                break;
        }
        return value;
    }

    public const int Basic001 = 001;
    public static int GetHeroSparkId(int military, int skill)
    {
        if (skill == 0) return Basic001;//0 表示非武将技(普通攻击)
        var value = Basic001; // "0A" 预设普通
        switch (military)
        {
            case 3: value = FeiJia3A; break; // "3A";飞甲
            case 1: //近战
            case 4: value = Shield4A; break; // "4A";大盾
            case 6: value = SuckBlood6A; break; // "6A";虎卫
            case 7: value = Blademail7A; break; // "7A";
            case 8: value = Elephant8A; break; // "8A";象兵
            case 9: value = Cavalry9A; break; // "9A";先锋
            case 10: value = Daredevil10A; break; // "10A";先登
            case 11: value = Knight11A; break; // "11A";白马
            case 12: value = Stimulate12A; break; // "12A";神武
            case 13: value = Guard13A; break; // "13A";禁卫
            case 15: value = Halberd15A; break; // "15A";大戟
            case 14: value = LongSpear14A; break;//"14A"长枪兵
            case 16: value = Cavalry16A; break; // "16A";骠骑
            case 17: value = Blade17A; break; // "17A";大刀
            case 18: value = Axe18A; break; // "18A";大斧
            case 19: value = CrossBow19A; break; // "19A";连弩
            case 20: value = Bow20A; break;// "20A";弓兵
            case 21: value = Warship21A; break; // "21A";战船
            case 22: value = Chariot22A; break; // "22A";战车
            case 23: value = SiegeMachine23A; break; // "23A";攻城车
            case 24: value = ThrowRocks24A; break; // "24A";投石车
            case 25: value = Assassin25A; break; // "25A";刺客
            case 26: value = Advisor26A; break;// "26A"  军师
            case 27: value = Advisor26A; break;// "27A"  大军师
            case 28:
            case 29:
                {
                    switch (skill)
                    {
                        case 1: value = Warlock29A; break; // "29A"  大术士
                        case 2: value = Warlock28A; break; // "28A"  术士
                    }
                    break;
                }
            case 30: value = PoisonMaster30A; break; // "31A"  大毒士
            case 31: value = PoisonMaster31A; break; // "30A"  毒士
            case 32: value = FlagBearer32A; break;// "32A"  统帅
            case 33: value = FlagBearer33A; break;// "33A"  大统帅
            case 34: value = Debate34A; break; // "34A";辩士
            case 35: value = Controversy35A; break; // "35A";大辩士
            case 36: value = Counselor36A; break;//"36"	谋士
            case 37: value = Counselor37A; break;//"37"  大谋士
            case 38: value = StateAffairs38A; break; // "38A";
            case 39: value = Support39A; break; // "39A";辅佐
            case 40: value = Mechanical40A; break; // "40A";器械
            case 42: value = Heal42A; break; // "42A";医师
            case 43: value = Heal43A; break; // "43A";大医师
            case 44: value = FemaleRider44A; break; // "44A";巾帼
            case 45: value = Lady45A; break;//"45"	美人
            case 46: value = Lady46A; break;//"46"  大美人
            case 47: value = Persuade47A; break; // "47A";说客
            case 48: value = Convince48A; break; // "48A";大说客
            case 49: value = Crossbow49A; break; // "49A";弩兵
            case 50: value = Scribe50A; break; // "50A";文士
            case 51: value = CrossBow51A; break;// "51A";强弩
            case 52: value = LongBow52A; break;// "52A";大弓
            case 53: value = Anchorite53A; break;//"53"	隐士
            case 54: value = Anchorite54A; break;//"54"  大隐士
            case 55:
                switch (skill)
                {
                    case 1: value = FireShip55A; break;
                    case 2: value = FireShip55A0; break;
                }
                break;
            case 56: value = Barbarians56A; break; // "56A";蛮族
            case 57: value = TengJia57A; break; // "57A";藤甲
            case 58: value = HeavyCavalry58A; break; // "58A";铁骑
            case 59: value = Spear59A; break;//"59A"枪兵
            case 60: value = Cavalry60A; break; // "60A";急先锋
            case 65:
                switch (skill)
                {
                    case 1:
                        value = YellowBand65A;
                        break; // "65A";黄巾
                               //todo 65B 换去状态文件夹
                               //case 2:
                               //    value = Effect.YellowBand65B;
                               //    break; // "65B";黄巾
                }
                break;
        }
        return value;
    }
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
    public static int GetTowerSparkId(int towerId, int skill)
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
                return Basic001;
        }
    }
    //需要反向显示的特效
    public static bool IsInvertControl(string effectId)
    {
        switch (effectId)
        {
            case Spear59A:
            case LongSpear14A:
                return true;
            default: return false;
        }
    }
    //需要反向显示的特效
    public static bool IsInvertControl(int effectId)
    {
        switch (effectId)
        {
            default: return false;
        }
    }
}