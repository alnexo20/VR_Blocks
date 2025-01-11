using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;

public class NetworkPlayer : NetworkBehaviour
{
    //First define some global variables in order to speed up the Update() function
    GameObject myXRRig;
    XRController leftController, rightController;
    Transform myXRCam;                  //positions and rotations of controllers and camera
    Transform avHead, avLeft, avRight, avBody;          //avatars moving parts 

    //some fine tuning parameters if needed
    [SerializeField]
    private Vector3 avatarLeftPositionOffset, avatarRightPositionOffset;
    [SerializeField]
    private Quaternion avatarLeftRotationOffset, avatarRightRotationOffset;
    [SerializeField]
    private Vector3 avatarHeadPositionOffset;
    [SerializeField]
    private Quaternion avatarHeadRotationOffset;
    [SerializeField]
    private Vector3 avatarBodyPositionOffset;

    // Start is called before the first frame update
    public override void OnNetworkSpawn()
    {
        var myID = transform.GetComponent<NetworkObject>().NetworkObjectId;
        if (IsOwnedByServer)
            transform.name = "Host:" + myID;    //this must be the host
        else
            transform.name = "Client:" + myID; //this must be the client 

        if (!IsOwner) return;

        myXRRig = GameObject.Find("XR Origin");
        if (myXRRig) Debug.Log("Found XR Rig");
        else Debug.Log("Could not find XR Rig!");

        //pointers to the XR RIg
        leftController = myXRRig.transform.Find("LeftHand").GetComponent<XRController>();
        rightController = myXRRig.transform.Find("RightHand").GetComponent<XRController>();
        myXRCam = myXRRig.transform.Find("Camera Offset/Main Camera").transform;

        //pointers to the avatar
        avLeft = transform.Find("Left Hand");
        avRight = transform.Find("Right Hand");
        avHead = transform.Find("Head");
        avBody = transform.Find("Body");
    }

    void Update()
    {
        if (!IsOwner) return;
        if (!myXRRig) return;

        if (avLeft)
        {
            avLeft.rotation = leftController.transform.rotation * avatarLeftRotationOffset;
            avLeft.position = leftController.transform.position + avatarLeftPositionOffset.x * leftController.transform.right + avatarLeftPositionOffset.y * leftController.transform.up + avatarLeftPositionOffset.z * leftController.transform.forward;
        }

        if (avRight)
        {
            avRight.rotation = rightController.transform.rotation * avatarRightRotationOffset;
            avRight.position = rightController.transform.position + avatarRightPositionOffset.x * rightController.transform.right + avatarRightPositionOffset.y * rightController.transform.up + avatarRightPositionOffset.z * rightController.transform.forward;
        }

        if (avHead)
        {
            avHead.rotation = myXRCam.rotation/* * avatarHeadRotationOffset*/;
            avHead.position = myXRCam.position + avatarHeadPositionOffset.x * myXRCam.transform.right + avatarHeadPositionOffset.y * myXRCam.transform.up + avatarHeadPositionOffset.z * myXRCam.transform.forward;
        }

        if (avBody)
        {
            avBody.position = avHead.position + avatarBodyPositionOffset;
        }
    }
}
