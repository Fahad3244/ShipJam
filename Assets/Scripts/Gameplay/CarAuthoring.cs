using UnityEngine;
using System.Collections.Generic;

public class CarAuthoring : MonoBehaviour
{
    [Header("Car Setup")]
    public CarType carType;

    [Tooltip("Parent object containing waypoint transforms as children.")]
    public Transform pathRoot;

    // Collect path points from children of pathRoot
    public List<Vector3> GetPathPoints()
    {
        List<Vector3> points = new List<Vector3>();
        if (pathRoot != null)
        {
            foreach (Transform t in pathRoot)
                points.Add(t.position);
        }
        return points;
    }

    // Draw gizmos in scene view for clarity
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);

        if (pathRoot != null)
        {
            Transform prev = null;
            foreach (Transform t in pathRoot)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(t.position, 0.2f);

                if (prev != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(prev.position, t.position);
                }
                prev = t;
            }
        }
    }
}
