using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CorrelateLib;
using UnityEngine;

public enum ForceFlags
{
    刘 = 0,
    曹 = 1,
    孙 = 2,
    袁 = 3,
    吕 = 4,
    司马 = 5,
    其它 = 6,
}

public class GameResources
{
    public static GameResources Instance
    {
        get
        {
            if (_instance != null)
                return _instance;
            _instance = new GameResources();
            _instance.Init();
            return _instance;
        }
    }

    private const string HeroImagesPath = "Image/Cards/Hero/";
    private const string ClassImagesPath = "Image/classImage/";
    private const string FuZhuImagesPath = "Image/Cards/FuZhu/";
    private const string GradeImagesPath = "Image/gradeImage/";
    private const string GuanQiaEventImagesPath = "Image/guanQiaEvents/";
    private const string FrameImagesPath = "Image/frameImage/";
    private const string ArtWindowImagesPath = "Image/ArtWindow/";
    private const string BattleBgImagesPath = "Image/battleBG/";
    private const string EffectsGameObjectPath = "Prefabs/Effects/";
    private const string StateDinPath = "Prefabs/stateDin/";
    private const string ForceFlagsPath = "Image/shiLi/Flag";
    private const string ForceNamePath = "Image/shiLi/Name";
    private const string CityIconPath = "Image/City/Icon";
    private const string AvatarPath = "Image/Player/Icon";
    private const string JiBanBgPath = "Image/JiBan/art";
    private const string JiBanVTextPath = "Image/JiBan/name_v";
    private const string JiBanHTextPath = "Image/JiBan/name_h";
    private const string VTextPath = "Image/BattleTJ";
    private const string BuffPath = "Prefabs/WarEffect/Buff";
    private const string FloorBuffPath = "Prefabs/WarEffect/FloorBuff";
    private const string IconPath = "Prefabs/WarEffect/Icon";
    private const string SparkPath = "Prefabs/WarEffect/Spark";
    private const string TeamSparkPath = "Prefabs/WarEffect/TeamSpark";
    /// <summary>
    /// Key = heroId, Value = sprite
    /// </summary>
    public IReadOnlyDictionary<int, Sprite> HeroImg => heroImgMap;
    public IReadOnlyDictionary<int, Sprite> ClassImg => classImgMap;
    /// <summary>
    /// key = imgId, value = sprite
    /// </summary>
    public IReadOnlyDictionary<int, Sprite> FuZhuImg => fuZhuImgMap;
    public IReadOnlyDictionary<int, Sprite> GradeImg => gradeImgMap;
    public IReadOnlyDictionary<int, Sprite> GuanQiaEventImg => guanQiaEventMap;
    public IReadOnlyDictionary<int, Sprite> FrameImg => frameImgMap;
    public IReadOnlyDictionary<int, Sprite> ArtWindow => artWindowMap;
    public IReadOnlyDictionary<int, Sprite> BattleBG => battleBgMap;
    public IReadOnlyDictionary<ForceFlags, Sprite> ForceFlag => forceFlagMap;
    public IReadOnlyDictionary<ForceFlags, Sprite> ForceName => forceNameMap;
    public IReadOnlyDictionary<string, GameObject> Effects => effectsMap;
    public IReadOnlyDictionary<string, EffectStateUi> StateDin => stateDinMap;
    public IReadOnlyDictionary<int, Sprite> CityFlag => cityFlag;
    public IReadOnlyDictionary<int, Sprite> CityIcon => cityIcon;
    public IReadOnlyDictionary<int, Sprite> Avatar => avatar;
    public IReadOnlyDictionary<int, Sprite> JiBanBg => jiBanBg;
    public IReadOnlyDictionary<int, Sprite> JiBanVText => jiBanVText;
    public IReadOnlyDictionary<int, Sprite> JiBanHText => jiBanHText;
    public IReadOnlyDictionary<int, Sprite> VText => vText;
    public IReadOnlyDictionary<int, EffectStateUi> Buff => buffMap;
    public IReadOnlyDictionary<int, EffectStateUi> FloorBuff => floorBuffMap;
    public IReadOnlyDictionary<int, Sprite> Icon => iconMap;
    public IReadOnlyDictionary<int, GameObject> Spark => sparkMap;
    public IReadOnlyDictionary<int, GameObject> TeamSpark => teamSparkMap;

    private bool isInit;

