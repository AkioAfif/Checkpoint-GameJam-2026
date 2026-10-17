using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySingleUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private UIPop pop;

    public void SetItem(ItemDataSO itemDataSO, int count, bool isGain)
    {
        iconImage.sprite = itemDataSO.icon;
        countText.text = count.ToString();
        canvasGroup.alpha = count > 0 ? 1f : .35f;
        if (isGain) pop.Play();
    }
}
