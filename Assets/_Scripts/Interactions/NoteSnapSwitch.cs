using _Scripts.SoundBoard;
using Oculus.Interaction;
using UnityEngine;

namespace _Scripts.Interactions
{
    public class NoteSnapSwitch : MonoBehaviour
    {
        [SerializeField] private Transform lever;
        [SerializeField] private float onAngle = -25f, offAngle = 25f;
        [SerializeField] private float speed = 14f;
        [SerializeField] private AudioTrigger audioTrigger;
        
        [SerializeField] private Renderer offLight;
        [SerializeField] private Renderer onLight;

        [SerializeField] private Material onMaterial;
        [SerializeField] private Material offMaterial;

        private bool SnapEnabled { get; set; }

        private float _target;

        private void Start()
        {
            SetState(false);
        }

        public void Toggle() => SetState(!SnapEnabled);

        public void SetSnap(bool on)
        {
            if (on == SnapEnabled) return;
            SetState(on);
        }

        private void SetState(bool on)
        {
            SnapEnabled = on;
            _target = on ? onAngle : offAngle;
            
            SoundEngine.Instance.SetChromaticSnap(on);
            
            offLight.material = on ? offMaterial : onMaterial;
            onLight.material = on ? onMaterial : offMaterial;
            
            if (audioTrigger) audioTrigger.PlayAudio();
        }

        private void Update()
        {
            Vector3 current = lever.localEulerAngles;
            float y = Mathf.LerpAngle(current.y, _target, Time.deltaTime * speed);
            lever.localEulerAngles = new Vector3(0, y, 0);
        }
    }
}