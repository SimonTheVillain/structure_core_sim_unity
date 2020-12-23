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

public class StructureCore : MonoBehaviour
{
    [System.Serializable]
    public class CamCollector
    {
        public void Setup(float zNear, float zFar)
        {

            cam.farClipPlane = zFar;
            cam.nearClipPlane = zNear;
            SetupCamFov();
            int width = res.x;
            int height = res.y;
            readback = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
            readbackHandle = RTHandles.Alloc(width, height, 1, DepthBits.None, GraphicsFormat.R32G32B32A32_SFloat);

            tempRT_depth = new RenderTexture(res.x, res.y, 24, RenderTextureFormat.ARGBFloat,
                 RenderTextureReadWrite.Default);
            tempRT_normals = new RenderTexture(res.x, res.y, 24, RenderTextureFormat.ARGBFloat, //ARGB32
                RenderTextureReadWrite.Default);

            rtCam =
                new RenderTexture(width, height, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default)
                {
                    enableRandomWrite = true
                };
            cam.targetTexture = rtCam;

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
            SetupAOV();
           
        }

        public void SetupCamFov()
        {
            float aspect = (float)res.x / (float)res.y;
            cam.aspect = aspect;
            //field of view in degree
            cam.fieldOfView = Mathf.Rad2Deg * Mathf.Atan((float)res.y * 0.5f / f) * 2.0f;

        }
        public void Clear()
        {
            HDAdditionalCameraData hdacd = cam.GetComponent<HDAdditionalCameraData>();
            hdacd.SetAOVRequests(null);
        }
        public void SetupAOV()
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
        public Texture2D CaptureDepth()
        {
            //TODO: find out how this still is failing here!!!!
            RenderTexture.active = tempRT_depth; // outputRT
            //print(tempRT_depth.width + " " + tempRT_depth.height);
            readback.ReadPixels(new Rect(0, 0, res.x, res.y), 0, 0, false);
            readback.Apply();
            RenderTexture.active = null;
            //TransformDepth(readback, result_depth);
            //StoreAs(result_depth, screenshotPath + "/" + count + "_depth.exr", true);
            return readback;
        }

        public RenderTexture CaptureColor()
        {
            TransformColor(cam.targetTexture, result_rgb);
            return result_rgb;
        }
        public RenderTexture CaptureNormals()
        {
            RenderTexture.active = tempRT_normals; // outputRT
            readback.ReadPixels(new Rect(0, 0, res.x, res.y), 0, 0, false);
            readback.Apply();
            RenderTexture.active = null;
            //TODO: fix normals
            Matrix4x4 w2c = cam.worldToCameraMatrix;
            TransformNormals(readback, result_normals, w2c);
            return result_normals;
        }
        public void TransformNormals(Texture2D n, RenderTexture result, Matrix4x4 mat)
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

        private RenderTexture rtCam;
        private RenderTexture tempRT_depth;
        private RenderTexture tempRT_normals;
        private RTHandle readbackHandle;

        private RenderTexture result_depth, result_normals, result_rgb;
        public Camera cam;
        private RenderTexture rt;
        private Texture2D readback;
        public Vector2Int res;
        //for structure core: (cxyfxy)
        //resolution 1216x896
        //604., 457., 1.1154399414062500e+03, 1.1154399414062500e+03
        public float f;
        public ComputeShader computeShaderNormals;
        public ComputeShader computeShaderGamma;
        //public Vector4 fxycxy;
    };
    public CamCollector camLeft;
    public CamCollector camRight;
    private CamCollector camProjector; // that one is needed for some occlusion estimation
    public Camera cameraProjector;
    public float focalProjector=850*2;
    public int resProjector = 1024*2;
    public string screenshotPath;



    public ComputeShader computeShaderGroundtruth;

    public float zNear;
    public float zFar;
    public float cutoff = 20.0f;
    public int count = 0;
    public int stop_count = 100000;



    public int heightIR;
    public int widthIR;
    public int heightProj;
    public int widthProj;
    public Camera camL;
    public Camera camR;
    public Camera camP;

    public Light projector;
    public Texture2D patternTexture;
    public Texture2D maskTexture;

    public float projectorIntensitySpecles = 1200000;
    public float projectorIntensityMask = 600000;

    public Vector4 intrinsicsLR;
    public Vector4 intrinsicsP;

    public ComputeShader computeShader;

    public Material locPosMat;






    public Rect patternRect;



    public bool autocaptureMode = false;

    private CameraVolume[] volumes;

    private RenderTexture resultGt;

