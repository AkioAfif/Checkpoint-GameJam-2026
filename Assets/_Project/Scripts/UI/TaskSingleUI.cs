using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskSingleUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Image checkboxImage;
    [SerializeField] private GameObject checkmark;
    [SerializeField] private UIPop pop;
    [SerializeField] private Color currentColor = new Color(.25f, .22f, .2f);
    [SerializeField] private Color doneColor = new Color(.55f, .62f, .5f);
    [SerializeField] private Color lockedColor = new Color(.6f, .58f, .56f);

    public void SetObjective(TaskObjectiveSO objectiveSO, int progress, bool isCurrent, bool isLocked, bool hasChanged)
    {
        bool isDone = progress >= objectiveSO.requiredCount;
        descriptionText.text = isDone ? $"<s>{objectiveSO.description}</s>" : objectiveSO.description;
        descriptionText.color = isDone ? doneColor : isLocked ? lockedColor : currentColor;
        descriptionText.fontStyle = isCurrent ? FontStyles.Bold : FontStyles.Normal;
        countText.text = objectiveSO.requiredCount > 1 ? $"{progress}/{objectiveSO.requiredCount}" : string.Empty;
        countText.color = descriptionText.color;
        checkmark.SetActive(isDone);
        checkboxImage.color = isDone ? doneColor : isCurrent ? new Color(1f, .85f, .45f) : Color.white;
        if (hasChanged) pop.Play();
    }
}
