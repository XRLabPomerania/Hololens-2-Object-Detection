using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.WebCam;

public class cast_ray : MonoBehaviour
{
    private GameObject last_target;
    private GameObject last_label;
    public GameObject prefab;
    public int pix_x = 960;
    public int pix_y = 540;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        /*
        if (Input.GetKeyDown(KeyCode.D))
            CastToPixel(pix_x, pix_y, "something");
        if (Input.GetKeyDown(KeyCode.R))
            last_label.transform.rotation *= Quaternion.Euler(10, 10, 10);
        */
    }

    public void CastToPixel(string label_text)
    {
        CastToPixel(960, 540, label_text);
    }
    public void CastToPixel(int pix_x, int pix_y, string label_text)
    {
        Debug.Log("CAST TO PXL CALLED WITH " + pix_x + " " + pix_y);
        float fixed_angle = (Mathf.PI / 180) * 64.69f;
        int width = 1920;
        int hight = 1080;
        
        Vector3 direction = new Vector3(pix_x - (width / 2), pix_y - (hight / 2), (width / 2) / Mathf.Tan(fixed_angle / 2));
        direction.Normalize();
        //from local to global:
        direction = UnityEngine.Camera.main.transform.rotation * direction;
        Vector3 origin = UnityEngine.Camera.main.transform.position;
        RaycastHit hit;
        
        if (Physics.Raycast(origin, direction, out hit))
        {
            Debug.Log("WE GOT A HIT!");
            if (last_target != null)
                Destroy(last_target);
            
            last_target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(last_target.GetComponent<Collider>());
            last_target.transform.position = hit.point;
            last_target.transform.localScale *= 0.05f;
            last_target.GetComponent<Renderer>().material.color = Color.red;

            if (last_label != null)
                Destroy(last_label);
            last_label = Instantiate(prefab, hit.point, Quaternion.identity);

            last_label.GetComponentInChildren<Microsoft.MixedReality.Toolkit.UI.ToolTip>().ToolTipText = label_text;

            //Debug.Log(hit.point);
            //last_label.transform.position = hit.point-0.1f*direction;
            //doesn't work yet

            last_label.transform.rotation = UnityEngine.Camera.main.transform.rotation;
            //Debug.Log(last_label.GetComponent<ScriptableObject>().name);// = lable_text;

        } 

    }
}