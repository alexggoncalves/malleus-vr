using UnityEngine;

namespace _Scripts.SoundBoard
{
    public class SoundBoardSurface : MonoBehaviour
    {
        [SerializeField] private float padding = 0.1f;
        [SerializeField] private BoxCollider panelCollider;

        private Vector2 Size
        {
            get
            {
                Vector3 scale = transform.lossyScale;
                return new Vector2(panelCollider.size.x * scale.x, panelCollider.size.y * scale.y);
            }
        }

        public Vector3 Normal => -transform.forward;

        // Position converting
        public Vector3 LocalToWorld(Vector2 local)
        {
            return transform.TransformPoint(local.x, local.y, 0f);
        }

        public Vector2 WorldToLocal(Vector3 world)
        {
            Vector3 local = transform.InverseTransformPoint(world);
            return new Vector2(local.x, local.y);
        }

        // Snap point to the board
        public Vector3 ProjectOntoPlane(Vector3 world) => LocalToWorld(WorldToLocal(world));


        // Get the distance between a point and the board
        public float DistanceFromPlane(Vector3 world) => transform.InverseTransformPoint(world).z;

        public bool IsInsidePlayableArea(Vector3 world)
        {
            Vector2 local = WorldToLocal(world);
            return Mathf.Abs(local.x) <= Size.x * 0.5f - padding
                   && Mathf.Abs(local.y) <= Size.y * 0.5f - padding;
        }

        public Vector3 ClampToPlayableArea(Vector3 world)
        {
            Vector2 local = WorldToLocal(world);
            float halfX = Mathf.Max(0f, Size.x * 0.5f - padding);
            float halfY = Mathf.Max(0f, Size.y * 0.5f - padding);
            return LocalToWorld(new Vector2(Mathf.Clamp(local.x, -halfX, halfX), Mathf.Clamp(local.y, -halfY, halfY)));
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(Size.x, Size.y, 0f));

            Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.6f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(
                Mathf.Max(0f, Size.x - padding * 2f), Mathf.Max(0f, Size.y - padding * 2f), 0f
            ));
        }
    }
}