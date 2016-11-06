using UnityEngine;
using System.Collections;

public class FreeCamera : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        float xAxisValue = Input.GetAxis("Horizontal");
        float zAxisValue = Input.GetAxis("Vertical");
        float tiltAroundZ = Input.GetAxis("Horizontal");
        float tiltAroundX = Input.GetAxis("Vertical");
        if (Camera.current != null)
        {
            Camera.current.transform.Translate(new Vector3(xAxisValue, 0.0f, zAxisValue));
            Quaternion target = Quaternion.Euler(tiltAroundX, 0, tiltAroundZ);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime);
        }
    }
}
