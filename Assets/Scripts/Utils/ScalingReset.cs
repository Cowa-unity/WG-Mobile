using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScalingReset : MonoBehaviour
{
    private Vector3 scale;
    private Vector3 parentScale;
    private Vector3 relativeScale;

    void Start()
    {
        scale = transform.localScale;
        parentScale = transform.parent.localScale;
        relativeScale = new Vector3(
            scale.x / parentScale.x,
            scale.y / parentScale.y,
            scale.z / parentScale.z
        );

        if (transform.CompareTag("Trees"))
        {
            SetMeshRenderersActive(transform, false);
        }
    }

    void Update()
    {
        if (transform.parent.localScale != parentScale)
        {
            relativeScale = new Vector3(
                scale.x / transform.parent.localScale.x,
                scale.y / transform.parent.localScale.y,
                scale.z / transform.parent.localScale.z
            );

            transform.localScale = new Vector3(
                parentScale.x * relativeScale.x,
                parentScale.y * relativeScale.y,
                parentScale.z * relativeScale.z
            );

            if (transform.CompareTag("Trees"))
            {
                SetMeshRenderersActive(transform, true);
            }

            Destroy(this);
        }
    }

    void SetMeshRenderersActive(Transform parent, bool isActive)
    {
        MeshRenderer meshRenderer = parent.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = isActive;
        }

        foreach (Transform child in parent)
        {
            SetMeshRenderersActive(child, isActive);
        }
    }
}
