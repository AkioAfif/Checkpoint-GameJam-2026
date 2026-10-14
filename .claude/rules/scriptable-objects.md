---
paths:
  - "Assets/Scripts/ScriptableObjects/**"
  - "Assets/ScriptableObjects/**"
---

# ScriptableObjects (data, not behaviour)

## Rules
- Class files go in `Assets/Scripts/ScriptableObjects/`, named `XSO.cs`. Asset files go in `Assets/ScriptableObjects/<Type>/`, named after the content (`Coin.asset`, `Spike_Trap.asset`).
- `[CreateAssetMenu()]` on every SO class that designers create by hand. Leave it off singleton-style lists that exist once (KC `RecipeListSO`) to avoid accidental duplicates. Create those once through the menu, or by script with `ScriptableObject.CreateInstance` + `AssetDatabase.CreateAsset` inside an `Editor/` script or Unity MCP.
- Fields are **public** and lowerCamelCase (KC style, data-only class). No methods with game logic and no `Update`. Small pure helpers are fine (`public float GetDurationMinutes()`).
- **Never write to an SO at runtime.** In the editor the change persists into the asset file. Copy values into the MonoBehaviour instead.
- References between data use SO references, not strings or ids (`public ItemSO input; public ItemSO output;`).
- Identity check = reference equality (`recipe.input == itemSO`). No name/string comparisons.
- Lookups over small arrays use the KC `foreach` + `return null` helper (`GetRecipeSOWithInput`), plus `HasRecipeWithInput` built on top of it. Write the lookup once and reuse it; don't duplicate loops.
- A visual prefab referenced from an SO is typed `Transform` (KC) so `Instantiate` returns a Transform directly.
- Audio: one `AudioClipRefsSO` asset holds every SFX as `AudioClip[]` arrays (random variant per play). Name them `xArray` (e.g. `footstepArray`).

## Templates
```csharp
using UnityEngine;

[CreateAssetMenu()]
public class PickupSO : ScriptableObject
{
    public Transform prefab;
    public Sprite sprite;
    public string pickupName;
    public int scoreValue;
}
```
```csharp
using System.Collections.Generic;
using UnityEngine;

// one asset only → no CreateAssetMenu
public class PickupListSO : ScriptableObject
{
    public List<PickupSO> pickupSOList;
}
```
```csharp
using UnityEngine;

[CreateAssetMenu()]
public class AudioClipRefsSO : ScriptableObject
{
    public AudioClip[] footstepArray;
    public AudioClip[] checkpointArray;
    public AudioClip[] jumpArray;
}
```
After creating an SO class, wait for the recompile before creating its asset. The asset menu entry only appears once the code compiles.
