using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA;


public class SpatialMapping : MonoBehaviour
{
    public static SpatialMapping Instance;
    // Used by the GazeCursor as a property with the RayCast call
    internal static int PhysicsRaycastMask;
    // The layer to use for spatial mapping collisions 
    internal int physicsLayer = 31;
    // Creates environment colliders to work with Physics 
    private MeshCollider spatialMappingCollider;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Initialize and configure the collider
        // TODO: fix the spatialMappingCollider
        //spatialMappingCollider = gameObject.GetComponent<MeshCollider>();
        //spatialMappingCollider.surfaceParent = this.gameObject;
        //spatialMappingCollider.freezeUpdates = false;
        //spatialMappingCollider.layer = physicsLayer;
        // spatialMappingCollider.enabled = true;

        // define the mask
        PhysicsRaycastMask = 1 << physicsLayer;

        // set the object as active one
        gameObject.SetActive(true);

    }

}
