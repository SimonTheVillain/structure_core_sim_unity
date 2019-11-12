using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraVolume : MonoBehaviour
{
    private BoxCollider collider;
    // Start is called before the first frame update
    void Start()
    {
        collider = GetComponent<BoxCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Vector3 GetRandomPos()
    {
        Vector3 pos = 
            new Vector3(Random.value * collider.size.x ,
                Random.value * collider.size.y,
                Random.value * collider.size.z) * 0.5f
            + collider.center;
        pos = transform.TransformPoint(pos);
        
        return pos; //Random.value
    }

    public float GetVolume()
    {
        return collider.extents.sqrMagnitude;
    }
}
