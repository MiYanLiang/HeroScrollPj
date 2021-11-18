using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AudioController0 : MonoBehaviour
{
    public static AudioController0 instance;

    [HideInInspector]
    public AudioSource audioSource;

    public AudioClip[] audioClips;  //0奖励,1-8古筝,12开始游戏,13点击选择,14出战，15回城，16升星,17出售,18确认出售,19升级战鼓成功-进入非战斗事件,20升级战鼓失败，21进入战斗事件
    [Range(0,1)]
    public float[] audioVolumes;

    bool isPlaying;
    private List<AudioSource> StackAudios { get; set; } = new List<AudioSource>();
    private AudioSource AudioPrefab { get; set; }

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
        isPlaying = false;
        audioSource = GetComponent<AudioSource>();
    }

    private AudioSource InstanceNewAudioSource()
    {
        var aus = gameObject.AddComponent<AudioSource>();
        StackAudios.Add(aus);
        return aus;
    }

    public void StackPlaying(AudioClip clip, float volume)
    {
        if (!GamePref.PrefMusicPlay) return;
        var aus = StackAudios.FirstOrDefault(s => !s.isPlaying);
        if (aus == null)
            aus = InstanceNewAudioSource();
        aus.volume = volume;
        aus.PlayOneShot(clip);
    }

    /// <summary>
    /// 改变音乐播放器clip
    /// </summary>
    public bool ChangeAudioClip(int audioClipId)
    {
        if (isPlaying) return false;
        audioSource.clip = audioClips[audioClipId];
        audioSource.volume = audioVolumes[audioClipId];
        return true;
    }

    public void ForcePlayAudio(int audioClipId)
    {
        StopAllCoroutines();
        if(audioSource.isPlaying) audioSource.Stop();
        audioSource.clip = audioClips[audioClipId];
        audioSource.volume = audioVolumes[audioClipId];
        StartCoroutine(StartPlaying(0));
    }

    public bool ChangeAudioClip(AudioClip audioClip, float volume)
    {
        if (isPlaying) return false;
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        return true;
    }

    //改变记录状态
    IEnumerator StartPlaying(float waitTime)
    {
        isPlaying = true;
        audioSource.PlayDelayed(waitTime);
        yield return new WaitForSeconds(audioSource.clip.length);
        isPlaying = false;
    }

    /// <summary>
    /// 播放音乐
    /// </summary>
    /// <param name="delayedTime">延迟时间</param>
    public void PlayAudioSource(float delayedTime)
    {
        if (!GamePref.PrefMusicPlay)
            return;

        if (!isPlaying) StartCoroutine(StartPlaying(delayedTime));

        //if (audioSource.isPlaying)
        //{
        //    audioSource.DOFade(0, 0.3f).OnComplete(delegate ()
        //    {
        //        audioSource.PlayDelayed(delayedTime);
        //    });
        //}
    }

    /// <summary>
    /// 随机播放古筝音效
    /// </summary>
    public void RandomPlayGuZhengAudio()
    {
        int rand = Random.Range(1, 8);
        //ChangeAudioClip(rand);
        ForcePlayAudio(rand);
        //PlayAudioSource(0);
    }

    public void MusicSwitch(bool isPlayMusic)
    {
        if(isPlayMusic) audioSource.Play();
        else audioSource.Pause();
    }

    private class StackAudio
    {
        public bool IsPlaying { get; set; }
        public AudioSource Source { get; set; }
    }
}
