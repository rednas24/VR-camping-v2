using UnityEngine;

public class AlwaysFullDetail : MonoBehaviour
{
    private void Awake()
    {
        foreach (var lodGroup in GetComponentsInChildren<LODGroup>(true))
            lodGroup.ForceLOD(0);
    }
}