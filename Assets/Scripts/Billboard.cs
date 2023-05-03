using UnityEngine;

public class Billboard : MonoBehaviour
{
    public enum PivotAxis { Free, X, Y }
    public PivotAxis pivotAxis = PivotAxis.Free;
    public Camera target;

    private void Update()
    {
        Vector3 forward;
        Vector3 up;

        switch( pivotAxis )
        {
            case PivotAxis.X:
            {
                Vector3 right = transform.right;
                forward = Vector3.ProjectOnPlane(target.transform.forward, right).normalized;
                up = Vector3.Cross(forward, right);
                break;
            }
            case PivotAxis.Y:
            {
                up = transform.up;
                forward = Vector3.ProjectOnPlane(target.transform.forward, up).normalized;
                break;
            }
            case PivotAxis.Free:
            default:
            {
                forward = target.transform.forward;
                up = target.transform.up;
                break;
            }
        }

        transform.rotation = Quaternion.LookRotation(forward, up);
    }
}