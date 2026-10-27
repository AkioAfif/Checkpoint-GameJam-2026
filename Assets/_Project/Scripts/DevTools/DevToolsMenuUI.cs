#if DEVELOPER_TOOLS || UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The "Developer Playtesting" menu: sections of tools, each with its shortcut and a status line. Rows that don't apply
// right now are greyed out and say why. Only shows and refreshes; every click calls DevTools.
public class DevToolsMenuUI
{
    private const float TOAST_TIME = 2.5f;
    private const float REFRESH_INTERVAL = .25f;

    private class ToolRow
    {
        public Button button;
        public TextMeshProUGUI label;
        public TextMeshProUGUI status;
        public Func<string> labelText;
        public Func<bool> isEnabled;
        public Func<string> statusText;
    }

    private readonly DevTools devTools;
    private readonly DevToolsSettingsSO settings;
    private readonly List<ToolRow> rowList = new List<ToolRow>();
    private readonly CanvasGroup panelCanvasGroup;
    private readonly CanvasGroup toastCanvasGroup;
    private readonly TextMeshProUGUI toastText;
    private readonly Button firstButton;
    private bool isOpen;
    private float toastTimer;
    private float refreshTimer;

    public DevToolsMenuUI(DevTools devTools, DevToolsSettingsSO settings)
    {
        this.devTools = devTools;
        this.settings = settings;

        Canvas canvas = DevToolsMenuBuilder.CreateCanvas(devTools.transform);
        RectTransform panel = DevToolsMenuBuilder.CreatePanel(canvas.transform);
        panelCanvasGroup = panel.GetComponent<CanvasGroup>();
        DevToolsMenuBuilder.CreateTitle(panel, "Developer Playtesting   [" + settings.toggleMenuKey + "] close");
        firstButton = BuildSections(panel);

        toastText = DevToolsMenuBuilder.CreateToast(canvas.transform, out toastCanvasGroup);
        toastCanvasGroup.alpha = 0f;

        devTools.OnMessage += DevTools_OnMessage;
        SetOpen(false);
    }

    public bool IsOpen() => isOpen;

    public void SetOpen(bool isOpen)
    {
        this.isOpen = isOpen;
        panelCanvasGroup.SetVisible(isOpen);
        if (!isOpen) return;
        RefreshRows();
        firstButton.Select();
    }

    // unscaled time, so the menu keeps working while the game is paused or in slow motion
    public void Tick(float unscaledDeltaTime)
    {
        if (toastTimer > 0f)
        {
            toastTimer -= unscaledDeltaTime;
            toastCanvasGroup.alpha = toastTimer > 0f ? 1f : 0f;
        }
        if (!isOpen) return;
        refreshTimer -= unscaledDeltaTime;
        if (refreshTimer > 0f) return;
        refreshTimer = REFRESH_INTERVAL;
        RefreshRows();
    }

    private void DevTools_OnMessage(object sender, DevTools.OnMessageEventArgs e)
    {
        toastText.text = e.text;
        toastText.color = e.isError ? new Color(1f, .5f, .45f) : Color.white;
        toastTimer = TOAST_TIME;
        toastCanvasGroup.alpha = 1f;
    }

    private void RefreshRows()
    {
        foreach (ToolRow row in rowList) RefreshRow(row);
    }