    private IReadOnlyDictionary<int, Sprite> heroImgMap;
    private IReadOnlyDictionary<int, Sprite> classImgMap;
    private IReadOnlyDictionary<int, Sprite> fuZhuImgMap;
    private IReadOnlyDictionary<int, Sprite> gradeImgMap;
    private IReadOnlyDictionary<int, Sprite> guanQiaEventMap;
    private IReadOnlyDictionary<int, Sprite> frameImgMap;
    private IReadOnlyDictionary<int, Sprite> artWindowMap;
    private IReadOnlyDictionary<int, Sprite> battleBgMap;
    private IReadOnlyDictionary<ForceFlags, Sprite> forceFlagMap;
    private IReadOnlyDictionary<ForceFlags, Sprite> forceNameMap;
    private IReadOnlyDictionary<string, GameObject> effectsMap;
    private IReadOnlyDictionary<string, EffectStateUi> stateDinMap;
    private IReadOnlyDictionary<int, Sprite> cityFlag;
    private IReadOnlyDictionary<int, Sprite> cityIcon;
    private IReadOnlyDictionary<int, Sprite> avatar;
    private IReadOnlyDictionary<int, Sprite> jiBanBg;
    private IReadOnlyDictionary<int,Sprite> jiBanHText;
    private IReadOnlyDictionary<int,Sprite> jiBanVText;
    private IReadOnlyDictionary<int, Sprite> vText;
    private IReadOnlyDictionary<int, EffectStateUi> buffMap;
    private IReadOnlyDictionary<int, EffectStateUi> floorBuffMap;
    private IReadOnlyDictionary<int, Sprite> iconMap;
    private IReadOnlyDictionary<int, GameObject> sparkMap;
    private IReadOnlyDictionary<int, GameObject> teamSparkMap;
    private static GameResources _instance;

    public void Init(bool forceReload = false)
    {
        if (isInit && !forceReload) return;
        var heroSprites = Resources.LoadAll<Sprite>(HeroImagesPath).ToList();
        var heroIdImgMap = heroSprites
            .Select(sprite => new { imageId = int.Parse(sprite.name), sprite })
            .Join(
                DataTable.Hero.Values, //英雄表 要融合的东西
                sprite => sprite.imageId, //列表元素对应 
                hero => hero.ImageId, //目标元素对应
                (sprite, hero) => new { hero.Id, sprite.sprite } //(图片,英雄表)-每一列
            )
            .OrderBy(c => c.Id)
            .ToDictionary(c => c.Id, c => c.sprite);
        heroImgMap = new ResourceDataWrapper<int, Sprite>(heroIdImgMap, nameof(heroImgMap));
        classImgMap = new ResourceDataWrapper<int, Sprite>(
            Resources.LoadAll<Sprite>(ClassImagesPath)
                .ToDictionary(s => int.Parse(s.name), s => s),
            nameof(classImgMap));
        fuZhuImgMap = new ResourceDataWrapper<int, Sprite>(
            Resources.LoadAll<Sprite>(FuZhuImagesPath).ToDictionary(s => int.Parse(s.name), s => s),
            nameof(fuZhuImgMap));
        gradeImgMap = new ResourceDataWrapper<int, Sprite>(
            Resources.LoadAll<Sprite>(GradeImagesPath).ToDictionary(s => int.Parse(s.name), s => s),
            nameof(gradeImgMap));

        guanQiaEventMap = new ResourceDataWrapper<int, Sprite>(
            Resources.LoadAll<Sprite>(GuanQiaEventImagesPath)
                .Where(s => int.TryParse(s.name, out _))
                .ToDictionary(s => int.Parse(s.name), s => s), nameof(guanQiaEventMap));

        frameImgMap = new ResourceDataWrapper<int, Sprite>(
            Resources.LoadAll<Sprite>(FrameImagesPath).ToDictionary(s => int.Parse(s.name), s => s),
            nameof(frameImgMap));
        artWindowMap = new ResourceDataWrapper<int, Sprite>(
            Resources.LoadAll<Sprite>(ArtWindowImagesPath).ToDictionary(s => int.Parse(s.name), s => s),
            nameof(artWindowMap));
        battleBgMap = new ResourceDataWrapper<int, Sprite>(
            Resources.LoadAll<Sprite>(BattleBgImagesPath).ToDictionary(s => int.Parse(s.name), s => s),
            nameof(battleBgMap));
        forceFlagMap = new ResourceDataWrapper<ForceFlags, Sprite>(
            Resources.LoadAll<Sprite>(ForceFlagsPath).ToDictionary(s => (ForceFlags) int.Parse(s.name), s => s),
            nameof(forceFlagMap));
        forceNameMap = new ResourceDataWrapper<ForceFlags, Sprite>(
            Resources.LoadAll<Sprite>(ForceNamePath).ToDictionary(s => (ForceFlags) int.Parse(s.name), s => s),
            nameof(forceNameMap));
        effectsMap = new ResourceDataWrapper<string, GameObject>(
            Resources.LoadAll<GameObject>(EffectsGameObjectPath).ToDictionary(g => g.name, g => g), nameof(effectsMap));
        stateDinMap = new ResourceDataWrapper<string, EffectStateUi>(
            Resources.LoadAll<EffectStateUi>(StateDinPath).ToDictionary(g => g.name, g => g), nameof(stateDinMap));
        cityFlag = new ResourceDataWrapper<int, Sprite>(Resources.LoadAll<Sprite>(ForceFlagsPath).ToDictionary(s => int.Parse(s.name), s => s), nameof(cityFlag));
        cityIcon = new ResourceDataWrapper<int, Sprite>(Resources.LoadAll<Sprite>(CityIconPath).ToDictionary(s => int.Parse(s.name), s => s), nameof(cityIcon));
        avatar = new ResourceDataWrapper<int, Sprite>(Resources.LoadAll<Sprite>(AvatarPath).ToDictionary(s => int.Parse(s.name), s => s), nameof(avatar));
        jiBanBg = new ResourceDataWrapper<int, Sprite>(Resources.LoadAll<Sprite>(JiBanBgPath).ToDictionary(s => int.Parse(s.name), s => s), nameof(jiBanBg));
        jiBanHText= new ResourceDataWrapper<int, Sprite>(Resources.LoadAll<Sprite>(JiBanHTextPath).ToDictionary(s => int.Parse(s.name), s => s), nameof(jiBanHText));
        jiBanVText= new ResourceDataWrapper<int, Sprite>(Resources.LoadAll<Sprite>(JiBanVTextPath).ToDictionary(s => int.Parse(s.name), s => s), nameof(jiBanVText));
        vText = new ResourceDataWrapper<int, Sprite>(Resources.LoadAll<Sprite>(VTextPath).ToDictionary(s => int.Parse(s.name), s => s), nameof(vText));
        buffMap = new ResourceDataWrapper<int, EffectStateUi>(Resources.LoadAll<EffectStateUi>(BuffPath).ToDictionary(s => int.Parse(s.name), s => s), nameof(buffMap));
        floorBuffMap = new ResourceDataWrapper<int, EffectStateUi>(Resources.LoadAll<EffectStateUi>(FloorBuffPath).ToDictionary(s => int.Parse(s.name), s => s), nameof(floorBuffMap));
        iconMap = new ResourceDataWrapper<int, Sprite>(Resources.LoadAll<Sprite>(IconPath).ToDictionary(s => int.Parse(s.name), s => s), nameof(iconMap));
        sparkMap = new ResourceDataWrapper<int, GameObject>(Resources.LoadAll<GameObject>(SparkPath).ToDictionary(s => int.Parse(s.name), s => s), nameof(sparkMap));
        teamSparkMap = new ResourceDataWrapper<int, GameObject>(Resources.LoadAll<GameObject>(TeamSparkPath).ToDictionary(s => int.Parse(s.name), s => s), nameof(teamSparkMap));
        isInit = true;
    }

