using System;
using System.Collections.Generic;
using UnityEngine;

// Top-left "Today" checklist.
public class TaskUI : MonoBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private TaskSingleUI rowTemplate;
    [SerializeField] private UIPop headerPop;

    private readonly List<TaskSingleUI> rowList = new List<TaskSingleUI>();

    private void Awake()
    {
        rowTemplate.gameObject.SetActive(false);
    }

    private void Start()
    {
        TaskManager.Instance.OnObjectiveProgress += TaskManager_OnObjectiveProgress;
        TaskManager.Instance.OnRequiredTasksCompleted += TaskManager_OnRequiredTasksCompleted;
        TaskManager.Instance.OnObjectivesChanged += TaskManager_OnObjectivesChanged;
        BuildRows();
    }

    private void OnDestroy()
    {
        if (TaskManager.Instance == null) return;
        TaskManager.Instance.OnObjectiveProgress -= TaskManager_OnObjectiveProgress;
        TaskManager.Instance.OnRequiredTasksCompleted -= TaskManager_OnRequiredTasksCompleted;
        TaskManager.Instance.OnObjectivesChanged -= TaskManager_OnObjectivesChanged;
    }

    private void TaskManager_OnObjectiveProgress(object sender, TaskManager.OnObjectiveProgressEventArgs e) => RefreshAll(e.index);

    private void TaskManager_OnRequiredTasksCompleted(object sender, EventArgs e) => headerPop.Play();

    private void TaskManager_OnObjectivesChanged(object sender, EventArgs e)
    {
        foreach (TaskSingleUI row in rowList) Destroy(row.gameObject);
        rowList.Clear();
        BuildRows();
        headerPop.Play();
    }

    private void BuildRows()
    {
        foreach (TaskObjectiveSO objectiveSO in TaskManager.Instance.GetObjectiveSOArray())
        {
            TaskSingleUI row = Instantiate(rowTemplate, container);
            row.gameObject.SetActive(true);
            rowList.Add(row);
        }
        RefreshAll(-1);
    }

    private void RefreshAll(int changedIndex)
    {
        TaskObjectiveSO[] objectiveSOArray = TaskManager.Instance.GetObjectiveSOArray();
        int currentIndex = TaskManager.Instance.GetCurrentIndex();
        for (int i = 0; i < rowList.Count; i++)
        {
            bool isLocked = objectiveSOArray[i].type == TaskObjectiveSO.Type.EnterHouse && !TaskManager.Instance.AreRequiredTasksComplete();
            rowList[i].SetObjective(objectiveSOArray[i], TaskManager.Instance.GetProgress(i), i == currentIndex, isLocked, i == changedIndex);
        }
    }
}
