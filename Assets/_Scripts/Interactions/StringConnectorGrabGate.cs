using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;

namespace _Scripts.Interactions
{
    public class StringConnectorGrabGate : MonoBehaviour, IActiveState
    {
        [SerializeField] private HandRef handRef;
 
        [SerializeField] private TwoHandStringConnector connector;
    
        public bool Active
        {
            get
            {
                if (!handRef || !handRef.Active) return false;

                // don't let the gesture read as a grab midline creation
                return !connector || !connector.SessionActive;
            }
        }
    }
}