    // Start is called before the first frame update
    void Start()
    {

        camLeft.Setup(zNear, zFar);
        camRight.Setup(zNear, zFar);
        camProjector = new CamCollector();
        camProjector.cam = cameraProjector;
        camProjector.f = focalProjector;
        camProjector.res = new Vector2Int(resProjector, resProjector);
        camProjector.Setup(zNear, zFar);
        SetupProjector();
        volumes = FindObjectsOfType<CameraVolume>();

        resultGt =
            new RenderTexture(widthIR, heightIR, 24, RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Default)
            {
                enableRandomWrite = true
            };

        /*
        SetupCamera(camL,intrinsicsLR,widthIR,heightIR);
        SetupCamera(camR,intrinsicsLR,widthIR,heightIR);
        SetupCamera(camP,intrinsicsP,widthProj,heightProj);
        SetupProjector(projector,intrinsicsP,widthProj,heightProj);
        



        resultGt = 
            new RenderTexture(widthIR, heightIR, 24, RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Default)
            {
                enableRandomWrite = true
            };
        readbackL = new Texture2D(widthIR, heightIR, TextureFormat.RGBAFloat, false);
        readbackR = new Texture2D(widthIR, heightIR, TextureFormat.RGBAFloat, false);
        readbackP = new Texture2D(widthProj, heightProj, TextureFormat.RGBAFloat, false);
        
        tempRT_I = new RenderTexture(widthIR, heightIR, 24, RenderTextureFormat.ARGBFloat,
            RenderTextureReadWrite.Default);
        tempRT_P = new RenderTexture(widthProj, heightProj, 24, RenderTextureFormat.ARGBFloat,
            RenderTextureReadWrite.Default);
        
        readbackLHandle = RTHandles.Alloc(widthIR, heightIR,1,DepthBits.None,GraphicsFormat.R32G32B32A32_SFloat);
        readbackRHandle = RTHandles.Alloc(widthIR, heightIR,1,DepthBits.None,GraphicsFormat.R32G32B32A32_SFloat);
        readbackPHandle = RTHandles.Alloc(widthProj, heightProj,1,DepthBits.None,GraphicsFormat.R32G32B32A32_SFloat);
    */
    }
    /*
    void SetupCamera(Camera cam, Vector4 intrinsics,int width,int height)
    {
        float aspect = (float)width / height;
        cam.aspect = aspect;
        //field of view in degree
        cam.fieldOfView = Mathf.Rad2Deg * Mathf.Atan((float) height * 0.5f / intrinsics.z)*2.0f;

        //cam.fieldOfView;
    }

    void SetupProjector(Light light, Vector4 intrinsics,int width,int height)
    {
        simpleCameraController = GetComponent<SimpleCameraController>();
        light.spotAngle = Mathf.Rad2Deg * Mathf.Atan((float) height*width/height * 0.5f / intrinsics.z) * 2.0f;// * Mathf.Sqrt(2.0f);
    }
    */
    void SetupProjector()
    {
        projector.cookie = patternTexture;
        projector.intensity = projectorIntensitySpecles;
        projector.spotAngle = Mathf.Rad2Deg * Mathf.Atan((float)resProjector / focalProjector * 0.5f) * 2.0f;// * Mathf.Sqrt(2.0f);
    }



