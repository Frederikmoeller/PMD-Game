using System.Collections;
using TMPro;
using UnityEngine;

namespace DialogueSystem.UI
{
    public class TypewriterEffect : MonoBehaviour
    {
        [Header("Typewriter Settings")]
        [SerializeField] private float _charactersPerSecond = 50f;
        [SerializeField] private float _startDelay = 0.1f;
        [SerializeField] private bool _pauseOnPunctuation = true;
        [SerializeField] private float _punctuationPauseMultiplier = 3f;
        
        [Header("Audio")]
        [SerializeField] private bool _useAudio = true;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _typeSound;
        [SerializeField] private float _soundVolume = 0.5f;
        [SerializeField] private float _minPitch = 0.9f;
        [SerializeField] private float _maxPitch = 1.1f;
        
        private TMP_Text _textComponent;
        private Coroutine _typewriterCoroutine;

        private string _currentText;
        
        // Events
        public System.Action OnCharacterTyped;
        public System.Action OnTypingCompleted;
        
        public bool IsTyping => _typewriterCoroutine != null;
        
        private void Awake()
        {
            _textComponent = GetComponent<TMP_Text>();
            
            if (_useAudio && _audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                    _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        public void StartTyping(string text, float speedOverride = -1f)
        {
            if (_typewriterCoroutine != null)
                StopCoroutine(_typewriterCoroutine);

            _currentText = text;
                
            _textComponent.text = "";
            _textComponent.ForceMeshUpdate();

            float speed = speedOverride > 0 ? speedOverride : _charactersPerSecond;
            _typewriterCoroutine = StartCoroutine(TypeTextRoutine(text, speed));
        }
        
        public void Finish()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
                _textComponent.text = _currentText;
            }
            
            OnTypingCompleted?.Invoke();
        }
        
        private IEnumerator TypeTextRoutine(string text, float speed)
        {
            yield return new WaitForSeconds(_startDelay);
            
            float delay = 1f / speed;
            
            for (int i = 0; i < text.Length; i++)
            {
                // Add the next character
                _textComponent.text += text[i];
                _textComponent.ForceMeshUpdate();
        
                OnCharacterTyped?.Invoke();
                
                // Play sound if enabled
                if (_useAudio && _typeSound != null && _audioSource != null)
                {
                    _audioSource.pitch = Random.Range(_minPitch, _maxPitch);
                    _audioSource.PlayOneShot(_typeSound, _soundVolume);
                }
        
                // Calculate pause for current character
                float currentDelay = delay;
                if (_pauseOnPunctuation && IsPunctuation(text[i]))
                {
                    currentDelay = delay * _punctuationPauseMultiplier;
                }
                
                yield return new WaitForSeconds(currentDelay);
            }
            
            _typewriterCoroutine = null;
            OnTypingCompleted?.Invoke();
        }
        
        private bool IsPunctuation(char character)
        {
            return character == '.' || character == '!' || character == '?' || character == ',';
        }
        
        // Public method to stop typing without triggering completion event
        public void Stop()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }
        }
    }
}