using System;
using UnityEngine;

public class WarMusicController : MonoBehaviour
{
    public static WarMusicController Instance { get; private set; }
    [SerializeField] private AudioClip[] BattleBgm;
    [SerializeField] private AudioClip[] EffectAudios;

    private void Awake()
    {
        if (Instance)
            throw new InvalidOperationException();
        Instance = this;
    }
    public void OnBattleMusic()
    {
        PlayAudio(21);
    }

    public void PlayBgm(int index, float volume = 0.5f) => AudioController1.instance.PlayLoop(BattleBgm[index], volume);

    public void PlayWarEffect(int clipIndex, float volume = 1f)
    {
        var clip = EffectAudios[clipIndex];
#if UNITY_EDITOR
        if (clip == null)
            Debug.LogError($"找不到音效id = {clipIndex}!");
#endif
        AudioController0.instance.StackPlaying(clip, volume);
    }

    private void PlayAudio(int index)
    {
        AudioController0.instance.ChangeAudioClip(index);
        AudioController0.instance.PlayAudioSource(0);
    }
}