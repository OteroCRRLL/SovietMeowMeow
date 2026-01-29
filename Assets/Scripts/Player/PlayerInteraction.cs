using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    public Transform rayOrigin;
    public float interactionDistance = 3f;
    public LayerMask interactableLayer;

    [Header("Input System")]
    public InputAction interactAction;

    private IInteractable currentInteractable;

    private void OnEnable()
    {
        interactAction.Enable();
    }

    private void OnDisable()
    {
        interactAction.Disable();
    }

    void Update()
    {
        HandleRaycast();
        HandleInput();
    }

    void HandleRaycast()
    {
        RaycastHit hit;
        bool hitSomething = Physics.Raycast(rayOrigin.position, rayOrigin.forward, out hit, interactionDistance, interactableLayer);

        Debug.DrawRay(rayOrigin.position, rayOrigin.forward * interactionDistance, Color.red);

        if (hitSomething)
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                if (currentInteractable != interactable)
                {
                    if (currentInteractable != null) currentInteractable.OnHoverExit();

                    currentInteractable = interactable;
                    currentInteractable.OnHoverEnter();
                }
            }
            else
            {
                ClearCurrentInteractable();
            }
        }
        else
        {
            ClearCurrentInteractable();
        }
    }

    void HandleInput()
    {
        if (currentInteractable != null && interactAction.triggered)
        {
            currentInteractable.Interact(this.gameObject);
        }
    }

    void ClearCurrentInteractable()
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnHoverExit();
            currentInteractable = null;
        }
    }
}