    /// <summary>
    /// 调试字典。报错的时候会返回字典名字，和key，方便找bug
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    internal class ResourceDataWrapper<TKey,TValue> : IReadOnlyDictionary<TKey,TValue>
    {
        public TValue this[TKey key]
        {
            get
            {
                if (Data.TryGetValue(key, out var value)) return value;
                var errorMsg = $"{Name}.{nameof(GameResources)} Not Found, Key = {key}!";
                var exception = new KeyNotFoundException(errorMsg);
                Debug.LogError(errorMsg);
                throw exception;
            }
        }

        public ResourceDataWrapper(Dictionary<TKey, TValue> data,string name)
        {
            Data = data;
            Name = name;
        }

        private Dictionary<TKey,TValue> Data { get; }
        public string Name { get; }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) Data).GetEnumerator();
        public int Count => Data.Count;
        public bool ContainsKey(TKey key) => Data.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => Data.TryGetValue(key, out value);
        public IEnumerable<TKey> Keys => Data.Keys;
        public IEnumerable<TValue> Values => Data.Values;
    }

    public Sprite GetCardImage(GameCard card)
    {
        var info = card.GetInfo();
        switch (info.Type)
        {
            case GameCardType.Hero:
                return HeroImg[card.CardId];
            case GameCardType.Tower:
            case GameCardType.Trap:
                return FuZhuImg[info.ImageId];
            case GameCardType.Spell:
            case GameCardType.Soldier:
            case GameCardType.Base:
            default:
                throw XDebug.Throw<GameResources>($"不支持类型 [{info.Type}]!");
        }
    }
}