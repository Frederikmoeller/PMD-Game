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

    void Start()
    {
        
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