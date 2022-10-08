/*===============================================================================
Copyright (c) 2019 PTC Inc. All Rights Reserved.

Confidential and Proprietary - Protected under copyright and other laws.
Vuforia is a trademark of PTC Inc., registered in the United States and other 
countries.
===============================================================================*/

#if !UNITY_EDITOR && UNITY_WSA
#define RUNTIME_WSA
#endif

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using Vuforia.Internal.Core;
using Object = UnityEngine.Object;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

#if RUNTIME_WSA && (WINDOWS_XR_ENABLED || OPEN_XR_ENABLED)
using UnityEngine.XR;
#endif

#if RUNTIME_WSA && OPEN_XR_ENABLED
using Microsoft.MixedReality.OpenXR;
using UnityEngine.XR.Management;
#endif

#if PLATFORM_LUMIN && ML_UNITYSDK_ENABLED
using UnityEngine.XR.MagicLeap;
using System.Linq;
#endif 

namespace Vuforia.UnityRuntimeCompiled
{
    public class RuntimeOpenSourceInitializer
    {
        static IUnityRuntimeCompiledFacade sFacade;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnRuntimeMethodLoad()
        {
            InitializeFacade();
        }

        static void InitializeFacade()
        {
            if (sFacade != null) return;
            
            sFacade = new OpenSourceUnityRuntimeCompiledFacade();
            UnityRuntimeCompiledFacade.Instance = sFacade;
        }
        
        class OpenSourceUnityRuntimeCompiledFacade : IUnityRuntimeCompiledFacade
        {
            readonly IUnityRenderPipeline mUnityRenderPipeline = new UnityRenderPipeline();
            readonly IUnityXRBridge mUnityXRBridge = new UnityXRBridge();
            readonly IUnityMagicLeapPrivileges mUnityMagicLeapPrivileges = new UnityMagicLeapPrivileges();

            public IUnityRenderPipeline UnityRenderPipeline
            {
                get { return mUnityRenderPipeline; }
            }

            public IUnityXRBridge UnityXRBridge
            {
                get { return mUnityXRBridge; }
            }
            
            public IUnityMagicLeapPrivileges UnityMagicLeapPrivileges
            {
                get { return mUnityMagicLeapPrivileges; }
            }

            public bool IsUnityUICurrentlySelected()
            {
                return !(EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null);
            }
        }

        class UnityRenderPipeline : IUnityRenderPipeline
        {
            public event Action<Camera[]> BeginFrameRendering;
            public event Action<Camera> BeginCameraRendering;

            public UnityRenderPipeline()
            {
#if UNITY_2018
                UnityEngine.Experimental.Rendering.RenderPipeline.beginFrameRendering += OnBeginFrameRendering;
                UnityEngine.Experimental.Rendering.RenderPipeline.beginCameraRendering += OnBeginCameraRendering;
#else
                UnityEngine.Rendering.RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;
                UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
#endif
            }

#if UNITY_2018
            void OnBeginCameraRendering(Camera camera)
#else
            void OnBeginCameraRendering(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
#endif
            {
                if (BeginCameraRendering != null)
                    BeginCameraRendering(camera);
            }

#if UNITY_2018
            void OnBeginFrameRendering(Camera[] cameras)
#else
            void OnBeginFrameRendering(UnityEngine.Rendering.ScriptableRenderContext context, Camera[] cameras)
#endif
            {
                if (BeginFrameRendering != null)
                    BeginFrameRendering(cameras);
            }
        }

        class UnityXRBridge : IUnityXRBridge
        {
            public UnityXRBridge()
            {
                RegisterCallbacks();
            }

            void RegisterCallbacks()
            {
#if RUNTIME_WSA && OPEN_XR_ENABLED

                var xrSettings = XRGeneralSettings.Instance;
                var xrManager = xrSettings.Manager;
                var xrLoader = xrManager.activeLoader;
                var xrInput = xrLoader.GetLoadedSubsystem<XRInputSubsystem>();

                xrInput.trackingOriginUpdated += TrackingOriginUpdated;
#endif
            }

#pragma warning disable 0067
            public event Action OnTrackingOriginUpdated;
#pragma warning restore 0067

            public bool IsOpenXREnabled()
            {
#if RUNTIME_WSA && OPEN_XR_ENABLED
                return true;
#else
                return false;
#endif
            }

