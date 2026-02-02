using UnityEngine;

public class RopeConnector : MonoBehaviour
{
    public static void Connect(Transform a, Transform b, Material ropeMaterial)
    {
        GameObject ropeObj = new GameObject("RopeConnector");
        var rope = ropeObj.AddComponent<RopeMeshGenerator>();
        rope.startPoint = a;
        rope.endPoint = b;

        var renderer = ropeObj.GetComponent<MeshRenderer>();
        renderer.material = ropeMaterial;
    }
}
