using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [SerializeField] private Vector3 rotateSpeed;

    void Update()
    {
        this.transform.Rotate(rotateSpeed);
    }
}
