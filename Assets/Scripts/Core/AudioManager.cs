using UnityEngine;

namespace LoseWeight.Core
{
    /// <summary>
    /// 音频管理器 - 管理背景音乐和音效
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _sfxSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip _bgmClip;
        [SerializeField] private AudioClip _clickClip;
        [SerializeField] private AudioClip _punchClip;
        [SerializeField] private AudioClip _hitClip;
        [SerializeField] private AudioClip _victoryClip;
        [SerializeField] private AudioClip _defeatClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            float bgmVol = PlayerPrefs.GetFloat("BgmVolume", 0.8f);
            float sfxVol = PlayerPrefs.GetFloat("SfxVolume", 1f);
            SetBgmVolume(bgmVol);
            SetSfxVolume(sfxVol);

            PlayBgm();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PunchDetectedEvent>(OnPunch);
            EventBus.Subscribe<DamageEvent>(OnDamage);
            EventBus.Subscribe<MatchEndEvent>(OnMatchEnd);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PunchDetectedEvent>(OnPunch);
            EventBus.Unsubscribe<DamageEvent>(OnDamage);
            EventBus.Unsubscribe<MatchEndEvent>(OnMatchEnd);
        }

        public void PlayBgm()
        {
            if (_bgmSource != null && _bgmClip != null)
            {
                _bgmSource.clip = _bgmClip;
                _bgmSource.loop = true;
                _bgmSource.Play();
            }
        }

        public void StopBgm()
        {
            _bgmSource?.Stop();
        }

        public void PlaySfx(AudioClip clip)
        {
            if (_sfxSource != null && clip != null)
                _sfxSource.PlayOneShot(clip);
        }

        public void PlayClick()
        {
            PlaySfx(_clickClip);
        }

        public void SetBgmVolume(float volume)
        {
            if (_bgmSource != null)
                _bgmSource.volume = volume;
        }

        public void SetSfxVolume(float volume)
        {
            if (_sfxSource != null)
                _sfxSource.volume = volume;
        }

        public void SetBgmMute(bool mute)
        {
            if (_bgmSource != null)
                _bgmSource.mute = mute;
        }

        public void SetSfxMute(bool mute)
        {
            if (_sfxSource != null)
                _sfxSource.mute = mute;
        }

        private void OnPunch(PunchDetectedEvent evt)
        {
            PlaySfx(_punchClip);
        }

        private void OnDamage(DamageEvent evt)
        {
            PlaySfx(_hitClip);
        }

        private void OnMatchEnd(MatchEndEvent evt)
        {
            if (evt.WinnerId == "player")
                PlaySfx(_victoryClip);
            else
                PlaySfx(_defeatClip);
        }
    }
}
