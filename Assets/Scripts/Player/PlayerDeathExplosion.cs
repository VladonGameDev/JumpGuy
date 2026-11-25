using UnityEngine;
using System.Collections;

public class PlayerDeathExplosion : MonoBehaviour
{
    [Header("References")]
    public GameObject explosionPrefab;

    [Header("Settings")]
    public float hideDelay = 0.15f;

    public void PlayExplosion()
    {
        if (explosionPrefab != null)
        {
            CapsuleCollider collider = GetComponent<CapsuleCollider>();
            Vector3 spawnPos = transform.position;

            if (collider != null)
            {
                spawnPos = transform.TransformPoint(collider.center);
            }

            Instantiate(explosionPrefab, spawnPos, Quaternion.identity);
        }

        StartCoroutine(HideModelRoutine());
    }

    IEnumerator HideModelRoutine()
    {
        yield return new WaitForSeconds(hideDelay);

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }
    }
}