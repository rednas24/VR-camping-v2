using UnityEngine;
using Oculus.Interaction;

public class WoodSnapFilter : MonoBehaviour, IGameObjectFilter
{
    public enum AcceptedWood
    {
        Logs,
        Firewood
    }
    [SerializeField] private AcceptedWood accepts = AcceptedWood.Logs;

    public bool Filter(GameObject candidate)
    {
        switch (accepts)
        {
            case AcceptedWood.Firewood:
                return candidate.GetComponentInParent<FirewoodPiece>() != null;

            default:
                return candidate.GetComponentInParent<WoodLog>() != null;
        }
    }
}
