using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Assets;
using CorrelateLib;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerCharacterUi : MonoBehaviour
{
    private enum States
    {
        NewCharacter,
        Registered
    }
    public Image Avatar;
    public Image AvatarFrame;
    public Text MilitaryPower;
    public InputField Name;
    public InputField Nickname;
    public InputField Sign;
    public CharacterGenderUi GenderUi;
    public Button CloseButton;
    public Button SubmitButton;
    public Button NameEditButton;
    public Button NicknameEditButton;
    public Button GenderEditButton;
    public Button SignEditButton;
    private ComponentActivateSwitch<Button> mapper;
    public Character Character { get; private set; }
    public event UnityAction OnCloseAction;
    
    private bool isInit = false;
    private States state;

    public void Init()
    {
        if (isInit) throw XDebug.Throw<PlayerCharacterUi>("Duplicate init!");
        mapper = new ComponentActivateSwitch<Button>((active, btn) =>
        {
            btn.interactable = active;
            btn.gameObject.SetActive(active);
        });
        mapper.Add(States.NewCharacter, SubmitButton);
        mapper.Add(States.Registered, NameEditButton, NicknameEditButton, GenderEditButton, SignEditButton);
        SubmitButton.onClick.AddListener(ReqCreateCharacter);
        CloseButton.onClick.AddListener(Off);
        GenderUi.Init();
        GenderUi.SetAvailability(true);
        gameObject.SetActive(false);
        isInit = true;
    }

    //private void ResetApiUpdateUis()
    //{
    //    var isCharacterValid = Character != null && Character.IsValidCharacter();
    //    consumeUis.ForEach(ui => ui.gameObject.SetActive(isCharacterValid));
    //    if (!isCharacterValid) return;
    //    UpdateNameApi.SetEditInteractive(new[] { Name });
    //    UpdateNameApi.SetCost(ApiConsumeUi.Currencies.YvQue, 100);
    //    UpdateNameApi.ResetApi(() => !string.IsNullOrWhiteSpace(Name.text) && !Name.text.Equals(Character.Name),
    //        EventStrings.Req_UpdateCharacterName, OnSuccessUpdateCharacter);
    //    UpdateNicknameApi.SetEditInteractive(new[] { Nickname });
    //    UpdateNicknameApi.SetCost(ApiConsumeUi.Currencies.YvQue, 100);
    //    UpdateNicknameApi.ResetApi(
    //        () => !string.IsNullOrWhiteSpace(Nickname.text) && !Nickname.text.Equals(Character.Nickname),
    //        EventStrings.Req_UpdateCharacterNickname, OnSuccessUpdateCharacter);
    //    UpdateGenderApi.SetEditInteractive(new[] { GenderUi.Male, GenderUi.Female });
    //    UpdateGenderApi.SetCost(ApiConsumeUi.Currencies.YvQue, 100);
    //    UpdateGenderApi.ResetApi(() => (int)GenderUi.Gender != Character.Gender,
    //        EventStrings.Req_UpdateCharacterGender, OnSuccessUpdateCharacter);
    //    UpdateSignApi.SetEditInteractive(new[] { Sign });
    //    UpdateSignApi.SetCost(ApiConsumeUi.Currencies.YuanBao, 200);
    //    UpdateSignApi.ResetApi(() => !string.IsNullOrWhiteSpace(Sign.text) && Sign.text.Equals(Character.Sign),
    //        EventStrings.Req_UpdateCharacterSign, OnSuccessUpdateCharacter);
    //}

    private void OnSuccessUpdateCharacter(ViewBag vb)
    {
        ConsumeManager.instance.SaveChangeUpdatePlayerData(vb.GetPlayerDataDto());
        PlayerDataForGame.instance.UpdateCharacter(vb.GetPlayerCharacterDto());
        UIManager.instance.ConfirmationWindowUi.Cancel();
        Show();
    }

    public void Show()
    {

        Character = PlayerDataForGame.instance.Character;
        state = Character == null ? States.NewCharacter : States.Registered;

        GenderUi.OnNotifyChanged.RemoveAllListeners();
        GenderUi.OnNotifyChanged.AddListener(g => SetAvatar((int) g));
        mapper.Set(state);
        if (Character != null)
        {
            OnComponentInputSubscribe(Name);//, NameEditButton, ()=> Character.Name);
            ApiRequestSet(NameEditButton, CharacterUpdateInfos.Name, () => Name.text);
            OnComponentInputSubscribe(Nickname);//, NicknameEditButton, ()=> Character.Nickname);
            ApiRequestSet(NicknameEditButton, CharacterUpdateInfos.Nickname, () => Nickname.text);
            OnComponentInputSubscribe(Sign);//, SignEditButton, ()=> Character.Sign);
            ApiRequestSet(SignEditButton, CharacterUpdateInfos.Sign, () => Sign.text);
            ApiRequestSet(GenderEditButton, CharacterUpdateInfos.Gender, () => GenderUi.Gender);
            GenderUi.OnNotifyChanged.AddListener(gender => ResolveAllEditBtns(gender.ToString()));
                //GenderEditButton.interactable = gender != (CharacterGender) Character.Gender);
            Name.text = Character.Name;
            Nickname.text = Character.Nickname;
            Sign.text = Character.Sign;
            GenderUi.SetGender((CharacterGender) Character.Gender);
        }

        gameObject.SetActive(true);

        void SetAvatar(int id)
        {
            if (GameResources.Instance != null)
                Avatar.sprite = GameResources.Instance.Avatar[id];
        }
    }

    private void ApiRequestSet(Button btn,CharacterUpdateInfos info, Expression<Func<object>> expression)
    {
        var isSign = info == CharacterUpdateInfos.Sign;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
            UIManager.instance.OnConfirmation(() => ApiRequest(info, expression),
                isSign ? ConfirmationWindowUi.Resources.YuanBao : ConfirmationWindowUi.Resources.YuQue,
                isSign ? 200 : 100));

        void ApiRequest(CharacterUpdateInfos updateInfo,Expression<Func<object>> ex)
        {
            var func = ex.Compile();
            ApiPanel.instance.InvokeVb(OnSuccessUpdateCharacter, PlayerDataForGame.instance.ShowStringTips,
                EventStrings.Req_UpdateCharacterInfo, ViewBag.Instance().SetValues(updateInfo, func()));
        }
    }

    private void OnComponentInputSubscribe(InputField inputField)
    //private void OnComponentInputSubscribe(InputField inputField, Button btn, Expression<Func<string>> valueEx)
    {
        inputField.onValueChanged.RemoveAllListeners();
        inputField.onValueChanged.AddListener(ResolveAllEditBtns);
        //inputField.onValueChanged.AddListener(input =>
        //{
        //    var value = valueEx.Compile()();
        //    btn.interactable = value != input;
        //});
    }

    private void ResolveAllEditBtns(string arg0)
    {
        NameEditButton.interactable = Name.text != Character.Name;
        NicknameEditButton.interactable = Nickname.text != Character.Nickname;
        SignEditButton.interactable = Sign.text != Character.Sign;
        GenderEditButton.interactable = GenderUi.Gender != (CharacterGender)Character.Gender;
    }

    public void Off()
    {
        gameObject.SetActive(false);
        OnCloseAction?.Invoke();
    }

    private void ReqCreateCharacter()
    {
        if (state == States.Registered) return;
        Character = new Character
        {
            Name = Name.text, Nickname = Nickname.text, Gender = (int) GenderUi.Gender, Sign = Sign.text
        };
        if (Character.IsValidCharacter())//完整信息才请求
        {
            PlayerDataForGame.instance.Character = global::Character.Instance(Character);
            ApiPanel.instance.InvokeVb(OnCreateCharacterSuccess, PlayerDataForGame.instance.ShowStringTips,
                EventStrings.Req_CreateCharacter, ViewBag.Instance().PlayerCharacterDto(Character.ToDto()), false);
            return;
        }

        CheckEntry(Name);
        CheckEntry(Nickname);

        void CheckEntry(InputField inputField)
        {
            if (string.IsNullOrWhiteSpace(inputField.text))
            {
                var text = inputField.placeholder.GetComponent<Text>();
                text.text = "请输入";
                text.color = Color.red;
            }
        }
    }

    private void OnCreateCharacterSuccess(ViewBag vb)
    {
        PlayerDataForGame.instance.NotifyDataUpdate();
        Show();
    }
}

public enum CharacterGender
{
    Female,
    Male
}