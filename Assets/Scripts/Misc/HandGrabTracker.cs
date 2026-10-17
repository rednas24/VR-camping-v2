using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;

public class HandGrabTracker
{
    private readonly List<IHandGrabInteractor> grabInteractors = new List<IHandGrabInteractor>();

    public HandGrabTracker()
    {
        foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(
                     FindObjectsInactive.Include))
        {
            if (behaviour is IHandGrabInteractor grabInteractor)
                grabInteractors.Add(grabInteractor);
        }
    }

    public bool IsHolding(Handedness handedness)
    {
        foreach (IHandGrabInteractor grabInteractor in grabInteractors)
        {
            // Skip interactors that have been destroyed.
            if (grabInteractor is Object unityObject && unityObject == null)
                continue;

            if (grabInteractor.Hand == null || grabInteractor.Hand.Handedness != handedness)
                continue;

            if (grabInteractor is IInteractorView interactor && interactor.HasSelectedInteractable)
                return true;
        }

        return false;
    }
}
