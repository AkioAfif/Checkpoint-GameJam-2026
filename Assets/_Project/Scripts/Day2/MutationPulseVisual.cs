using System;
using System.Collections.Generic;
using UnityEngine;

// The night's hook: for a moment the new palms throb and flicker to a raw, fleshy colour, then look normal again.
public class MutationPulseVisual : MonoBehaviour
{
    [SerializeField] private Day2Director director;
    [Tooltip("Only palms grown from this crop twitch.")]
    [SerializeField] private CropDataSO mutantCropDataSO;
    [SerializeField] private FarmPlot[] farmPlotArray;
    [SerializeField] private Material pulseMaterial;
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private float flickerInterval = .09f;
    [SerializeField] private float throbScale = .12f;
    [SerializeField] private float throbFrequency = 14f;

    private readonly List<Transform> palmList = new List<Transform>();
    private readonly List<MeshRenderer> rendererList = new List<MeshRenderer>();
    private readonly List<Material[]> originalMaterialsList = new List<Material[]>();
    private float timer = -1f;
    private bool isShowingPulse;

    private void Start()
    {
        director.OnCropsPulsed += Director_OnCropsPulsed;
    }

    private void Update()
    {
        if (timer < 0f) return;
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            timer = -1f;
            SetPulseMaterial(false);
            foreach (Transform palm in palmList) palm.localScale = Vector3.one;
            return;
        }
        SetPulseMaterial(Mathf.FloorToInt(timer / flickerInterval) % 2 == 0);
        float throb = 1f + Mathf.Abs(Mathf.Sin(timer * throbFrequency)) * throbScale;
        foreach (Transform palm in palmList) palm.localScale = new Vector3(throb, 1f / throb, throb);
    }

    private void Director_OnCropsPulsed(object sender, EventArgs e)
    {
        palmList.Clear();
        rendererList.Clear();
        originalMaterialsList.Clear();
        foreach (FarmPlot farmPlot in farmPlotArray)
        {
            if (farmPlot.GetState() != FarmPlot.State.Grown || farmPlot.GetPlantedCropDataSO() != mutantCropDataSO) continue;
            // shortcut: reaches into the plot prefab's layout (Visual/SaplingParent, see InteractablePrefabBuilder.FarmPlot); fine while only this hook needs it
            Transform saplingParent = farmPlot.transform.Find("Visual/SaplingParent");
            palmList.Add(saplingParent);
            foreach (MeshRenderer meshRenderer in saplingParent.GetComponentsInChildren<MeshRenderer>())
            {
                rendererList.Add(meshRenderer);
                originalMaterialsList.Add(meshRenderer.sharedMaterials);
            }
        }
        timer = 0f;
    }

    private void SetPulseMaterial(bool isPulse)
    {
        if (isPulse == isShowingPulse) return;
        isShowingPulse = isPulse;
        for (int i = 0; i < rendererList.Count; i++)
        {
            Material[] materials = originalMaterialsList[i];
            if (isPulse)
            {
                materials = new Material[materials.Length];
                for (int m = 0; m < materials.Length; m++) materials[m] = pulseMaterial;
            }
            rendererList[i].sharedMaterials = materials;
        }
    }
}
