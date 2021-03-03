using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingObjectManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public FloatingObjectList objectList;
    public int simultaneousObjects = 7;

    List<GameObject> currentObjects = new List<GameObject>();
    public void Flip()
    {
        // Destroy the old floating objects!
        foreach(GameObject obj in currentObjects)
        {
            Destroy(obj);
        }
        currentObjects.Clear();

        int overallProb = 0;
        foreach(FloatingObjectList.FloatingObject obj in objectList.objects)
        {
            overallProb += obj.relativeProbability;
        }

        FloatingObjectList.FloatingObject[] objs = new FloatingObjectList.FloatingObject[overallProb];
        overallProb = 0;
        foreach (FloatingObjectList.FloatingObject obj in objectList.objects)
        {
            for(int i = 0; i < obj.relativeProbability; i++)
            {
                objs[overallProb] = obj;
                overallProb++;
            }
        }

        for (int i = 0; i < simultaneousObjects; i++)
        {
            int randInd = Random.Range(0, overallProb - 1);
            FloatingObjectList.FloatingObject floatingObj = objs[randInd];
            Vector3 pos = Random.onUnitSphere * Random.Range(floatingObj.minMaxDist.x, floatingObj.minMaxDist.y) +
                gameObject.transform.position;
            Quaternion rot = Random.rotationUniform;
            GameObject go = Instantiate(floatingObj.prefab,pos,rot);
            float scale = Random.Range(floatingObj.minMaxScale.x, floatingObj.minMaxScale.y);
            go.transform.localScale = new Vector3(scale, scale, scale);

            currentObjects.Add(go);
        }
    }
}
