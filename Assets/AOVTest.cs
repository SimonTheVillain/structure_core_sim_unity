using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Experimental.Rendering.HDPipeline.Attributes;
using UnityEngine.Rendering;
using System.Linq;

public class AOVTest : MonoBehaviour
{

    public string screenshotPath;
    int width = 640;
    int height = 480;
    public Material locPosMat;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private int i = 30;
    // Update is called once per frame
    void Update()
    {
        i--;
        if (i == 1)
        {
            SetReplacementMaterial();
        }
        if (i == 0)
        {
            Test();
            UnsetReplacementMaterial();
        }
    }
    void Test()
    {
        Camera c = GetComponent<Camera>();
        HDAdditionalCameraData hdacd = GetComponent<HDAdditionalCameraData>();
        //hdacd.

        /***SETUP AOV ***/
        var pipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;

        Texture2D readbackTexture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);




        RenderTexture m_TempRT = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBFloat,
            RenderTextureReadWrite.Default);

        var aovRequest = new AOVRequest(AOVRequest.@default);
        var aovBuffer = AOVBuffers.Color; //hopefully this means we are only rendering albedo
        aovRequest.SetFullscreenOutput(MaterialSharedProperty.Albedo);
        //obviously m_colorRT is not set here
        //var bufAlloc = m_ColorRT ?? (m_ColorRT = RTHandles.Alloc(sce.widthIR, sce.heightIR));
        RTHandleSystem.RTHandle ColorRT = RTHandles.Alloc(width, height);


        var aovRequestBuilder = new AOVRequestBuilder();
        aovRequestBuilder.Add(aovRequest,
            bufferId => ColorRT,
            null,
            new[] { aovBuffer },
            (cmd, textures, properties) =>
            {
                if (m_TempRT != null)
                {
                    cmd.Blit(textures[0], m_TempRT);
                }
            });
        var aovRequestDataCollection = aovRequestBuilder.Build();
        var previousRequests = hdacd.aovRequests;
        print("wtf");

        if (previousRequests != null && previousRequests.Any())
        {
            print("oh shit");
            var listOfRequests = previousRequests.ToList();
            foreach (var p in aovRequestDataCollection)
            {
                listOfRequests.Add(p);
            }
            var allRequests = new AOVRequestDataCollection(listOfRequests);
            hdacd.SetAOVRequests(allRequests);
        }
        else
        {
            hdacd.SetAOVRequests(aovRequestDataCollection);
        }


        /*****************************************************RENDER and READBACK**************/

        print("rendering and storing");
        //sce.irLeft.targetTexture = outputRT;
        Rect old = c.rect;
        c.rect = new Rect(0, 0, 1, 1);
        //SetReplacementMaterial();
        c.Render();
        //UnsetReplacementMaterial();
        c.rect = old;
        //sce.irLeft.targetTexture = null;

        RenderTexture.active = m_TempRT; // outputRT
        readbackTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
        readbackTexture.Apply();
        RenderTexture.active = null;
        StoreAs(readbackTexture, screenshotPath + "/testitest.png", false);





        /******************************************************READ*************/
        /*
        RenderTexture.active = m_TempRT;
        readbackTexture.ReadPixels(new Rect(0, 0, sce.widthIR, sce.heightIR), 0, 0, false);
        readbackTexture.Apply();
        RenderTexture.active = null;


        StoreAs(readbackTexture, screenshotPath + "/testitest.png", false);
        */


        /***********************************************UNDO SETUP****************************/
        //print("removing");
        hdacd.SetAOVRequests(null);
        c.targetTexture = null;

    }

    void StoreAs(RenderTexture rt, string filename, bool exr)
    {
        int width = rt.width;
        int height = rt.height;
        RenderTexture old = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        StoreAs(screenShot, filename, exr);
        RenderTexture.active = old;
    }

    void StoreAs(Texture2D tex, string filename, bool exr)
    {
        byte[] bytes;
        if (exr)
        {
            bytes = tex.EncodeToEXR(Texture2D.EXRFlags.CompressZIP);
        }
        else
        {
            bytes = tex.EncodeToPNG();
        }

        string filepath = filename;
        System.IO.File.WriteAllBytes(filepath, bytes);
        //Debug.Log(string.Format("Took screenshot to: {0}", filename));
    }




    private List<Material> matOrig;
    void SetReplacementMaterial()
    {
        matOrig = new List<Material>();
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        for(int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].materials;
            for (int j = 0; j < renderers[i].materials.Length; j++)
            {
                matOrig.Add(renderers[i].materials[j]);
                materials[j] = locPosMat;
            }

            renderers[i].materials = materials;
        }
    }
    void UnsetReplacementMaterial()
    {
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        int k = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            //renderers[i].material = matOrig[k];
            //k++;
            Material[] materials = renderers[i].materials;
            for (int j = 0; j < renderers[i].materials.Length; j++)
            {
                materials[j] = matOrig[k];
                k++;
            }

            renderers[i].materials = materials;
        }
    }

}
