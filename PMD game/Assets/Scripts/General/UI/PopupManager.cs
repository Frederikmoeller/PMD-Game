// PopupManager.cs
using UnityEngine;
using UnityEngine.UI;
using System;

public class PopupManager : MonoBehaviour
{
    [Header("Popup References")]
    public GameObject inventoryPopup;
    public GameObject pauseMenuPopup;
    public GameObject deathScreenPopup;
    public GameObject confirmationPopup;
    public GameObject loadingScreen;
    
    [Header("Confirmation Popup Components")]
    public Text popupTitleText;
    public Text popupMessageText;
    public Button popupConfirmButton;
    public Button popupCancelButton;
    
    [Header("Inventory Components")]
    public Transform inventoryItemContainer;
    public GameObject inventoryItemPrefab;
    
    private Action _currentConfirmAction;
    private Action _currentCancelAction;
    private PlayerStats _playerStats;
    
    public void Initialize(PlayerStats playerStats = null)
    {
        _playerStats = playerStats;
        
        // Hide all popups initially
        HideAllPopups();
        
        // Setup confirmation button listeners
        if (popupConfirmButton != null)
            popupConfirmButton.onClick.AddListener(OnPopupConfirmClicked);
        
        if (popupCancelButton != null)
            popupCancelButton.onClick.AddListener(OnPopupCancelClicked);
    }
    
    public void ShowConfirmation(string title, string message, Action onConfirm, Action onCancel = null)
    {
        if (confirmationPopup == null) return;
        
        _currentConfirmAction = onConfirm;
        _currentCancelAction = onCancel;
        
        if (popupTitleText != null)
            popupTitleText.text = title;
        if (popupMessageText != null)
            popupMessageText.text = message;
        
        confirmationPopup.SetActive(true);
    }
    
    public void ShowStairsPopup(int nextFloor, Action onConfirm, Action onCancel = null)
    {
        ShowConfirmation(
            $"Floor {nextFloor}",
            GetFloorDescription(nextFloor),
            onConfirm,
            onCancel
        );
    }
    
    public void ToggleInventory()
    {
        if (inventoryPopup == null) return;
        
        bool show = !inventoryPopup.activeSelf;
        inventoryPopup.SetActive(show);
        
        if (show && _playerStats != null)
        {
            RefreshInventoryUI();
        }
    }
    
    public void TogglePauseMenu()
    {
        if (pauseMenuPopup == null) return;
        
        bool show = !pauseMenuPopup.activeSelf;
        pauseMenuPopup.SetActive(show);
    }
    
    public void ShowDeathScreen()
    {
        if (deathScreenPopup != null)
            deathScreenPopup.SetActive(true);
    }
    
    public void ShowLoadingScreen(bool show)
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(show);
    }
    
    public void HideAllPopups()
    {
        if (inventoryPopup != null) inventoryPopup.SetActive(false);
        if (pauseMenuPopup != null) pauseMenuPopup.SetActive(false);
        if (deathScreenPopup != null) deathScreenPopup.SetActive(false);
        if (confirmationPopup != null) confirmationPopup.SetActive(false);
    }
    
    private void RefreshInventoryUI()
    {
        if (_playerStats == null || inventoryItemContainer == null || inventoryItemPrefab == null)
            return;
        
        foreach (Transform child in inventoryItemContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Add current inventory items
        foreach (var item in _playerStats.Inventory)
        {
            var itemUI = Instantiate(inventoryItemPrefab, inventoryItemContainer);
            var itemComponent = itemUI.GetComponent<InventoryItemUI>();
            if (itemComponent != null)
            {
                itemComponent.Setup(item);
            }
        }
    }
    
    private void OnPopupConfirmClicked()
    {
        if (_currentConfirmAction != null)
            _currentConfirmAction.Invoke();
        
        if (confirmationPopup != null)
            confirmationPopup.SetActive(false);
    }
    
    private void OnPopupCancelClicked()
    {
        if (_currentCancelAction != null)
            _currentCancelAction.Invoke();
        
        if (confirmationPopup != null)
            confirmationPopup.SetActive(false);
    }
    
    private string GetFloorDescription(int floor)
    {
        if (floor <= 5) return "A relatively safe area for beginners.";
        if (floor <= 10) return "Deeper levels with stronger enemies.";
        if (floor <= 15) return "Challenging depths with rare treasures.";
        return "Dangerous territory. Proceed with caution.";
    }
}