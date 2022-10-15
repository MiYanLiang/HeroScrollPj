using System.Collections;
using DG.Tweening;
using UnityEngine;

public class AudioController1 : MonoBehaviour
{
    public static AudioController1 instance;

    [HideInInspector]
    public AudioSource audioSource;

    [SerializeField]
    AudioClip[] audioClipsBack; //背景音乐
    [Range(0,1)]
    [SerializeField]
    float[] audioVolumeBack;    //背景音乐音量
    float audioPlayInterval;    //背景音乐间隔时间

    [SerializeField]
    float minRandTime;
    [SerializeField]
    float maxRandTime;

    [HideInInspector]
    public bool isPlayRandom;   //播放随机开关

    float audioTimer;

    [HideInInspector]
    public bool isNeedPlayLongMusic;   //是否要播放长背景音乐

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

        audioSource = GetComponent<AudioSource>();
        audioPlayInterval = minRandTime;
        audioTimer = 0;
        isPlayRandom = false;
        isNeedPlayLongMusic = true;
    }

    private void LateUpdate()
    {
        if (isPlayRandom)
        {
            PlayRandomBackClip();
        }
    }

    //随机背景小音乐
    private void PlayRandomBackClip()
    {
        if (!GamePref.PrefMusicPlay) return;

        audioTimer += Time.deltaTime;
        if (audioTimer >= audioPlayInterval)
        {
            audioTimer = 0;
            audioPlayInterval = Random.Range(minRandTime, maxRandTime);
            //音乐随机库
            int rand = Random.Range(0, audioClipsBack.Length);
            ChangeAudioClip(audioClipsBack[rand], audioVolumeBack[rand]);
            audioSource.Play();
        }
    }


    //关闭长背景音乐
    public void FadeEndMusic()
    {
        isNeedPlayLongMusic = false;
        StartCoroutine(AudioFade(3));

        IEnumerator AudioFade(int secs)
        {
            var frame = secs * 1000 / 100;
            var value = audioSource.volume / frame;
            for (int i = 0; i < frame; i++)
            {
                audioSource.volume -= value;
                yield return new WaitForSeconds(0.1f);
            }

            if (!isNeedPlayLongMusic)
            {
                audioSource.Stop();
                audioSource.loop = false;
                isPlayRandom = true;
            }
        }
    }

    //改变音乐播放器clip
    private void ChangeAudioClip(AudioClip audioClip, float audioVolume)
    {
        if (!GamePref.PrefMusicPlay) return;
        //Debug.Log("audioVolume: " + audioVolume + " audioClip: " + audioClip.name);
        audioSource.clip = audioClip;
        audioSource.volume = audioVolume;
    }

    //播放长背景音乐参数设置
    public void PlayLoop(AudioClip audioClip, float audioVolume,float delayed = 0f)
    {
        if (!GamePref.PrefMusicPlay) return;
        StopAllCoroutines();
        isNeedPlayLongMusic = true;
        ChangeAudioClip(audioClip, audioVolume);
        isPlayRandom = false;
        audioSource.loop = true;
        audioSource.PlayDelayed(delayed);
    }

    //播放音乐
    public void PlayAudioSource(float delayedTime)
    {
        if (!GamePref.PrefMusicPlay)
            return;

        audioSource.PlayDelayed(delayedTime);
    }

    public void MusicSwitch(bool play)
    {
        if (!GamePref.PrefMusicPlay) return;
        if (play)audioSource.Play();
        else audioSource.Pause();
    }
}
