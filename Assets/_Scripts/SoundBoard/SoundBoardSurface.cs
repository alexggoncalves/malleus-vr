using UnityEngine;

public class SoundBoardSurface : MonoBehaviour
{
    [SerializeField] private float padding = 0.1f;
    [SerializeField] private Transform panel;

    public Vector2 Size => new(panel.transform.localScale.x, panel.transform.localScale.z);

    public Vector3 Normal => transform.up;

    // Position converting
    public Vector3 LocalToWorld(Vector2 local)
    {
        return transform.TransformPoint(local.x, 0f, local.y);

    }
    public Vector2 WorldToLocal(Vector3 world)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        return new(local.x, local.z);
    }

    // Snap point to the board
    public Vector3 ProjectOntoPlane(Vector3 world) => LocalToWorld(WorldToLocal(world));


    // Get the distance between a point and the board
    public float DistanceFromPlane(Vector3 world) => transform.InverseTransformPoint(world).y;

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
        return LocalToWorld(new(Mathf.Clamp(local.x, -halfX, halfX), Mathf.Clamp(local.y, -halfY, halfY)));
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
        Gizmos.DrawWireCube(Vector3.zero, new(Size.x, 0f, Size.y));

        Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.6f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(
            Mathf.Max(0f, Size.x - padding * 2f), 0f, 
            Mathf.Max(0f, Size.y - padding * 2f)));
    }
}
