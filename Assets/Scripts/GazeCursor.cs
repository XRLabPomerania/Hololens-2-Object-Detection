using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Sets up the cursor in the correct position in real space by using SpatialMappingCollider class 
/// </summary>
public class GazeCursor : MonoBehaviour
{
    // The cursor mesh renderer
    private MeshRenderer meshRenderer;
    public cast_ray cast_ray;
    public ImageCapture imgCap;

    private void Awake()
    {
        cast_ray = gameObject.GetComponent<cast_ray>();
        SceneOrganiser.instance.castRay = cast_ray;
    }
    void Start()
    {
        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        SceneOrganiser.instance.cursor = gameObject;
        gameObject.GetComponent<Renderer>().material.color = Color.green;
        gameObject.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 headPosition = Camera.main.transform.position;
        Vector3 gazeDirection = Camera.main.transform.forward;

        RaycastHit gazeHitInfo;
        if (Physics.Raycast(headPosition, gazeDirection, out gazeHitInfo, 30.0f, SpatialMapping.PhysicsRaycastMask))
        {
            meshRenderer.enabled = true;
            transform.position = gazeHitInfo.point;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, gazeHitInfo.normal);
        }
        else
        {
            meshRenderer.enabled = false;
        }
    }

    public void StartAnalysis()
    {
        CustomVisionAnalyzer vision = SceneOrganiser.instance.GetComponent<CustomVisionAnalyzer>();
        //vision.StartAnalysis();
    }

}
