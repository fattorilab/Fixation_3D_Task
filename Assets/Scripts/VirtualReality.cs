using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualReality : MonoBehaviour
{
    public float EyeDistanceInCM;
    // Start is called before the first frame update
    void Start()
    {
        Camera camL = GameObject.Find("Left Camera").GetComponent<Camera>();
        Camera camR = GameObject.Find("Right Camera").GetComponent<Camera>();

        camL.transform.localPosition -= new Vector3(EyeDistanceInCM / 200, 0, 0);
        camR.transform.localPosition += new Vector3(EyeDistanceInCM / 200, 0, 0);

    }
}
