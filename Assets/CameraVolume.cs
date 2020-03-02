using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraVolume : MonoBehaviour
{
    private BoxCollider colliderVolume;
    // Start is called before the first frame update
    void Start()
    {
        colliderVolume = GetComponent<BoxCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Vector3 GetRandomPos()
    {
        Vector3 pos = 
            new Vector3(Random.value * colliderVolume.size.x ,
                Random.value * colliderVolume.size.y,
                Random.value * colliderVolume.size.z) * 0.5f
            + colliderVolume.center;
        pos = transform.TransformPoint(pos);
        
        return pos; //Random.value
    }

    public float GetVolume()
    {
        return colliderVolume.size.sqrMagnitude;
    }
}
