using UnityEngine;

public class FieldGizmo : MonoBehaviour
{
    public Transform fieldTransform;
    Vector3 size;
    public void Start()
    {
        size = transform.localScale;
    }
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, size);
    }
}
