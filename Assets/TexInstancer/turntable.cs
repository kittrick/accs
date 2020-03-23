using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class turntable : MonoBehaviour
{
    public float rotationAngle = 1f;

    // Update is called once per frame
    void Update()
    {
        float currenZrotation = transform.rotation.eulerAngles.y;
        float newRotaiton = rotationAngle * Time.time;
        transform.rotation = Quaternion.Euler(0, newRotaiton, 0);
    }
}
