using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraZoomController : MonoBehaviour
{
    private Camera cam;



    [Header("Zoom Configuration")]
    [SerializeField] private float zoomSpeed;
    [SerializeField] private float maxZoom;
    [SerializeField] private float minZoom;
    [SerializeField] private float smoothTime;


    void Awake()
    {
        cam = this.GetComponent<Camera>();
    }


    void FixedUpdate()
    {
        float wheel = (Input.GetAxis("Mouse ScrollWheel")) * 10 * zoomSpeed;
        float zoom = Mathf.Clamp(cam.orthographicSize - wheel, maxZoom, minZoom);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, zoom, smoothTime * Time.deltaTime);
    }




}


