﻿using System;
using UnityEngine;
using UnityEngine.UI;

public class LoginUi : SignInBaseUi
{
    public InputField username;
    public InputField password;
    public Button regBtn;
    public Button directLoginBtn;
    public Button forgetPasswordBtn;
    public Button resetAccountBtn;
    public Button loginBtn;

    public override void ResetUi()
    {
        username.text = GamePref.Username;
        password.text = GamePref.Password;
        regBtn.onClick.RemoveAllListeners();
        directLoginBtn.onClick.RemoveAllListeners();
        forgetPasswordBtn.onClick.RemoveAllListeners();
        resetAccountBtn.onClick.RemoveAllListeners();
        loginBtn.onClick.RemoveAllListeners();
        base.ResetUi();
    }
}