    // builds the sections; returns the first button, the one that gets focus when the menu opens
    private Button BuildSections(RectTransform panel)
    {
        Button first = null;

        GameObject animation = AddSection(panel, "Animation & Cinematics");
        AddRow(animation, "Skip Cutscene / Dialogue   [" + settings.skipKey + "]", null, devTools.IsCinematicPlaying,
            () => devTools.IsSkipArmed() ? "Skipping..." : devTools.IsCinematicPlaying() || devTools.IsDialogueActive() ? "Cutscene or dialogue playing" : "Nothing to skip right now",
            devTools.SkipCinematic, out first);
        AddRow(animation, "", () => "Fast-Forward: " + devTools.GetFastForwardSpeed() + "x   [" + settings.fastForwardKey + "]", () => true,
            () => "Applies to cutscenes and dialogue only", devTools.FastForward, out _);

        GameObject saveLoad = AddSection(panel, "Save & Load");
        AddRow(saveLoad, "Quick Save   [" + settings.quickSaveKey + "]", null, devTools.IsFreePlay,
            devTools.GetQuickSaveStatus, devTools.QuickSave, out _);
        AddRow(saveLoad, "Quick Load   [" + settings.quickLoadKey + "]", null, devTools.HasQuickSave,
            () => devTools.IsQuickLoadConfirming() ? "Press again to confirm: unsaved progress will be lost" : devTools.GetQuickSaveStatus(),
            devTools.QuickLoad, out _);

        GameObject gameplay = AddSection(panel, "Gameplay");
        AddRow(gameplay, "Skip Fabricator   [" + settings.skipFabricatorKey + "]", null, devTools.IsFabricatorActive,
            devTools.GetFabricatorStatus, devTools.SkipFabricator, out _);
        AddRow(gameplay, "Complete Current Objective", null, devTools.HasObjectives,
            devTools.GetObjectiveStatus, devTools.CompleteObjective, out _);
        AddRow(gameplay, "Reset Current Objective", null, devTools.HasObjectives,
            devTools.GetObjectiveStatus, devTools.ResetObjective, out _);

        GameObject playerWorld = AddSection(panel, "Player & World");
        AddRow(playerWorld, "Reset Player to Spawn", null, () => devTools.HasSpawnPose() && devTools.IsFreePlay(),
            () => devTools.HasSpawnPose() ? "Back to where free play began in this scene" : "Waiting for free play to start",
            devTools.ResetPlayer, out _);
        AddRow(playerWorld, "Reset Current Interaction", null, devTools.IsFabricatorActive,
            devTools.GetFabricatorStatus, devTools.AbortFabricator, out _);

        GameObject inventory = AddSection(panel, "Inventory");
        if (settings.testItemArray != null)
        {
            foreach (ItemDataSO itemDataSO in settings.testItemArray)
            {
                ItemDataSO item = itemDataSO;
                AddRow(inventory, "Add 1 " + item.itemName, null, () => true, null, () => devTools.AddTestItem(item), out _);
            }
        }
        AddRow(inventory, "Reset Test Inventory", null, () => true, () => "Sets the bag to the test baseline", devTools.ResetTestInventory, out _);

        return first;
    }

    private static GameObject AddSection(RectTransform panel, string title)
    {
        Button header = DevToolsMenuBuilder.CreateButton(panel, "- " + title, out TextMeshProUGUI headerLabel, out TextMeshProUGUI headerStatus);
        headerStatus.gameObject.SetActive(false);
        headerLabel.fontStyle = FontStyles.Bold;
        // the collapsible body is a plain container (no listening script), so hiding it is safe
        GameObject content = DevToolsMenuBuilder.CreateColumn(title, panel).gameObject;
        header.onClick.AddListener(() =>
        {
            content.SetActive(!content.activeSelf);
            headerLabel.text = (content.activeSelf ? "- " : "+ ") + title;
        });
        return content;
    }

    private void AddRow(GameObject section, string labelText, Func<string> dynamicLabel, Func<bool> isEnabled, Func<string> statusText, Action onClick, out Button button)
    {
        Button rowButton = DevToolsMenuBuilder.CreateButton(section.transform, labelText, out TextMeshProUGUI label, out TextMeshProUGUI status);
        rowButton.onClick.AddListener(() => onClick());
        ToolRow row = new ToolRow
        {
            button = rowButton,
            label = label,
            status = status,
            labelText = dynamicLabel,
            isEnabled = isEnabled,
            statusText = statusText,
        };
        if (statusText == null) status.gameObject.SetActive(false);
        rowList.Add(row);
        button = rowButton;
        RefreshRow(row);
    }

    private static void RefreshRow(ToolRow row)
    {
        row.button.interactable = row.isEnabled();
        if (row.labelText != null) row.label.text = row.labelText();
        if (row.statusText != null) row.status.text = row.statusText();
    }
}
#endif
