using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySingleUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private UIPop pop;

    public void SetItem(ItemDataSO itemDataSO, int count, bool isGain)
    {
        iconImage.sprite = itemDataSO.icon;
        countText.text = count.ToString();
        if (isGain) pop.Play();
    }
}
