using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectMove : MonoBehaviour
{
    [SerializeField] private float amp, freq;
    [SerializeField] private Vector3 move;

    private Vector3 initialPos;


    private void Awake()
    {
        initialPos = transform.position;
    }

    private void FixedUpdate()
    {
        this.transform.position = initialPos + amp * Mathf.Sin(Time.time * freq) * move;
    }
}
