#if DEVELOPER_TOOLS || UNITY_EDITOR
using UnityEngine;

// Keys, speeds and test items for the developer playtesting tools.
// Optional asset at Assets/_Project/Resources/DevTools/DevToolsSettings.asset; without it the defaults below are used.
[CreateAssetMenu()]
public class DevToolsSettingsSO : ScriptableObject
{
    private const string RESOURCE_PATH = "DevTools/DevToolsSettings";

    [Header("Shortcuts (checked against the other bindings in GameInput: all F-keys are free)")]
    public KeyCode toggleMenuKey = KeyCode.F1;
    public KeyCode quickSaveKey = KeyCode.F5;
    public KeyCode quickLoadKey = KeyCode.F9;
    public KeyCode skipKey = KeyCode.F6;
    public KeyCode fastForwardKey = KeyCode.F7;
    public KeyCode skipFabricatorKey = KeyCode.F8;

    [Header("Animation")]
    [Tooltip("Speeds F7 cycles through. The first entry is normal speed.")]
    public float[] fastForwardSpeedArray = { 1f, 2f, 4f, 8f };

    [Header("Inventory")]
    [Tooltip("Items the Add Test Item buttons give, one each.")]
    public ItemDataSO[] testItemArray;
    [Tooltip("What Reset Test Inventory sets the bag to.")]
    public ItemAmount[] testInventoryArray;

    public static DevToolsSettingsSO Load()
    {
        DevToolsSettingsSO settings = Resources.Load<DevToolsSettingsSO>(RESOURCE_PATH);
        return settings != null ? settings : CreateInstance<DevToolsSettingsSO>();
    }
}
#endif
