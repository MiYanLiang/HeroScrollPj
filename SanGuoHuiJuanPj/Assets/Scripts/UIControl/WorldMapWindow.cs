using System;
using CorrelateLib;
using UnityEngine;
using UnityEngine.UI;

public class WorldMapWindow : MonoBehaviour
{
    private enum Switches
    {
        Character,
        World
    }
    public CityInfoUi CityInfo;
    public PlayerCharacterUi PlayerCharacter;
    public WorldMapUi WorldMap;
    public Button CharacterButton;
    public Button WorldButton;
    public Button CloseButton;

    void Start()
    {
        CharacterButton.onClick.AddListener(()=>OnSwitch(Switches.Character));
        WorldButton.onClick.AddListener(()=>OnSwitch(Switches.World));
        CloseButton.onClick.AddListener(()=>gameObject.SetActive(false));
    }

    public void Show() => gameObject.SetActive(true);

    private void OnSwitch(Switches state)
    {
        switch (state)
        {
            case Switches.Character:
                WorldMap.Off();
                PlayerCharacter.Show();
                break;
            case Switches.World:
                PlayerCharacter.Off();
                WorldMap.Show();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    public void CreateCharacter()
    {
        ApiPanel.instance.CallVb(vb =>
            {
                Debug.Log("角色创建成功！");
            }, Debug.Log,
            EventStrings.Call_CreateCharacter,
            DataBag.SerializeBag(EventStrings.Call_CreateCharacter, PlayerCharacter.Character.ToDto()));
    }
}