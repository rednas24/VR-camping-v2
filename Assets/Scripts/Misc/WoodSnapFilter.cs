using UnityEngine;
using Oculus.Interaction;

public class WoodSnapFilter : MonoBehaviour, IGameObjectFilter
{
    public enum AcceptedWood
    {
        Logs,
        Firewood
    }

    [Tooltip("Logs: whole and half logs, which still need chopping (anything with a WoodLog).\n" +
             "Firewood: wood that is done being chopped (anything with a FirewoodPiece).")]
    [SerializeField] private AcceptedWood accepts = AcceptedWood.Logs;

    // Called with the snap interactor on the piece of wood, which may be a child of it.
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
