using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeMat : MonoBehaviour
{
    public Material[] materials;
    public string[] tags;
    private Material[][] originalMaterials;
    private GameObject[][] groups;
    [HideInInspector]
    public bool isCoroutineRunning = false;
    public float ChangedSeconds = 10f;

    private void Start()
    {
        originalMaterials = new Material[tags.Length][];
        groups = new GameObject[tags.Length][];

        for (int i = 0; i < tags.Length; i++)
        {
            groups[i] = GameObject.FindGameObjectsWithTag(tags[i]);

            originalMaterials[i] = new Material[groups[i].Length];
            for (int j = 0; j < groups[i].Length; j++)
            {
                originalMaterials[i][j] = groups[i][j].GetComponent<MeshRenderer>().material;
            }
        }
    }
    public void Usechange()
    {
        StartCoroutine(ChangeAndRevert());
    }
    IEnumerator ChangeAndRevert()
    {
        if (groups.Length > 0 && groups[0].Length > 0)
        {
            MeshRenderer meshRenderer = groups[0][0].GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = true;
            }
        }


        for (int i = 0; i < groups.Length; i++)
        {
            for (int j = (i == 0 ? 1 : 0); j < groups[i].Length; j++)
            {
                groups[i][j].GetComponent<MeshRenderer>().material = materials[i];
                if (i == 3)
                {
                    groups[i][j].GetComponent<MeshRenderer>().enabled = true;
                }
            }
        }

        yield return new WaitForSeconds(ChangedSeconds);


        if (groups.Length > 0 && groups[0].Length > 0)
        {
            MeshRenderer meshRenderer = groups[0][0].GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
        }


        for (int i = 0; i < groups.Length; i++)
        {
            for (int j = (i == 0 ? 1 : 0); j < groups[i].Length; j++)
            {
                groups[i][j].GetComponent<MeshRenderer>().material = originalMaterials[i][j];
                if (i == 3)
                {
                    groups[i][j].GetComponent<MeshRenderer>().enabled = false;
                }
            }
        }

        isCoroutineRunning = false;
    }

}
