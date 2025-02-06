using Unity.Netcode;
using UnityEngine;

public class IKTargetFollowVRRig : NetworkBehaviour
{
    [Range(0,1)]
    public float turnSmoothness = 0.1f;

    public Vector3 headBodyPositionOffset;
    public float headBodyYawOffset;
    Transform headTarget;
    Transform leftHandTarget;
    Transform rightHandTarget;
    public Transform HeadIKTarget;
    public Transform LeftIKTarget;
    public Transform RightIKTarget;

    public override void OnNetworkSpawn()
    {
        headTarget = GameObject.FindGameObjectWithTag("HeadVRTarget").GetComponent<Transform>();
        leftHandTarget = GameObject.FindGameObjectWithTag("LeftHandVRTarget").GetComponent<Transform>();
        rightHandTarget = GameObject.FindGameObjectWithTag("RightHandVRTarget").GetComponent<Transform>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = HeadIKTarget.position + headBodyPositionOffset;
        float yaw = headTarget.eulerAngles.y;
        transform.rotation = Quaternion.Lerp(transform.rotation,Quaternion.Euler(transform.eulerAngles.x, yaw, transform.eulerAngles.z),turnSmoothness);

        Map();
    }

    public void Map()
    {
        HeadIKTarget.position = headTarget.TransformPoint(new Vector3(0,0,0));
        HeadIKTarget.rotation = headTarget.rotation * Quaternion.Euler(new Vector3(0,0,0));
        LeftIKTarget.position = leftHandTarget.TransformPoint(new Vector3(0,0,0));
        LeftIKTarget.rotation = leftHandTarget.rotation * Quaternion.Euler(new Vector3(90,0,0));
        RightIKTarget.position = rightHandTarget.TransformPoint(new Vector3(0,0,0));
        RightIKTarget.rotation = rightHandTarget.rotation * Quaternion.Euler(new Vector3(90,0,0));
    }
}
