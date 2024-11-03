using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour
{
    private Vector3 offset;
    [SerializeField] private GameObject player;
    [SerializeField] private float speed;

    void Awake()
    {
        offset = player.transform.position - this.transform.position;
    }

    void FixedUpdate()
    {
        Vector3 dest = player.transform.position - offset;
        Vector3 curr = this.transform.position;
        this.transform.position = Vector3.Lerp(dest, curr, speed * Time.deltaTime);
    }
}
