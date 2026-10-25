using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One material line in the Fabricator recipe panel: icon, name, and needed / owned.
public class FabricatorRecipeSingleUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Color enoughColor = new Color(.3f, .25f, .23f);
    [SerializeField] private Color missingColor = new Color(.75f, .2f, .15f);

    public void SetItem(ItemDataSO itemDataSO, int needed, int owned)
    {
        iconImage.sprite = itemDataSO.icon;
        iconImage.enabled = itemDataSO.icon != null;
        nameText.text = itemDataSO.itemName;
        countText.text = $"{needed} / {owned}";
        countText.color = owned >= needed ? enoughColor : missingColor;
    }
}
