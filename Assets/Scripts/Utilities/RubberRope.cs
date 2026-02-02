using UnityEngine;
using System.Collections.Generic;

public class RubberRope : MonoBehaviour
{
    public Transform startPoint;         // Assign hole 1
    public Transform endPoint;           // Assign hole 2
    public GameObject ropeSegmentPrefab; // Assign RopeSegment prefab
    public int segmentCount = 12;        // More segments = smoother rope
    public float segmentLength = 0.2f;

    private List<Rigidbody> segments = new List<Rigidbody>();

    void Start()
    {
        BuildRope();
    }

    void BuildRope()
    {
        Vector3 direction = (endPoint.position - startPoint.position).normalized;
        Vector3 currentPos = startPoint.position + direction * segmentLength;

        Rigidbody prevBody = startPoint.GetComponent<Rigidbody>();

        for (int i = 0; i < segmentCount; i++)
        {
            GameObject segment = Instantiate(ropeSegmentPrefab, currentPos, Quaternion.identity);
            Rigidbody rb = segment.GetComponent<Rigidbody>();
            segments.Add(rb);

            ConfigurableJoint joint = segment.GetComponent<ConfigurableJoint>();
            joint.connectedBody = prevBody;

            // ✅ Rubber-like settings
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;
            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Free;

            SoftJointLimit limit = new SoftJointLimit();
            limit.limit = segmentLength * 0.5f; // slight give
            joint.linearLimit = limit;

            JointDrive drive = new JointDrive();
            drive.positionSpring = 50f; // elasticity
            drive.positionDamper = 5f;  // damping
            drive.maximumForce = 100f;
            joint.xDrive = joint.yDrive = joint.zDrive = drive;

            prevBody = rb;
            currentPos += direction * segmentLength;
        }

        // ✅ Attach last segment to endPoint
        FixedJoint endJoint = segments[segments.Count - 1].gameObject.AddComponent<FixedJoint>();
        endJoint.connectedBody = endPoint.GetComponent<Rigidbody>();
    }
}
