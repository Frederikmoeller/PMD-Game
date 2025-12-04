using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Player Info")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI levelText;
    public Image healthBar;
    
    [Header("Story/Dialogue")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI speakerNameText;
    
    [Header("Floor Info")]
    public TextMeshProUGUI floorText;
    public Animator floorTransitionAnimator;
    
    [Header("References")]
    public PlayerStats playerStats;
    
    void Start()
    {
        // Find player stats if not assigned
        if (playerStats == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerStats = player.GetComponent<PlayerStats>();
            }
        }
        
        // Subscribe to events
        if (playerStats != null)
        {
            playerStats.OnHealthChanged.AddListener(UpdateHealthUI);
            playerStats.OnLevelUp.AddListener(UpdateLevelUI);
        }
        
        if (GameManager.Instance != null)
        {
            // Could subscribe to GameManager events
        }
        
        UpdateAllUI();
    }
    
    void UpdateAllUI()
    {
        if (playerStats != null)
        {
            UpdateHealthUI(playerStats.CurrentHealth);
            UpdateLevelUI(playerStats.Level);
        }
        
        if (GameManager.Instance != null)
        {
            UpdateFloorUI(GameManager.Instance.CurrentFloor);
        }
    }
    
    void UpdateHealthUI(int health)
    {
        if (healthText != null)
        {
            healthText.text = $"HP: {health}/{playerStats.MaxHealth}";
        }
        
        if (healthBar != null && playerStats != null)
        {
            healthBar.fillAmount = (float)health / playerStats.MaxHealth;
        }
    }
    
    void UpdateLevelUI(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"Lv: {level}";
        }
    }
    
    void UpdateFloorUI(int floor)
    {
        if (floorText != null)
        {
            floorText.text = $"B{floor}F";
        }
        
        // Play transition animation
        if (floorTransitionAnimator != null)
        {
            floorTransitionAnimator.Play("FloorTransition");
        }
    }
    
    public void ShowDialogue(string speaker, string dialogue)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (speakerNameText != null) speakerNameText.text = speaker;
        if (dialogueText != null) dialogueText.text = dialogue;
        
        // Pause game during dialogue
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartCutscene();
        }
    }
    
    public void HideDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        
        // Resume game
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndCutscene();
        }
    }
    
    // Called by continue button in dialogue
    public void ContinueDialogue()
    {
        HideDialogue();
    }
}