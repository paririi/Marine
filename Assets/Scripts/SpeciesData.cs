using UnityEngine;

[System.Serializable]
public class SpeciesData
{
    public string speciesName;

    [TextArea(3, 8)]
    public string educationalInfo;

    public GameObject prefab;
}