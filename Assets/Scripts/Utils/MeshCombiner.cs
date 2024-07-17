using System.Collections.Generic;
using UnityEngine;

public class MeshCombiner : MonoBehaviour
{
    public GameObject CombineMeshesRecursive(GameObject rootObject)
    {
        // Dictionary to hold the CombineInstances grouped by material
        Dictionary<Material, List<CombineInstance>> materialToCombineInstances = new Dictionary<Material, List<CombineInstance>>();
        
        // List to keep track of the original MeshRenderers
        List<MeshRenderer> originalMeshRenderers = new List<MeshRenderer>();

        // Recursively find all MeshRenderers excluding those with tag "Animals" and "Water"
        MeshFilter[] meshFilters = rootObject.GetComponentsInChildren<MeshFilter>(true);
        
        foreach (var meshFilter in meshFilters)
        {
            //if (!meshFilter.gameObject.CompareTag("Animals") && !meshFilter.gameObject.CompareTag("Water"))
            if (!meshFilter.gameObject.CompareTag("Animals"))
            {
                MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
                if (meshRenderer != null && meshRenderer.sharedMaterials != null)
                {
                    Material[] materials = meshRenderer.sharedMaterials;

                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material material = materials[i];

                        if (material == null) continue;

                        if (!materialToCombineInstances.ContainsKey(material))
                        {
                            materialToCombineInstances[material] = new List<CombineInstance>();
                        }

                        CombineInstance combineInstance = new CombineInstance
                        {
                            mesh = meshFilter.sharedMesh,
                            transform = meshFilter.transform.localToWorldMatrix,
                            subMeshIndex = i // Make sure to use the correct submesh index
                        };

                        materialToCombineInstances[material].Add(combineInstance);
                    }

                    // Add the original MeshRenderer to the list for later deactivation
                    originalMeshRenderers.Add(meshRenderer);
                }
            }
        }

        // Create a new GameObject to hold the combined meshes
        GameObject combinedObject = new GameObject("CombinedMesh");
        combinedObject.isStatic = true;

        // For each material, create a mesh and a corresponding MeshRenderer
        foreach (var kvp in materialToCombineInstances)
        {
            Material material = kvp.Key;
            List<CombineInstance> combineInstances = kvp.Value;

            Mesh combinedMesh = new Mesh
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 // Use UInt32 index format to support large vertex counts
            };
            combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);

            GameObject subObject = new GameObject("CombinedMesh_" + material.name);
            subObject.transform.SetParent(combinedObject.transform, false);
            subObject.transform.localPosition = Vector3.zero;
            subObject.isStatic = true; // Mark the sub-object as static

            MeshFilter combinedMeshFilter = subObject.AddComponent<MeshFilter>();
            combinedMeshFilter.mesh = combinedMesh;

            MeshRenderer combinedRenderer = subObject.AddComponent<MeshRenderer>();
            combinedRenderer.sharedMaterial = material;

            // Disable shadows
            combinedRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            combinedRenderer.receiveShadows = false;
        }

        // Position the combined object at (0, 0, 0) and parent it to the root object's parent
        combinedObject.transform.position = Vector3.zero;
        combinedObject.transform.SetParent(rootObject.transform.parent, false);

        // Deactivate the original MeshRenderers
        foreach (var meshRenderer in originalMeshRenderers)
        {
            meshRenderer.enabled = false;
        }
        return combinedObject;
    }
}
