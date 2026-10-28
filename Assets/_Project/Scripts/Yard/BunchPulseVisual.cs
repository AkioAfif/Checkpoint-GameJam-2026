using System.Collections.Generic;
using UnityEngine;

// The evening's hook: the Lv.2 bunch the MC kept, on the porch table, throbs and flickers raw red for a moment while he looks
// away, then looks normal again.
public class BunchPulseVisual : MonoBehaviour
{
    [SerializeField] private YardDayDirector director;
    [SerializeField] private Transform bunch;
    [SerializeField] private Material pulseMaterial;
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private float flickerInterval = .09f;
    [SerializeField] private float throbScale = .12f;
    [SerializeField] private float throbFrequency = 14f;

    private readonly List<Renderer> rendererList = new List<Renderer>();
    private readonly List<Material[]> originalMaterialsList = new List<Material[]>();
    private Vector3 restScale;
    private float timer = -1f;
    private bool isShowingPulse;

    private void Start()
    {
        director.OnBunchPulsed += Director_OnBunchPulsed;
        restScale = bunch.localScale;
        foreach (Renderer part in bunch.GetComponentsInChildren<Renderer>())
        {
            rendererList.Add(part);
            originalMaterialsList.Add(part.sharedMaterials);
        }
    }

    private void Update()
    {
        if (timer < 0f) return;
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            timer = -1f;
            SetPulseMaterial(false);
            bunch.localScale = restScale;
            return;
        }
        SetPulseMaterial(Mathf.FloorToInt(timer / flickerInterval) % 2 == 0);
        bunch.localScale = restScale * (1f + Mathf.Abs(Mathf.Sin(timer * throbFrequency)) * throbScale);
    }

    private void Director_OnBunchPulsed(object sender, System.EventArgs e) => timer = 0f;

    private void SetPulseMaterial(bool isPulse)
    {
        if (isPulse == isShowingPulse) return;
        isShowingPulse = isPulse;
        for (int i = 0; i < rendererList.Count; i++)
        {
            Material[] materialArray = originalMaterialsList[i];
            if (isPulse)
            {
                materialArray = new Material[materialArray.Length];
                for (int m = 0; m < materialArray.Length; m++) materialArray[m] = pulseMaterial;
            }
            rendererList[i].sharedMaterials = materialArray;
        }
    }
}
