using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SaveLoadMenuController : MonoBehaviour
{
    private enum ConfirmationAction
    {
        None,
        Overwrite,
        Delete
    }

    [Header("Open Buttons")]
    [SerializeField] private Button openSaveButton;
    [SerializeField] private Button openLoadButton;

    [Header("Main Panel")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text feedbackText;
    [SerializeField] private SaveSlotUI[] slotViews;
    [SerializeField] private Button closeButton;

    [Header("Confirmation")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private Text confirmationMessageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private SaveMenuMode mode;
    private ConfirmationAction pendingAction;
    private int pendingSlotId;
    private bool ownsPause;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        BindSlotViews();
        SetPanelState(false);
    }

    private void OnEnable()
    {
        AddListeners();
        BindSlotViews();
    }

    private void OnDisable()
    {
        RemoveListeners();
        RestoreTimeScale();
    }

    public void OpenSaveMenu()
    {
        Open(SaveMenuMode.Save);
    }

    public void OpenLoadMenu()
    {
        Open(SaveMenuMode.Load);
    }

    public void CloseMenu()
    {
        HideConfirmation();
        SetPanelState(false);
        RestoreTimeScale();
    }

    public void HandleSlotAction(int slotId)
    {
        SaveSlotInfo info = SaveSlotStorage.GetSlotInfo(slotId);
        if (mode == SaveMenuMode.Load)
        {
            if (!info.CanLoad)
            {
                SetFeedback($"Slot {slotId} cannot be loaded: {info.Status}.");
                return;
            }

            PerformLoad(slotId);
            return;
        }

        if (info.HasFile)
        {
            ShowConfirmation(
                ConfirmationAction.Overwrite,
                slotId,
                $"Overwrite Save Slot {slotId}?");
            return;
        }

        PerformSave(slotId);
    }

    public void RequestDelete(int slotId)
    {
        SaveSlotInfo info = SaveSlotStorage.GetSlotInfo(slotId);
        if (!info.HasFile)
        {
            RefreshSlots();
            SetFeedback($"Slot {slotId} is already empty.");
            return;
        }

        ShowConfirmation(
            ConfirmationAction.Delete,
            slotId,
            $"Delete Save Slot {slotId}?\nThis cannot be undone.");
    }

    public void RefreshSlots()
    {
        if (slotViews == null)
            return;

        for (int i = 0; i < slotViews.Length; i++)
        {
            SaveSlotUI view = slotViews[i];
            if (view == null)
                continue;

            int slotId = i + 1;
            view.Bind(this, slotId);
            view.Refresh(SaveSlotStorage.GetSlotInfo(slotId), mode);
        }
    }

    private void Open(SaveMenuMode requestedMode)
    {
        mode = requestedMode;
        if (!ownsPause)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            ownsPause = true;
        }

        if (titleText != null)
            titleText.text = mode == SaveMenuMode.Save ? "SAVE GAME" : "LOAD GAME";
        SetFeedback(string.Empty);
        HideConfirmation();
        SetPanelState(true);
        RefreshSlots();
    }

    private void PerformSave(int slotId)
    {
        HideConfirmation();
        SaveManager manager = Object.FindAnyObjectByType<SaveManager>();
        if (manager == null)
        {
            SetFeedback("Save failed: no active SaveManager was found.");
            return;
        }

        if (manager.SaveToSlot(slotId))
            SetFeedback($"Saved to Slot {slotId}.");
        else
            SetFeedback($"Save Slot {slotId} failed. See Console for details.");
        RefreshSlots();
    }

    private void PerformLoad(int slotId)
    {
        CloseMenu();
        if (SaveManager.RequestLoadSlot(slotId, out string error))
            return;

        Open(SaveMenuMode.Load);
        SetFeedback($"Load Slot {slotId} failed: {error}");
    }

    private void PerformDelete(int slotId)
    {
        HideConfirmation();
        if (SaveManager.DeleteSlot(slotId, out string error))
            SetFeedback($"Deleted Slot {slotId}.");
        else
            SetFeedback($"Delete Slot {slotId} failed: {error}");
        RefreshSlots();
    }

    private void ShowConfirmation(ConfirmationAction action, int slotId, string message)
    {
        pendingAction = action;
        pendingSlotId = slotId;
        if (confirmationMessageText != null)
            confirmationMessageText.text = message;
        if (confirmationPanel != null)
            confirmationPanel.SetActive(true);
    }

    private void HideConfirmation()
    {
        pendingAction = ConfirmationAction.None;
        pendingSlotId = 0;
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }

    private void HandleConfirm()
    {
        ConfirmationAction action = pendingAction;
        int slotId = pendingSlotId;
        if (action == ConfirmationAction.Overwrite)
            PerformSave(slotId);
        else if (action == ConfirmationAction.Delete)
            PerformDelete(slotId);
        else
            HideConfirmation();
    }

    private void BindSlotViews()
    {
        if (slotViews == null)
            return;

        for (int i = 0; i < slotViews.Length; i++)
        {
            if (slotViews[i] != null)
                slotViews[i].Bind(this, i + 1);
        }
    }

    private void AddListeners()
    {
        if (openSaveButton != null)
            openSaveButton.onClick.AddListener(OpenSaveMenu);
        if (openLoadButton != null)
            openLoadButton.onClick.AddListener(OpenLoadMenu);
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseMenu);
        if (confirmButton != null)
            confirmButton.onClick.AddListener(HandleConfirm);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(HideConfirmation);
    }

    private void RemoveListeners()
    {
        if (openSaveButton != null)
            openSaveButton.onClick.RemoveListener(OpenSaveMenu);
        if (openLoadButton != null)
            openLoadButton.onClick.RemoveListener(OpenLoadMenu);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseMenu);
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(HandleConfirm);
        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(HideConfirmation);
    }

    private void SetPanelState(bool visible)
    {
        if (mainPanel != null)
            mainPanel.SetActive(visible);
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message ?? string.Empty;
    }

    private void RestoreTimeScale()
    {
        if (!ownsPause)
            return;

        Time.timeScale = previousTimeScale;
        ownsPause = false;
    }
}
