using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SaveSlotUI : MonoBehaviour
{
    [SerializeField] private Text slotNameText;
    [SerializeField] private Text detailsText;
    [SerializeField] private Text statusText;
    [SerializeField] private Button actionButton;
    [SerializeField] private Text actionButtonText;
    [SerializeField] private Button deleteButton;

    private SaveLoadMenuController controller;
    private int slotId;

    private void OnEnable()
    {
        if (actionButton != null)
            actionButton.onClick.AddListener(HandleAction);
        if (deleteButton != null)
            deleteButton.onClick.AddListener(HandleDelete);
    }

    private void OnDisable()
    {
        if (actionButton != null)
            actionButton.onClick.RemoveListener(HandleAction);
        if (deleteButton != null)
            deleteButton.onClick.RemoveListener(HandleDelete);
    }

    public void Bind(SaveLoadMenuController owner, int id)
    {
        controller = owner;
        slotId = id;
        if (slotNameText != null)
            slotNameText.text = $"SLOT {slotId}";
    }

    public void Refresh(SaveSlotInfo info, SaveMenuMode mode)
    {
        if (info == null)
            info = new SaveSlotInfo(slotId, SaveSlotStatus.Corrupted, null, "Slot information is unavailable.");

        bool isSaveMode = mode == SaveMenuMode.Save;
        if (actionButtonText != null)
            actionButtonText.text = isSaveMode ? "SAVE" : "LOAD";
        if (actionButton != null)
            actionButton.interactable = isSaveMode || info.CanLoad;
        if (deleteButton != null)
            deleteButton.interactable = info.HasFile;

        switch (info.Status)
        {
            case SaveSlotStatus.Empty:
                SetText("EMPTY", "No save data");
                break;

            case SaveSlotStatus.Valid:
                SetValidMetadata(info.Metadata);
                break;

            case SaveSlotStatus.Incompatible:
                SetText("INCOMPATIBLE", info.Error);
                break;

            default:
                SetText("CORRUPTED", info.Error);
                break;
        }
    }

    private void SetValidMetadata(SaveSlotMetadata metadata)
    {
        if (metadata == null)
        {
            SetText("CORRUPTED", "Metadata is missing.");
            return;
        }

        string savedAt = metadata.savedAtUtc;
        if (DateTime.TryParse(
                metadata.savedAtUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTime timestamp))
        {
            savedAt = timestamp.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
        }

        SetText(
            "READY",
            $"Scene: {metadata.sceneName}\nLevel: {metadata.playerLevel}   HP: {metadata.playerHealth}\nSaved: {savedAt}");
    }

    private void SetText(string status, string details)
    {
        if (statusText != null)
            statusText.text = status;
        if (detailsText != null)
            detailsText.text = string.IsNullOrWhiteSpace(details) ? string.Empty : details;
    }

    private void HandleAction()
    {
        if (controller != null)
            controller.HandleSlotAction(slotId);
    }

    private void HandleDelete()
    {
        if (controller != null)
            controller.RequestDelete(slotId);
    }
}
