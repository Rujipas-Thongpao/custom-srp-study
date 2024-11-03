using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CeilingMovement : MonoBehaviour
{
    public float Xamplitude = 30f; // Maximum swing angle in degrees
    public float Xfrequency = 1f;  // Speed of the swing
    public float Yamplitude = 30f; // Maximum swing angle in degrees
    public float Yfrequency = 1f;  // Speed of the swing

    private float startAngle;

    void Start()
    {
        startAngle = transform.rotation.eulerAngles.z;
    }

    void Update()
    {
        // Calculate new rotation angle using a sine wave for smooth oscillation
        float angleX = startAngle + Xamplitude * Mathf.Sin(Time.time * Xfrequency);
        float angleY = startAngle + Yamplitude * Mathf.Sin(Time.time * Yfrequency);
        transform.rotation = Quaternion.Euler(angleX, 0f, angleY);
    }
}
