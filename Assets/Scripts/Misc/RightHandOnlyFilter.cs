using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;

/// <summary>
/// Add this to the object, then drag it into the "Interactor Filters" list
/// of the grab interactable. Distance grab is limited to the allowed hand;
/// near (direct) grabbing is left open to both hands.
/// </summary>
public class RightHandOnlyFilter : MonoBehaviour, IGameObjectFilter
{
    [SerializeField] private Handedness allowedHand = Handedness.Right;

    // Receives the GameObject of the interactor trying to interact.
    public bool Filter(GameObject interactorObject)
    {
        // Only distance grabbing is restricted to one hand.
        if (interactorObject.TryGetComponent(out DistanceHandGrabInteractor distanceInteractor))
            return distanceInteractor.Hand.Handedness == allowedHand;

        // Near grab, controllers, anything else: both hands allowed.
        return true;
    }
}