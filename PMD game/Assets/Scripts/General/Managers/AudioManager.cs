using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace GameSystem
{
    public class AudioManager : MonoBehaviour, IGameManagerListener
    {
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer _audioMixer;
        
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _ambientSource;
        [SerializeField] private AudioSource _uiSource;
        
        [Header("Audio Clips")]
        [SerializeField] private AudioClip _titleMusic;
        [SerializeField] private AudioClip _townMusic;
        [SerializeField] private AudioClip _dungeonMusic;
        [SerializeField] private AudioClip _combatMusic;
        [SerializeField] private AudioClip _victoryMusic;
        [SerializeField] private AudioClip _gameOverMusic;
        
        [Header("SFX Clips")]
        [SerializeField] private AudioClip _buttonClick;
        [SerializeField] private AudioClip _playerMove;
        [SerializeField] private AudioClip _playerAttack;
        [SerializeField] private AudioClip _enemyAttack;
        [SerializeField] private AudioClip _itemPickup;
        [SerializeField] private AudioClip _levelUp;
        
        // State
        private Dictionary<string, AudioClip> _sfxClips = new Dictionary<string, AudioClip>();
        private float _musicVolume = 0.8f;
        private float _sfxVolume = 0.8f;
        private float _ambientVolume = 0.5f;
        
        public float MusicVolume => _musicVolume;
        public float SfxVolume => _sfxVolume;
        public float AmbientVolume => _ambientVolume;
        
        public void Initialize()
        {
            Debug.Log("AudioManager Initializing");
            
            // Set up audio sources
            if (_musicSource == null) _musicSource = gameObject.AddComponent<AudioSource>();
            if (_sfxSource == null) _sfxSource = gameObject.AddComponent<AudioSource>();
            if (_ambientSource == null) _ambientSource = gameObject.AddComponent<AudioSource>();
            if (_uiSource == null) _uiSource = gameObject.AddComponent<AudioSource>();
            
            // Configure audio sources
            _musicSource.loop = true;
            _ambientSource.loop = true;
            
            // Populate SFX dictionary
            PopulateSfxDictionary();
            
            // Load saved volume settings
            LoadVolumeSettings();
            
            Debug.Log("AudioManager initialized successfully");
        }
        
        // ===== GAME MANAGER INTERFACE =====
        public void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.TitleScreen:
                    PlayMusic(_titleMusic);
                    break;
                case GameState.InTown:
                    PlayMusic(_townMusic);
                    break;
                case GameState.InDungeon:
                    PlayMusic(_dungeonMusic);
                    break;
            }
        }
        
        public void OnSceneChanged(SceneType sceneType, SceneConfig config)
        {
            // Update ambient sounds based on scene
            switch (sceneType)
            {
                case SceneType.Dungeon:
                    PlayAmbient("DungeonAmbient");
                    break;
                case SceneType.Town:
                    PlayAmbient("TownAmbient");
                    break;
                default:
                    StopAmbient();
                    break;
            }
        }
        
        public void OnPauseStateChanged(bool paused)
        {
            if (paused)
            {
                _musicSource.Pause();
                _ambientSource.Pause();
            }
            else
            {
                _musicSource.UnPause();
                _ambientSource.UnPause();
            }
        }
        
        // ===== MUSIC CONTROL =====
        public void PlayMusic(AudioClip clip, float fadeTime = 1f)
        {
            if (clip == null || _musicSource.clip == clip) return;
            
            if (fadeTime > 0 && _musicSource.isPlaying)
            {
                StartCoroutine(CrossfadeMusic(clip, fadeTime));
            }
            else
            {
                _musicSource.clip = clip;
                _musicSource.Play();
            }
        }
        
        public void StopMusic(float fadeTime = 1f)
        {
            if (fadeTime > 0)
            {
                StartCoroutine(FadeOutMusic(fadeTime));
            }
            else
            {
                _musicSource.Stop();
            }
        }
        
        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            _musicSource.volume = _musicVolume;
            
            // Save setting
            SaveVolumeSettings();
        }
        
        // ===== SFX CONTROL =====
        public void PlaySfx(string sfxName, float volumeScale = 1f)
        {
            if (_sfxClips.TryGetValue(sfxName, out var clip))
            {
                _sfxSource.PlayOneShot(clip, volumeScale * _sfxVolume);
            }
        }
        
        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip != null)
            {
                _sfxSource.PlayOneShot(clip, volumeScale * _sfxVolume);
            }
        }
        
        public void PlayUiSound(AudioClip clip, float volumeScale = 1f)
        {
            if (clip != null)
            {
                _uiSource.PlayOneShot(clip, volumeScale * _sfxVolume);
            }
        }
        
        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            _sfxSource.volume = _sfxVolume;
            _uiSource.volume = _sfxVolume;
            
            // Save setting
            SaveVolumeSettings();
        }
        
        // ===== AMBIENT CONTROL =====
        public void PlayAmbient(string ambientName)
        {
            // This would need your ambient audio clips
            // For now, it's a placeholder
            _ambientSource.volume = _ambientVolume;
            
            if (!_ambientSource.isPlaying)
            {
                _ambientSource.Play();
            }
        }
        
        public void StopAmbient(float fadeTime = 1f)
        {
            if (fadeTime > 0)
            {
                StartCoroutine(FadeOutAmbient(fadeTime));
            }
            else
            {
                _ambientSource.Stop();
            }
        }
        
        public void SetAmbientVolume(float volume)
        {
            _ambientVolume = Mathf.Clamp01(volume);
            _ambientSource.volume = _ambientVolume;
            
            // Save setting
            SaveVolumeSettings();
        }
        
        // ===== HELPER METHODS =====
        private void PopulateSfxDictionary()
        {
            // Add default SFX
            if (_buttonClick != null) _sfxClips["ButtonClick"] = _buttonClick;
            if (_playerMove != null) _sfxClips["PlayerMove"] = _playerMove;
            if (_playerAttack != null) _sfxClips["PlayerAttack"] = _playerAttack;
            if (_enemyAttack != null) _sfxClips["EnemyAttack"] = _enemyAttack;
            if (_itemPickup != null) _sfxClips["ItemPickup"] = _itemPickup;
            if (_levelUp != null) _sfxClips["LevelUp"] = _levelUp;
        }
        
        private System.Collections.IEnumerator CrossfadeMusic(AudioClip newClip, float fadeTime)
        {
            // Fade out current music
            float startVolume = _musicSource.volume;
            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                _musicSource.volume = Mathf.Lerp(startVolume, 0, t / fadeTime);
                yield return null;
            }
            
            // Switch clip and fade in
            _musicSource.clip = newClip;
            _musicSource.Play();
            
            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                _musicSource.volume = Mathf.Lerp(0, _musicVolume, t / fadeTime);
                yield return null;
            }
        }
        
        private System.Collections.IEnumerator FadeOutMusic(float fadeTime)
        {
            float startVolume = _musicSource.volume;
            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                _musicSource.volume = Mathf.Lerp(startVolume, 0, t / fadeTime);
                yield return null;
            }
            
            _musicSource.Stop();
            _musicSource.volume = _musicVolume;
        }
        
        private System.Collections.IEnumerator FadeOutAmbient(float fadeTime)
        {
            float startVolume = _ambientSource.volume;
            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                _ambientSource.volume = Mathf.Lerp(startVolume, 0, t / fadeTime);
                yield return null;
            }
            
            _ambientSource.Stop();
            _ambientSource.volume = _ambientVolume;
        }
        
        private void LoadVolumeSettings()
        {
            // Load from PlayerPrefs or save file
            _musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
            _sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
            _ambientVolume = PlayerPrefs.GetFloat("AmbientVolume", 0.5f);
            
            // Apply
            _musicSource.volume = _musicVolume;
            _sfxSource.volume = _sfxVolume;
            _uiSource.volume = _sfxVolume;
            _ambientSource.volume = _ambientVolume;
        }
        
        private void SaveVolumeSettings()
        {
            PlayerPrefs.SetFloat("MusicVolume", _musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", _sfxVolume);
            PlayerPrefs.SetFloat("AmbientVolume", _ambientVolume);
            PlayerPrefs.Save();
        }
        
        // ===== PUBLIC API =====
        public void PlayButtonClick()
        {
            PlayUiSound(_buttonClick);
        }
        
        public void PlayPlayerMove()
        {
            PlaySfx("PlayerMove", 0.7f);
        }
        
        public void PlayPlayerAttack()
        {
            PlaySfx("PlayerAttack");
        }
        
        public void PlayEnemyAttack()
        {
            PlaySfx("EnemyAttack");
        }
        
        public void PlayItemPickup()
        {
            PlaySfx("ItemPickup");
        }
        
        public void PlayLevelUp()
        {
            PlaySfx("LevelUp");
        }
        
        public void MuteAll(bool mute)
        {
            AudioListener.pause = mute;
        }
        
        // ===== EDITOR HELPERS =====
        [ContextMenu("Test Music")]
        private void TestMusic()
        {
            if (Application.isPlaying)
            {
                PlayMusic(_titleMusic);
            }
        }
    }
}