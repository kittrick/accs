using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class turntable : MonoBehaviour
{
    public Vector3 rotationAngle;

    // Update is called once per frame
    void Update()
    {
        Vector3 newRotaiton = rotationAngle * Time.time;
        transform.rotation = Quaternion.Euler(newRotaiton.x, newRotaiton.y, newRotaiton.z);
    }
}
