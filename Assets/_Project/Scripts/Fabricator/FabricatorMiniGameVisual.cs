using System;
using System.Collections.Generic;
using UnityEngine;

// Draws the puzzle in 3D with GPU instancing: bevelled dirt/rock voxels, glossy liquid blobs and a glowing mould.
// Whatever reaches the mould turns to oil, a hint of what the seeds really are.
public class FabricatorMiniGameVisual : MonoBehaviour
{
    [SerializeField] private FabricatorMiniGame miniGame;
    [SerializeField] private Camera puzzleCamera;
    [SerializeField] private Mesh voxelMesh;
    [SerializeField] private Mesh blobMesh;
    [SerializeField] private Mesh mouldMesh;
    [SerializeField] private Material dirtMaterial;
    [SerializeField] private Material rockMaterial;
    [SerializeField] private Material mouldMaterial;
    [SerializeField] private Material[] liquidMaterialArray;
    [SerializeField] private Material oilMaterial;
    [Tooltip("Blobs are drawn bigger than their physics circle so neighbours merge into one liquid.")]
    [SerializeField] private float blobVisualScale = 1.45f;

    private readonly List<Matrix4x4> dirtMatrixList = new List<Matrix4x4>();
    private readonly List<Matrix4x4> rockMatrixList = new List<Matrix4x4>();
    private readonly List<Matrix4x4> blobMatrixList = new List<Matrix4x4>();
    private readonly List<Matrix4x4> oilMatrixList = new List<Matrix4x4>();
    private RenderParams dirtParams, rockParams, mouldParams, oilParams;
    private RenderParams[] liquidParamsArray;
    private bool isVisible;

    private void Awake()
    {
        dirtParams = Params(dirtMaterial);
        rockParams = Params(rockMaterial);
        mouldParams = Params(mouldMaterial);
        oilParams = Params(oilMaterial);
        liquidParamsArray = new RenderParams[liquidMaterialArray.Length];
        for (int i = 0; i < liquidMaterialArray.Length; i++) liquidParamsArray[i] = Params(liquidMaterialArray[i]);
    }

    private void Start()
    {
        miniGame.OnBuilt += MiniGame_OnTilesChanged;
        miniGame.OnDug += MiniGame_OnTilesChanged;
        miniGame.OnEnded += MiniGame_OnEnded;
    }

    private void Update()
    {
        if (!isVisible) return;
        DrawInstanced(voxelMesh, dirtParams, dirtMatrixList);
        DrawInstanced(voxelMesh, rockParams, rockMatrixList);

        Rect mould = miniGame.GetMouldRect();
        // mouldMesh is a unit box with its pivot at the bottom centre
        Vector3 mouldSize = new Vector3(mould.width, mould.height, 1.1f);
        Graphics.RenderMesh(mouldParams, mouldMesh, 0, Matrix4x4.TRS(new Vector3(mould.center.x, mould.yMin, transform.position.z), Quaternion.identity, mouldSize));

        float diameter = miniGame.GetParticleRadius() * 2f * blobVisualScale;
        Vector3 offset = -blobMesh.bounds.center * diameter / blobMesh.bounds.size.x;
        // oil sits just in front of the mould's tinted glass (the camera looks along +z) so it reads dark, not washed out
        Vector3 oilOffset = offset + Vector3.back * (mouldSize.z * .5f + diameter * .5f);
        oilMatrixList.Clear();
        for (int i = 0; i < FabricatorMiniGame.LIQUID_COUNT; i++)
        {
            blobMatrixList.Clear();
            foreach (Rigidbody2D body in miniGame.GetParticleList(i))
            {
                Vector3 position = body.transform.position;
                Vector3 scale = Vector3.one * diameter / blobMesh.bounds.size.x;
                if (mould.Contains(body.position)) oilMatrixList.Add(Matrix4x4.TRS(position + oilOffset, Quaternion.identity, scale));
                else blobMatrixList.Add(Matrix4x4.TRS(position + offset, Quaternion.identity, scale));
            }
            DrawInstanced(blobMesh, liquidParamsArray[i], blobMatrixList);
        }
        DrawInstanced(blobMesh, oilParams, oilMatrixList);
    }

    private void MiniGame_OnTilesChanged(object sender, EventArgs e)
    {
        isVisible = true;
        dirtMatrixList.Clear();
        rockMatrixList.Clear();
        UnityEngine.Tilemaps.Tilemap tilemap = miniGame.GetTilemap();
        Vector3 cellSize = tilemap.layoutGrid.cellSize;
        Vector3 voxelScale = new Vector3(cellSize.x / voxelMesh.bounds.size.x, cellSize.y / voxelMesh.bounds.size.y, 1f);
        foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
        {
            bool isDirt = miniGame.IsDirt(cell);
            if (!isDirt && !miniGame.IsRock(cell)) continue;
            Vector3 centre = tilemap.GetCellCenterWorld(cell);
            Matrix4x4 matrix = Matrix4x4.TRS(centre - Vector3.Scale(voxelMesh.bounds.center, voxelScale), Quaternion.identity, voxelScale);
            (isDirt ? dirtMatrixList : rockMatrixList).Add(matrix);
        }
    }

    private void MiniGame_OnEnded(object sender, EventArgs e) => isVisible = false;

    private RenderParams Params(Material material)
    {
        return new RenderParams(material) { layer = gameObject.layer, camera = puzzleCamera, receiveShadows = true, shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On };
    }

    private static void DrawInstanced(Mesh mesh, RenderParams renderParams, List<Matrix4x4> matrixList)
    {
        if (matrixList.Count > 0) Graphics.RenderMeshInstanced(renderParams, mesh, 0, matrixList);
    }
}
