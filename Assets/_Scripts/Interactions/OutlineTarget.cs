using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

namespace _Scripts.Interactions
{
    [RequireComponent(typeof(Outline))]
    public class OutlineTarget : MonoBehaviour
    {
        private Outline outline;
        private HandGrabInteractable[] interactables;
    
        public Vector3 Position => transform.position;

        public bool IsSelected()
        {
            foreach (HandGrabInteractable interactable in interactables)
            {
                if (interactable.State == InteractableState.Select) return true;
            }
            return false;
        }

        private void Awake()
        {
            outline = GetComponent<Outline>();
            outline.enabled = false;
            interactables = GetComponentsInChildren<HandGrabInteractable>(true);
        }
    
        private void OnEnable()
        {
            ObjectOutlineHandler.Register(this);
        }
    
        private void OnDisable()
        {
            ObjectOutlineHandler.Unregister(this);
            if (outline) outline.enabled = false;
        }

        public void SetOutline(bool value)
        {
            if (!outline) return;
            outline.enabled = value;
        }
    }
}
