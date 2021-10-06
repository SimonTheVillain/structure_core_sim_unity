using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureFlipper : MonoBehaviour
{
    public MaterialList materialList;

    MaterialList.RandomizedMaterial[] mats = null;
    int overallProb;
    Renderer renderer;
    Vector2 relativeScale = new Vector2(1, 1);
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        FixLookup();
    }

    public void flip()
    {
        if(mats == null)
        {
            FixLookup();
        }
        int ind = Random.Range(0, overallProb - 1);
        MaterialList.RandomizedMaterial randMat = mats[ind];
        Material mat = new Material(randMat.material);
        //
        float scale = Random.Range(randMat.minMaxScale.x, randMat.minMaxScale.y);
        mat.SetFloat("_InvTilingScale", 1.0f / (scale * relativeScale.x));
        renderer.material = mat;


    }
    void FixLookup()
    {
        renderer = gameObject.GetComponent<Renderer>();

        overallProb = 0;
        foreach (MaterialList.RandomizedMaterial obj in materialList.materials)
        {
            overallProb += obj.relativeProbability;
        }

        mats = new MaterialList.RandomizedMaterial[overallProb];
        overallProb = 0;
        foreach (MaterialList.RandomizedMaterial obj in materialList.materials)
        {
            for (int i = 0; i < obj.relativeProbability; i++)
            {
                mats[overallProb] = obj;
                overallProb++;
            }
        }
    }
}
