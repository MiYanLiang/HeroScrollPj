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
        if (!GamePref.PrefMusicPlay)
            return;

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
    public void ChangeBackMusic()
    {
        isNeedPlayLongMusic = false;
        audioSource.DOFade(0, 3f).OnComplete(delegate ()
        {
            if (!isNeedPlayLongMusic)
            {
                audioSource.Stop();
                audioSource.loop = false;
                isPlayRandom = true;
            }
        });
    }

    //改变音乐播放器clip
    public void ChangeAudioClip(AudioClip audioClip, float audioVolume)
    {
        //Debug.Log("audioVolume: " + audioVolume + " audioClip: " + audioClip.name);
        audioSource.clip = audioClip;
        audioSource.volume = audioVolume;
    }

    //播放长背景音乐参数设置
    public void PlayLongBackMusInit()
    {
        isPlayRandom = false;
        audioSource.loop = true;

        if (!GamePref.PrefMusicPlay)
            return;
        audioSource.Play();
    }

    //播放音乐
    public void PlayAudioSource(float delayedTime)
    {
        //Debug.Log("AudioController1.PlayAudioSource()");

        if (!GamePref.PrefMusicPlay)
            return;

        audioSource.PlayDelayed(delayedTime);
    }

    public void MusicSwitch(bool isPlay)
    {
        if(isPlay)audioSource.Play();
        else audioSource.Pause();
    }
}
