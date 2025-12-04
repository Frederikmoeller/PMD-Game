using System.Collections;
using TMPro;
using UnityEngine;

namespace DialogueSystem.UI
{
    public class TypewriterEffect : MonoBehaviour
    {
        [Header("Typewriter Settings")]
        [SerializeField] private float charactersPerSecond = 50f;
        [SerializeField] private float startDelay = 0.1f;
        [SerializeField] private bool pauseOnPunctuation = true;
        [SerializeField] private float punctuationPauseMultiplier = 3f;
        
        [Header("Audio")]
        [SerializeField] private bool useAudio = true;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip typeSound;
        [SerializeField] private float soundVolume = 0.5f;
        [SerializeField] private float minPitch = 0.9f;
        [SerializeField] private float maxPitch = 1.1f;
        
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
            
            if (useAudio && audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                    audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        public void StartTyping(string text, float speedOverride = -1f)
        {
            if (_typewriterCoroutine != null)
                StopCoroutine(_typewriterCoroutine);

            _currentText = text;
                
            _textComponent.text = "";
            _textComponent.ForceMeshUpdate();

            float speed = speedOverride > 0 ? speedOverride : charactersPerSecond;
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
            yield return new WaitForSeconds(startDelay);
            
            float delay = 1f / speed;
            
            for (int i = 0; i < text.Length; i++)
            {
                // Add the next character
                _textComponent.text += text[i];
                _textComponent.ForceMeshUpdate();
        
                OnCharacterTyped?.Invoke();
                
                // Play sound if enabled
                if (useAudio && typeSound != null && audioSource != null)
                {
                    audioSource.pitch = Random.Range(minPitch, maxPitch);
                    audioSource.PlayOneShot(typeSound, soundVolume);
                }
        
                // Calculate pause for current character
                float currentDelay = delay;
                if (pauseOnPunctuation && IsPunctuation(text[i]))
                {
                    currentDelay = delay * punctuationPauseMultiplier;
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