            public IntPtr GetHoloLensSpatialCoordinateSystemPtr()
            {
#if RUNTIME_WSA && OPEN_XR_ENABLED
                // This method returns null for a short amount of time during initialization.
                // On HoloLens we attempt the configuration of the SceneCoordinateSystem until 
                // a non-null value is returned.
                // After initialization, we rely on the XRInputSubsystem.trackingOriginUpdated event
                // to retrieve a valid pointer to the SceneCoordinateSystem.
                var sceneCoordinateSystem = PerceptionInterop.GetSceneCoordinateSystem(Pose.identity);
                if (sceneCoordinateSystem == null)
                {
                    return IntPtr.Zero;
                }
                
                return Marshal.GetIUnknownForObject(sceneCoordinateSystem);
#elif RUNTIME_WSA && WINDOWS_XR_ENABLED
                return UnityEngine.XR.WindowsMR.WindowsMREnvironment.OriginSpatialCoordinateSystem;
#elif RUNTIME_WSA && !UNITY_2020_1_OR_NEWER
                return UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();
#else
                Debug.LogError("Failed to get HoloLens Spatial Coordinate System. " +
                               "Please include the appropriate XR Plugin package into your project.");
                return IntPtr.Zero;
#endif
            }

            public bool IsHolographicDevice()
            {
#if RUNTIME_WSA && (WINDOWS_XR_ENABLED || OPEN_XR_ENABLED)
                var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
                SubsystemManager.GetInstances(xrDisplaySubsystems);

                foreach (var xrDisplay in xrDisplaySubsystems)
                {
                    if (xrDisplay.running && !xrDisplay.displayOpaque)
                        return true;
                }
                return false;
#elif RUNTIME_WSA && !UNITY_2020_1_OR_NEWER
                return XRDevice.isPresent && !UnityEngine.XR.WSA.HolographicSettings.IsDisplayOpaque;
#else
                return false;
#endif
            }

            public void SetFocusPointForFrame(Vector3 position, Vector3 normal)
            {
#if RUNTIME_WSA && (WINDOWS_XR_ENABLED || OPEN_XR_ENABLED)
                var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
                SubsystemManager.GetInstances(xrDisplaySubsystems);

                foreach (var xrDisplay in xrDisplaySubsystems)
                {
                    if (xrDisplay.running && !xrDisplay.displayOpaque)
                    {
                        xrDisplay.SetFocusPlane(position, normal, Vector3.zero);
                        return;
                    }
                }
#endif
            }

#if RUNTIME_WSA && OPEN_XR_ENABLED
            void TrackingOriginUpdated(XRInputSubsystem inputSubsystem)
            {
                OnTrackingOriginUpdated?.Invoke();
            }
#endif
        }

        class UnityMagicLeapPrivileges : IUnityMagicLeapPrivileges
        {
            GameObject mRequesterObject;
            Action<bool> mPrivilegesCallback;

            public void RequestMagicLeapPrivileges(List<string> permissions, Action<bool> callback)
            {
                mPrivilegesCallback = callback;

#if PLATFORM_LUMIN && ML_UNITYSDK_ENABLED
                mRequesterObject = new GameObject("ML Privileges");
                mRequesterObject.hideFlags |= HideFlags.HideAndDontSave;
                Object.DontDestroyOnLoad(mRequesterObject);
                var requester = mRequesterObject.AddComponent<MLPrivilegeRequesterBehavior>();
                requester.OnPrivilegesDone += OnPrivilegesDone;
                requester.Privileges = ToPrivileges(permissions);
#endif
            }

#if PLATFORM_LUMIN && ML_UNITYSDK_ENABLED
            MLPrivileges.RuntimeRequestId[] ToPrivileges(List<string> permissions)
            {
                return permissions.Select(p => ConvertPermission(p)).ToArray();
            }

            MLPrivileges.RuntimeRequestId ConvertPermission(string permission)
            {
                 switch (permission)
                 {
                     case "ComputerVision":
                        return MLPrivileges.RuntimeRequestId.ComputerVision;
                     case "Camera":
                         return MLPrivileges.RuntimeRequestId.CameraCapture;
                     case "Microphone":
                         return MLPrivileges.RuntimeRequestId.AudioCaptureMic;
                     default: 
                         throw new Exception("Unknown permission "+permission+" defined for MagicLeap privilege");
                 }
            }

            void OnPrivilegesDone(MLResult result)
            {
                if (!result.IsOk)
                {
                    Debug.LogErrorFormat("Error: Failed to get all requested privileges, Reason: {0}", result);
                }

                if (mRequesterObject != null)
                {
                    GameObject.Destroy(mRequesterObject);
                }

                mPrivilegesCallback?.Invoke(result.IsOk);
            }
#endif
        }
    }
}
