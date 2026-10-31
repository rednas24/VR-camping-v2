using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
public class RightHandOnlyFilter : MonoBehaviour, IGameObjectFilter
{
    [SerializeField] private Handedness allowedHand = Handedness.Right;

    public bool Filter(GameObject interactorObject)
    {
        if (interactorObject.TryGetComponent(out DistanceHandGrabInteractor distanceInteractor))
            return distanceInteractor.Hand.Handedness == allowedHand;

        return true;
    }
}