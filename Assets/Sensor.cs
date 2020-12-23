using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition.Attributes;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Linq;
//using System.Runtime.Remoting.Messaging;
using UnityTemplateProjects;
using UnityEngine.Profiling;
using UnityEditor.Recorder;
using System;
using UnityEditor;

public class Sensor : MonoBehaviour
{
    
    public string screenshotPath;
    public int height;
    public int width;
    public Camera cam;
    
    public Vector4 intrinsics;

    public ComputeShader computeShader;
    public ComputeShader computeShaderNormals;
    public ComputeShader computeShaderGamma;

    public Material locPosMat;

    public SimpleCameraController simpleCameraController;



    public float zNear;

    public float zFar;


    public bool autocaptureMode = false;

    private CameraVolume[] volumes;

    private Texture2D readback;

    private RenderTexture tempRT_depth;
    private RenderTexture tempRT_normals;


    private RenderTexture result_depth;
    private RenderTexture result_normals;
    private RenderTexture result_rgb;

    private RTHandle readbackHandle;

    void OnEnable()
    {
        // as per: https://answers.unity.com/questions/1545858/onpostrender-is-not-called.html
        RenderPipelineManager.endCameraRendering += RenderPipelineManager_endCameraRendering;
    }
    void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= RenderPipelineManager_endCameraRendering;
    }
    private void RenderPipelineManager_endCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        OnPostRender();
    }
    // Start is called before the first frame update
    void Start()
    {


        SetupCamera(cam,intrinsics,width, height);

        volumes = FindObjectsOfType<CameraVolume>();
        
        readback = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
        
        tempRT_depth = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBFloat,
            RenderTextureReadWrite.Default);
        tempRT_normals = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBFloat, //ARGB32
            RenderTextureReadWrite.Default);


        result_depth =
            new RenderTexture(width, height, 24, RenderTextureFormat.RFloat, RenderTextureReadWrite.Default)
            {
                enableRandomWrite = true
            };
        result_normals =
            new RenderTexture(width, height, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default)
            {
                enableRandomWrite = true
            };

        result_rgb =
            new RenderTexture(width, height, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default)
            {
                enableRandomWrite = true
            };

        readbackHandle = RTHandles.Alloc(width, height,1,DepthBits.None,GraphicsFormat.R32G32B32A32_SFloat);



        SetupAOV();
    }

    void SetupCamera(Camera cam, Vector4 intrinsics,int width,int height)
    {
        float aspect = (float)width / height;
        cam.aspect = aspect;
        //field of view in degree
        cam.fieldOfView = Mathf.Rad2Deg * Mathf.Atan((float) height * 0.5f / intrinsics.z)*2.0f;
        
        //cam.fieldOfView;
    }



    //private int countdown = 30;
    // Update is called once per frame
    bool firstFrame = true;

    void Update()
    {
        //return;
        if (firstFrame)
        {
            firstFrame = false;
            return;
        }

        CollectFrameNow();

        //find random position in volume!!!

        float volume = 0.0f;
        for (int i = 0; i < volumes.Length; i++)
        {
            volume += volumes[i].GetVolume();
        }

        float r = UnityEngine.Random.value * volume;

        volume = 0.0f;
        for (int i = 0; i < volumes.Length; i++)
        {
            volume += volumes[i].GetVolume();
            if (r <= volume)
            {
                transform.position = volumes[i].GetRandomPos();
                transform.rotation = UnityEngine.Random.rotation;
                transform.rotation =
                    Quaternion.AngleAxis(UnityEngine.Random.Range(-180.0f, 180.0f), Vector3.up) * //direction we are viewing in
                    Quaternion.AngleAxis(UnityEngine.Random.Range(-60.0f, 60.0f), Vector3.left) * //tilt
                    Quaternion.AngleAxis(UnityEngine.Random.Range(-30.0f, 30.0f), Vector3.forward);//roll
                break;
            }
        }
    }
    public void OnPostRender()
    {

        print("i shit you not!! we stored images, did we!?");


        //return;

        //HDAdditionalCameraData add = camL.GetComponent<HDAdditionalCameraData>()

    }

    void OnDestroy()
    {
        ClearAOV();
    }


    void StoreAs(RenderTexture rt, string filename, bool exr)
    {
        //return;
        int width = rt.width;
        int height = rt.height;
        RenderTexture old = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        StoreAs(screenShot, filename, exr);
        RenderTexture.active = old;
        Destroy(screenShot);
    }

    
    void CheckFloatValues(RenderTexture rt)
    {
        int width = rt.width;
        int height = rt.height;
        
        RenderTexture old = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        Color[] pix = screenShot.GetPixels();
        print(pix[height * width / 2]);
        //screenShot.
        //StoreAs(screenShot, filename, exr);
        RenderTexture.active = old;
        Destroy(screenShot);
    }
    void CheckFloatValues(Texture2D t)
    {
        int width = t.width;
        int height = t.height;
        Color[] pix = t.GetPixels();
        print(pix[height * width / 2].r);
        
        float z_b = pix[height * width / 2].r;
        float z_n = 1.0f - 2.0f * z_b;//2.0f * z_b - 1.0f;
        float z_e = 2.0f * zNear * zFar / (zFar + zNear - z_n * (zFar - zNear));
        print(z_e);
        
    }

    
    
    void StoreAs(Texture2D tex, string filename, bool exr)
    {
        //return;
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

    public int count = 0;
    public int stop_count = 2500;


    void CollectFrameNow()
    {
        TransformColor(cam.targetTexture, result_rgb);
        StoreAs(result_rgb, screenshotPath + "/" + count + ".png", false);
        //StoreAs(cam.targetTexture, screenshotPath + "/" + count + ".png",false);

        CaptureAndStoreDepth();

        count++;

        if(count > stop_count) {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }


    }
    
    void SetupAOV()// maybe ref RTHandle readbackTextureHandle
    {
        HDAdditionalCameraData hdacd = cam.GetComponent<HDAdditionalCameraData>();
        //hdacd.

        /***SETUP AOV ***/
        var pipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;

        //Texture2D readbackTexture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);



        /*
        RenderTexture m_TempRT = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBFloat,
            RenderTextureReadWrite.Default);
        */
        var aovRequest = new AOVRequest(AOVRequest.NewDefault());

        aovRequest.SetFullscreenOutput(DebugFullScreen.Depth);
        //aovRequest.SetFullscreenOutput(MaterialSharedProperty.Depth);

        readbackHandle = readbackHandle ?? RTHandles.Alloc(cam.targetTexture.width, cam.targetTexture.height);

        //AOVBuffers.DepthStencil;
        //AOVGType.Depth;
        var aovRequestBuilder = new AOVRequestBuilder();
        aovRequestBuilder.Add(aovRequest,
            bufferId => readbackHandle,
            null,
            new[] { AOVBuffers.DepthStencil },// , AOVBuffers.Normals }, //TODO: figure out why adding the normals here also destroys depth!!!!
            (cmd, textures, properties) =>
            {
                if (tempRT_depth != null)
                {
                    cmd.Blit(textures[0], tempRT_depth);
                }
            });


        //add request for normals
        aovRequest = new AOVRequest(AOVRequest.NewDefault());
        aovRequest.SetFullscreenOutput(MaterialSharedProperty.Normal);
        aovRequestBuilder.Add(aovRequest,
            bufferId => readbackHandle,
            null,
            new[] { AOVBuffers.Color },//TODO: figure out how this is done in the AOV recorder. Results here look quite different!!
            (cmd, textures, properties) =>
            {
                if (tempRT_normals != null)
                {
                    cmd.Blit(textures[0], tempRT_normals);
                }
            });

        var aovRequestDataCollection = aovRequestBuilder.Build();
        var previousRequests = hdacd.aovRequests;
        //print("wtf");

        if (previousRequests != null && previousRequests.Any())
        {
            print("we have to add a new request to an existing list!!!");
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
            print("completely new AOV request");
            hdacd.SetAOVRequests(aovRequestDataCollection);
        }
    }

    void CaptureAndStoreDepth()
    {

        //return;
        Profiler.BeginSample("SimonRecorder.EncodeImage");

        //cam.Render();

        RenderTexture.active = tempRT_depth; // outputRT
        readback.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
        readback.Apply();
        RenderTexture.active = null;
        TransformDepth(readback, result_depth);
        StoreAs(result_depth, screenshotPath + "/" + count +  "_depth.exr", true);


        RenderTexture.active = tempRT_normals; // outputRT
        readback.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
        readback.Apply();
        RenderTexture.active = null;
        //TODO: fix normals
        Matrix4x4 w2c = cam.worldToCameraMatrix;
        TransformNormals(readback, result_normals, w2c);

        StoreAs(result_normals, screenshotPath + "/" + count + "_normals.exr", true);

        Profiler.EndSample();
    }

    void ClearAOV()
    {
        HDAdditionalCameraData hdacd = cam.GetComponent<HDAdditionalCameraData>();
        hdacd.SetAOVRequests(null);
    }

    
    void TransformDepth(Texture2D pos, RenderTexture result)
    {
        int kernelHandle = computeShader.FindKernel("CSMain");
        result.Create();
        //print("projC" + intrinsicsLR);
        //print("projP" + intrinsicsP);
        computeShader.SetVector("res",new Vector4(pos.width, pos.height, 0, 0));
        computeShader.SetFloat("zNear", zNear);
        computeShader.SetFloat("zFar",zFar);
        computeShader.SetTexture(kernelHandle,"pos",pos);
        computeShader.SetTexture(kernelHandle, "gt", result);
        
        computeShader.Dispatch(kernelHandle, pos.width/8 + 1, pos.height/8 + 1, 1);

        //StoreAs(result,screenshotPath + "/" + count +"_result.png",false);
        //return result;

    }
    //TODO: transform normals!!!
    void TransformNormals(Texture2D n, RenderTexture result, Matrix4x4 mat)
    {
        //print(mat);
        int kernelHandle = computeShaderNormals.FindKernel("CSMain");
        result.Create();
        //print("projC" + intrinsicsLR);
        //print("projP" + intrinsicsP);
        computeShaderNormals.SetVector("res", new Vector4(n.width, n.height, 0, 0));
        computeShaderNormals.SetMatrix("mat", mat);
        computeShaderNormals.SetTexture(kernelHandle, "n", n);
        computeShaderNormals.SetTexture(kernelHandle, "gt", result);

        computeShaderNormals.Dispatch(kernelHandle, n.width / 8 + 1, n.height / 8 + 1, 1);

        //StoreAs(result,screenshotPath + "/" + count +"_result.png",false);
        //return result;

    }

    void TransformColor(RenderTexture rgb, RenderTexture result)
    {
        int kernelHandle = computeShaderGamma.FindKernel("CSMain");
        result.Create();
        //print("projC" + intrinsicsLR);
        //print("projP" + intrinsicsP);
        computeShaderGamma.SetVector("res", new Vector4(rgb.width, rgb.height, 0, 0));
        computeShaderGamma.SetTexture(kernelHandle, "rgb_in", rgb);
        computeShaderGamma.SetTexture(kernelHandle, "rgb_out", result);
        computeShaderGamma.Dispatch(kernelHandle, rgb.width / 8 + 1, rgb.height / 8 + 1, 1);

    }
}
