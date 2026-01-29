using UnityEngine;

public interface IInteractable
{
    string InteractionPrompt { get; }

    //All interactable objects must have this function
    void Interact(GameObject interactor);

    void OnHoverEnter();
    void OnHoverExit();
}