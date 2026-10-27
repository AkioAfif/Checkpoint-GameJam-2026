using UnityEngine;

// Hides the model when picked (unless the node is infinite), plays the float-up, and pops the model back on regrow.
public class ResourceNodeVisual : MonoBehaviour
{
    [SerializeField] private ResourceNode resourceNode;
    [SerializeField] private GameObject modelGameObject;
    [SerializeField] private Transform burstPrefab;
    [SerializeField] private bool hideOnCollect = true;
    [SerializeField] private float regrowDuration = .35f;

    private float regrowTimer = -1f;

    private void Start()
    {
        resourceNode.OnCollected += ResourceNode_OnCollected;
        resourceNode.OnRespawned += ResourceNode_OnRespawned;
        resourceNode.OnRestored += ResourceNode_OnRestored;
    }

    private void Update()
    {
        if (regrowTimer < 0f) return;
        regrowTimer += Time.deltaTime;
        float t = Mathf.Clamp01(regrowTimer / regrowDuration);
        modelGameObject.transform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * .25f) * t;
        if (t >= 1f) regrowTimer = -1f;
    }

    private void ResourceNode_OnCollected(object sender, System.EventArgs e)
    {
        ItemFloatUp.Spawn(resourceNode.GetItemDataSO().prefab, modelGameObject.transform.position, burstPrefab);
        if (hideOnCollect) modelGameObject.SetActive(false);
    }

    private void ResourceNode_OnRespawned(object sender, System.EventArgs e)
    {
        modelGameObject.SetActive(true);
        regrowTimer = 0f;
    }

    private void ResourceNode_OnRestored(object sender, System.EventArgs e)
    {
        modelGameObject.SetActive(!hideOnCollect || resourceNode.IsAvailable());
        regrowTimer = -1f;
    }
}
