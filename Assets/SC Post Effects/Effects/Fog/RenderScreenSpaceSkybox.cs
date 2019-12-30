#if LWRP_5_7_2_OR_NEWER
#define LWRP
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if LWRP
using UnityEngine.Rendering.LWRP;
#if UNITY_2019_3_OR_NEWER
using UnityEngine.Rendering.Universal;
#endif
#endif

namespace SCPE
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class RenderScreenSpaceSkybox : MonoBehaviour
    {
        private Camera thisCam;
        private Camera skyboxCam;

        private RenderTexture skyboxRT;
        private const string RENDER_TAG = "[SCPE] Render skybox to texture";
        private CommandBuffer cmd;
#if LWRP
        private RenderTargetHandle skyboxTexID;
#else
        private int skyboxTexID;
#endif
        private const string texName = "_SkyboxTex";
        private const int downsamples = 2;

        public bool manuallyAdded = true;

        private void OnEnable()
        {
            cmd = new CommandBuffer();
            cmd.name = RENDER_TAG;

            if (!thisCam) thisCam = GetComponent<Camera>();

            if (!skyboxCam) CreateSkyboxCamera();

#if LWRP
            RenderPipelineManager.beginCameraRendering += Render;
#endif
        }

#if LWRP
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= Render;
        }
#endif

#if LWRP
        private void Render(ScriptableRenderContext context, Camera cam)
        {
            //Only execute for this camera
            if (!skyboxCam || cam != thisCam) return;

            CopyCameraSettings(thisCam, skyboxCam);

            cmd = CommandBufferPool.Get(RENDER_TAG);

            cmd.SetGlobalTexture(texName, skyboxCam.targetTexture);

            //Cleanup
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
#else
        private void Update()
        {
            if (thisCam) CopyCameraSettings(thisCam, skyboxCam);
        }
#endif


        private void CreateSkyboxCamera()
        {
            GameObject camObj = new GameObject("Skybox renderer for " + thisCam.name);

            skyboxCam = camObj.AddComponent<Camera>();

            camObj.hideFlags = HideFlags.HideAndDontSave;
            skyboxCam.hideFlags = HideFlags.NotEditable;

            skyboxCam.useOcclusionCulling = false;
            skyboxCam.depth = -100;
            skyboxCam.allowMSAA = false;
            skyboxCam.cullingMask = 0;
            skyboxCam.clearFlags = CameraClearFlags.Skybox;
            skyboxCam.nearClipPlane = 0.01f;
            skyboxCam.farClipPlane = 1;

            CreateSkyboxRT();

#if !LWRP
            skyboxCam.AddCommandBuffer(CameraEvent.AfterSkybox, cmd);     
#endif
            skyboxCam.targetTexture = skyboxRT;
        }

        void CreateSkyboxRT()
        {
            //Use traditional RenderTexture, which has mipmaps
            skyboxRT = new RenderTexture(thisCam.pixelWidth / downsamples, thisCam.pixelHeight / downsamples, 0, RenderTextureFormat.ARGB32);
            skyboxRT.filterMode = FilterMode.Trilinear;
            skyboxRT.useMipMap = true;
            skyboxRT.autoGenerateMips = true;
            skyboxRT.Create();
            cmd.Blit(BuiltinRenderTextureType.CurrentActive, skyboxRT);
            cmd.SetGlobalTexture(texName, skyboxRT);
        }

#if !LWRP
        public void OnDestroy()
        {
            if (skyboxCam)
            {
                skyboxCam.RemoveCommandBuffer(CameraEvent.AfterSkybox, cmd);
            }
        }
#endif

        private static void CopyCameraSettings(Camera src, Camera dest)
        {
            if (dest == null) return;

            dest.transform.position = src.transform.position;
            dest.transform.rotation = src.transform.rotation;

            dest.fieldOfView = src.fieldOfView;
            dest.aspect = src.aspect;

            dest.orthographic = src.orthographic;
            dest.orthographicSize = src.orthographicSize;

            dest.renderingPath = src.renderingPath;
            dest.targetDisplay = src.targetDisplay;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(RenderScreenSpaceSkybox))]
    public class RenderScreenSpaceSkyboxInspector : Editor
    {
        RenderScreenSpaceSkybox script;

        private void OnEnable()
        {
            script = (RenderScreenSpaceSkybox)target;
        }
        override public void OnInspectorGUI()
        {
            if (script.manuallyAdded)
            {
                EditorGUILayout.HelpBox("\nThis script should not be manually added to a camera!\n\nIt is automatically managed by the Fog effect.\n", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox("This script was automatically added by the SCPE Fog effect.", MessageType.Info);

            }
        }
    }
#endif

}
