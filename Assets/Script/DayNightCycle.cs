using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [SerializeField] Transform light;
    [SerializeField] private float rotateSpeed;

    void Update()
    {
        light.Rotate(new Vector3(1f, 0f, 0f) * rotateSpeed);
    }
}