    //private int countdown = 30;
    // Update is called once per frame
    bool firstFrame = true;
    void Update()
    {
        if (firstFrame)
        {
            firstFrame = false;
            return;
        }
        /*
        countdown--;
        if (countdown == 0)
        {
            StartCoroutine(CollectFrames());
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            print("this is it! we capture something");
            StartCoroutine(CollectFrames());
        }
        */

        //return;
        CollectFrameNow();

        //HDAdditionalCameraData add = camL.GetComponent<HDAdditionalCameraData>()
       

        float volume = 0.0f;
        for (int i = 0; i < volumes.Length; i++)
        {
            volume += volumes[i].GetVolume();
        }

        float r = Random.value * volume;

        volume = 0.0f;
        for (int i = 0; i < volumes.Length; i++)
        {
            volume += volumes[i].GetVolume();
            if (r <= volume)
            {
                transform.position = volumes[i].GetRandomPos();
                transform.rotation = Random.rotation;
                break;
            }
        }
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

    //TODO: remove this as it is just to check out if some of this stuff really is working
    /*
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
    */

    
    
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


    void CollectFrameNow()
    {

        RenderTexture color = camLeft.CaptureColor();
        StoreAs(color, screenshotPath + "/" + count + "_left.png", false);
       
        RenderTexture normals = camLeft.CaptureNormals();
        StoreAs(normals, screenshotPath + "/" + count + "_left_n.png", false);
        Texture2D depthLeft = camLeft.CaptureDepth();


        color = camRight.CaptureColor();
        StoreAs(color, screenshotPath + "/" + count + "_right.png", false);
        normals = camRight.CaptureNormals();
        StoreAs(normals, screenshotPath + "/" + count + "_right_n.png", false);
        Texture2D depthRight = camRight.CaptureDepth();

        Texture2D depthProjector = camProjector.CaptureDepth();
        StoreAs(depthLeft, screenshotPath + "/" + count + "_left_debug.exr", true);

        
        Matrix4x4 poseLtoP = //transform from camera to projector
            camProjector.cam.transform.worldToLocalMatrix * 
            camLeft.cam.transform.localToWorldMatrix;
        GenGT(depthLeft, depthProjector,
            camLeft.f,
            camProjector.f,
            poseLtoP,
            resultGt);
        StoreAs(resultGt, screenshotPath + "/" + count + "_left_gt.exr", true);
        
        Matrix4x4 poseRtoP =
            camProjector.cam.transform.worldToLocalMatrix * 
            camRight.cam.transform.localToWorldMatrix;
        GenGT(depthRight, depthProjector,
            camRight.f,
            camProjector.f,
            poseRtoP,
            resultGt);
        StoreAs(resultGt, screenshotPath + "/" + count + "_right_gt.exr", true);

        


        //TODO: generate groundtruth and such!!!!
        //GenGT();

        //store the rendertextures of each camera.
        //StoreAs(camL.targetTexture,screenshotPath + "/" + count + "_l.png",false);
        //StoreAs(camR.targetTexture,screenshotPath + "/" + count + "_r.png",false);
        /*
        StoreAs(camL.targetTexture,screenshotPath + "/" + count + "_l.exr",true);
        StoreAs(camR.targetTexture,screenshotPath + "/" + count + "_r.exr",true);
        StoreAs(camP.targetTexture,screenshotPath + "/" + count + "_p.png",false);
        projector.cookie = maskTexture;
        projector.intensity = projectorIntensityMask;
        camL.Render();
        camR.Render();
        camP.Render();
        StoreAs(camL.targetTexture, screenshotPath + "/" + count + "_l_w.exr", true);
        StoreAs(camR.targetTexture, screenshotPath + "/" + count + "_r_w.exr", true);
        StoreAs(camP.targetTexture, screenshotPath + "/" + count + "_p_w.png", false);
        projector.gameObject.SetActive(false);
        camL.Render();
        camR.Render();
        camP.Render();
        StoreAs(camL.targetTexture, screenshotPath + "/" + count + "_l_wo.exr", true);
        StoreAs(camR.targetTexture, screenshotPath + "/" + count + "_r_wo.exr", true);
        StoreAs(camP.targetTexture, screenshotPath + "/" + count + "_p_wo.png", false);

        //cleanup
        projector.gameObject.SetActive(true);
        projector.cookie = patternTexture;
        projector.intensity = projectorIntensitySpecles;



        //render albedo via AOV
        ReadDepth(camL, widthIR, heightIR,readbackL,tempRT_I,readbackLHandle);
        ReadDepth(camR, widthIR, heightIR,readbackR,tempRT_I,readbackRHandle);
        ReadDepth(camP, widthProj, heightProj,readbackP,tempRT_P,readbackPHandle);
        //StoreAs(test,screenshotPath + "/" + count +"_depth.png",false);
        //CheckFloatValues(posL);
        
        Matrix4x4 poseLtoP = //transform from camera to projector
            camP.transform.worldToLocalMatrix * camL.transform.localToWorldMatrix;
        Matrix4x4 poseLtoR = //transform from camera to projector
            camR.transform.worldToLocalMatrix * camL.transform.localToWorldMatrix;
        GenGT(readbackL, poseLtoP,poseLtoR, readbackP,readbackR,resultGt);
        StoreAs(resultGt,screenshotPath + "/" + count +"_gt_l.exr",true);
        
        Matrix4x4 posRtoP = 
            camP.transform.worldToLocalMatrix * camR.transform.localToWorldMatrix;
        Matrix4x4 posRtoL = 
            camL.transform.worldToLocalMatrix * camR.transform.localToWorldMatrix;
        GenGT(readbackR, posRtoP, posRtoL,readbackP,readbackL,resultGt);
        StoreAs(resultGt,screenshotPath + "/" + count +"_gt_r.exr",true);
        //StoreAs(gt,screenshotPath + "/" + count +"_gt_r.png",false);

        */

        count++;

        if (count > stop_count)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

    }
    
    /*
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
    */


    /*
    void ReadDepth(Camera c,int width,int height,Texture2D readbackTexture,RenderTexture m_TempRT,RTHandle readbackTextureHandle)
    {
        HDAdditionalCameraData hdacd = c.GetComponent<HDAdditionalCameraData>();
        //hdacd.

        /// SETUP AOV
        var pipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;

        //Texture2D readbackTexture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);

        var aovRequest = new AOVRequest(AOVRequest.NewDefault());
        
        aovRequest.SetFullscreenOutput(DebugFullScreen.Depth);
        var aovBuffer = AOVBuffers.DepthStencil;
        //obviously m_colorRT is not set here
        //var bufAlloc = m_ColorRT ?? (m_ColorRT = RTHandles.Alloc(sce.widthIR, sce.heightIR));
        //RTHandleSystem.RTHandle ColorRT = RTHandles.Alloc(width, height,1,DepthBits.None,GraphicsFormat.R32G32B32A32_SFloat);
        RTHandle ColorRT = readbackTextureHandle;

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
        //print("wtf");

        if (previousRequests != null && previousRequests.Any())
        {
            //print("oh shit");
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


        //****************************************************RENDER and READBACK**************

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
        //StoreAs(readbackTexture, screenshotPath + "/testitest.png", false);





        //*****************************************************READ*************



        //**********************************************UNDO SETUP****************************
        //print("removing");
        hdacd.SetAOVRequests(null);
        //c.targetTexture = null;
        //return readbackTexture;

    }
    */
    /*
    void GenGT(Texture2D posI1, Matrix4x4 poseI1toP,Matrix4x4 poseI1toI2,Texture2D posP,Texture2D posI2,RenderTexture result)
    {
        int kernelHandle = computeShader.FindKernel("CSMain");
        result.Create();
        //print("projC" + intrinsicsLR);
        //print("projP" + intrinsicsP);
        computeShader.SetVector("res",new Vector4(posI1.width,posI1.height,posP.width,posP.height));
        computeShader.SetVector("projI",intrinsicsLR);
        computeShader.SetVector("projP",intrinsicsP);
        computeShader.SetVector("patternRect",new Vector4(
            patternRect.xMin,patternRect.yMin,patternRect.width,patternRect.height));
        computeShader.SetMatrix("poseI1toP",poseI1toP);
        computeShader.SetMatrix("poseI1toI2",poseI1toI2);

        computeShader.SetFloat("zNear", zNear);
        computeShader.SetFloat("zFar",zFar);
        computeShader.SetFloat("cutoff",cutoff);
        computeShader.SetTexture(kernelHandle,"posI1",posI1);
        computeShader.SetTexture(kernelHandle,"posI2",posI2);
        computeShader.SetTexture(kernelHandle,"posP",posP);
        computeShader.SetTexture(kernelHandle, "gt", result);
        
        computeShader.Dispatch(kernelHandle, posI1.width/8 + 1, posI1.height/8 + 1, 1);

        //StoreAs(result,screenshotPath + "/" + count +"_result.png",false);
        //return result;

    }
    */

    void GenGT(Texture2D z1, Texture2D zP, 
        float fIR, float fP, 
        Matrix4x4 pose1toP, RenderTexture result)
    {
        Vector2Int resIR = new Vector2Int(z1.width, z1.height);
        Vector2Int resP = new Vector2Int(zP.width, zP.height);
        int kernelHandle = computeShaderGroundtruth.FindKernel("CSMain");
        /*
        print(resIR);
        print(fIR);
        print(resP);
        print(fP);
        print(pose1toP);
        print(zNear);
        print(zFar);
        print(cutoff);
        */
        computeShaderGroundtruth.SetVector("resIR", new Vector2(resIR.x, resIR.y));
        computeShaderGroundtruth.SetFloat("focalIR", fIR);


        computeShaderGroundtruth.SetVector("resP", new Vector2(resP.x, resP.y));
        computeShaderGroundtruth.SetFloat("focalP", fP);

        computeShaderGroundtruth.SetMatrix("pose1toP", pose1toP);

        computeShaderGroundtruth.SetFloat("zNear", zNear);
        computeShaderGroundtruth.SetFloat("zFar", zFar);
        computeShaderGroundtruth.SetFloat("cutoff", cutoff);


        computeShaderGroundtruth.SetTexture(kernelHandle, "z1", z1);
        computeShaderGroundtruth.SetTexture(kernelHandle, "zP", zP);
        computeShaderGroundtruth.SetTexture(kernelHandle, "gt", result);


        computeShaderGroundtruth.Dispatch(kernelHandle, resIR.x / 8 + 1, resIR.y / 8 + 1, 1);


    }

}
