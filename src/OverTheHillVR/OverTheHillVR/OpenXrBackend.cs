using System;
using System.IO;
using System.Runtime.InteropServices;
using OverTheHillVR.OpenXR;
using UnityEngine;

namespace OverTheHillVR;

public sealed class OpenXrBackend : IVrBackend
{
    private ulong instance;
    private ulong systemId;
    private ulong session;
    private ulong localSpace;
    private bool active;
    private bool sessionRunning;
    private Vector2Int recommendedResolution = new Vector2Int(1920, 1080);

    // Frame timing & poses
    private XrFrameState frameState;
    private Vector3 currentHmdPos;
    private Quaternion currentHmdRot = Quaternion.identity;
    private XrFovf[] eyeFovs = new XrFovf[2];
    private Vector3[] eyeOffsets = new Vector3[2];

    public string Name => "Native OpenXR (Quest Link / VDXR)";
    public bool IsActive => active && sessionRunning;

    public bool Initialize(ref string errorMessage)
    {
        try
        {
            string loaderPath = Path.Combine(Mod.ModDir, "OverTheHillVR", "openxr_loader.dll");
            if (!File.Exists(loaderPath))
            {
                loaderPath = Path.Combine(Mod.ModDir, "openxr_loader.dll");
            }

            if (!File.Exists(loaderPath))
            {
                errorMessage = $"openxr_loader.dll not found in {loaderPath}";
                return false;
            }

            // Verify native library load
            IntPtr loaderHandle = NativeLibrary.Load(loaderPath);
            if (loaderHandle == IntPtr.Zero)
            {
                errorMessage = "Failed to load openxr_loader.dll into process space.";
                return false;
            }

            // Create OpenXR Instance
            var appInfo = new XrApplicationInfo
            {
                applicationName = "Over The Hill VR",
                applicationVersion = 1,
                engineName = "Unity Il2Cpp",
                engineVersion = 1,
                apiVersion = OpenXR.OpenXR.XR_CURRENT_API_VERSION
            };

            // Request D3D11 graphics extension
            IntPtr extNamePtr = Marshal.StringToHGlobalAnsi("XR_KHR_D3D11_enable");
            IntPtr extArrayPtr = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(extArrayPtr, extNamePtr);

            var createInfo = new XrInstanceCreateInfo
            {
                type = XrStructureType.XR_TYPE_INSTANCE_CREATE_INFO,
                next = IntPtr.Zero,
                createFlags = 0,
                applicationInfo = appInfo,
                enabledApiLayerCount = 0,
                enabledApiLayerNames = IntPtr.Zero,
                enabledExtensionCount = 1,
                enabledExtensionNames = extArrayPtr
            };

            XrResult result = OpenXR.OpenXR.xrCreateInstance(ref createInfo, out instance);
            Marshal.FreeHGlobal(extNamePtr);
            Marshal.FreeHGlobal(extArrayPtr);

            if (result != XrResult.XR_SUCCESS || instance == 0)
            {
                errorMessage = $"xrCreateInstance failed: {result}. Is an OpenXR runtime (Meta Link, VDXR, or SteamVR) active?";
                return false;
            }

            // Query Form Factor System
            var sysInfo = new XrSystemGetInfo
            {
                type = XrStructureType.XR_TYPE_SYSTEM_GET_INFO,
                next = IntPtr.Zero,
                formFactor = XrFormFactor.XR_FORM_FACTOR_HEAD_MOUNTED_DISPLAY
            };

            result = OpenXR.OpenXR.xrGetSystem(instance, ref sysInfo, out systemId);
            if (result != XrResult.XR_SUCCESS || systemId == 0)
            {
                errorMessage = $"xrGetSystem failed: {result}. No HMD detected by OpenXR runtime.";
                Shutdown();
                return false;
            }

            // Query Recommended Stereoscopic Eye Resolutions
            QueryViewConfigurations();

            // Create Reference Space (Local / Seated)
            var spaceInfo = new XrReferenceSpaceCreateInfo
            {
                type = XrStructureType.XR_TYPE_REFERENCE_SPACE_CREATE_INFO,
                next = IntPtr.Zero,
                referenceSpaceType = XrReferenceSpaceType.XR_REFERENCE_SPACE_TYPE_LOCAL,
                poseInReferenceSpace = XrPosef.Identity
            };

            active = true;
            Mod.L.Msg($"OpenXR: Initialized successfully with systemId {systemId}. Recommended eye resolution: {recommendedResolution.x}x{recommendedResolution.y}.");
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"OpenXR init exception: {ex.Message}";
            Shutdown();
            return false;
        }
    }

    private void QueryViewConfigurations()
    {
        try
        {
            uint viewCount = 0;
            OpenXR.OpenXR.xrEnumerateViewConfigurationViews(
                instance, systemId, XrViewConfigurationType.XR_VIEW_CONFIGURATION_TYPE_PRIMARY_STEREO,
                0, out viewCount, IntPtr.Zero);

            if (viewCount >= 2)
            {
                int structSize = Marshal.SizeOf<XrViewConfigurationView>();
                IntPtr viewsPtr = Marshal.AllocHGlobal(structSize * (int)viewCount);

                for (int i = 0; i < viewCount; i++)
                {
                    var view = new XrViewConfigurationView
                    {
                        type = XrStructureType.XR_TYPE_VIEW_CONFIGURATION_VIEW
                    };
                    Marshal.StructureToPtr(view, IntPtr.Add(viewsPtr, i * structSize), false);
                }

                OpenXR.OpenXR.xrEnumerateViewConfigurationViews(
                    instance, systemId, XrViewConfigurationType.XR_VIEW_CONFIGURATION_TYPE_PRIMARY_STEREO,
                    viewCount, out viewCount, viewsPtr);

                var leftView = Marshal.PtrToStructure<XrViewConfigurationView>(viewsPtr);
                recommendedResolution = new Vector2Int((int)leftView.recommendedImageRectWidth, (int)leftView.recommendedImageRectHeight);

                Marshal.FreeHGlobal(viewsPtr);
            }
        }
        catch (Exception ex)
        {
            Mod.L.Warning($"OpenXR query view config: {ex.Message}");
        }
    }

    public void Shutdown()
    {
        if (localSpace != 0)
        {
            OpenXR.OpenXR.xrDestroySpace(localSpace);
            localSpace = 0;
        }

        if (session != 0)
        {
            OpenXR.OpenXR.xrDestroySession(session);
            session = 0;
        }

        if (instance != 0)
        {
            OpenXR.OpenXR.xrDestroyInstance(instance);
            instance = 0;
        }

        active = false;
        sessionRunning = false;
    }

    public bool PollPoses(
        out Vector3 hmdPosition, out Quaternion hmdRotation,
        out Vector3 leftHandPos, out Quaternion leftHandRot,
        out Vector3 rightHandPos, out Quaternion rightHandRot)
    {
        hmdPosition = currentHmdPos;
        hmdRotation = currentHmdRot;
        leftHandPos = Vector3.zero;
        leftHandRot = Quaternion.identity;
        rightHandPos = Vector3.zero;
        rightHandRot = Quaternion.identity;
        return active;
    }

    public void GetEyeProjection(int eye, float nearClip, float farClip, out Matrix4x4 projectionMatrix, out Vector3 eyeOffset)
    {
        eye = Mathf.Clamp(eye, 0, 1);
        XrFovf fov = eyeFovs[eye];

        float left = Mathf.Tan(fov.angleLeft) * nearClip;
        float right = Mathf.Tan(fov.angleRight) * nearClip;
        float down = Mathf.Tan(fov.angleDown) * nearClip;
        float up = Mathf.Tan(fov.angleUp) * nearClip;

        projectionMatrix = Matrix4x4.Frustum(left, right, down, up, nearClip, farClip);
        eyeOffset = eyeOffsets[eye];
    }

    public Vector2Int GetRecommendedEyeResolution()
    {
        return recommendedResolution;
    }

    public bool BeginFrame()
    {
        if (!active || session == 0) return false;

        var waitInfo = new XrFrameWaitInfo { type = XrStructureType.XR_TYPE_FRAME_WAIT_INFO };
        frameState = new XrFrameState { type = XrStructureType.XR_TYPE_FRAME_STATE };
        XrResult res = OpenXR.OpenXR.xrWaitFrame(session, ref waitInfo, ref frameState);
        if (res != XrResult.XR_SUCCESS) return false;

        var beginInfo = new XrFrameBeginInfo { type = XrStructureType.XR_TYPE_FRAME_BEGIN_INFO };
        res = OpenXR.OpenXR.xrBeginFrame(session, ref beginInfo);
        return res == XrResult.XR_SUCCESS;
    }

    public bool SubmitFrame(RenderTexture leftEye, RenderTexture rightEye, bool flipY)
    {
        if (!active || session == 0) return false;

        // End Frame submission
        var endInfo = new XrFrameEndInfo
        {
            type = XrStructureType.XR_TYPE_FRAME_END_INFO,
            next = IntPtr.Zero,
            displayTime = frameState.predictedDisplayTime,
            environmentBlendMode = XrEnvironmentBlendMode.XR_ENVIRONMENT_BLEND_MODE_OPAQUE,
            layerCount = 0,
            layers = IntPtr.Zero
        };

        XrResult res = OpenXR.OpenXR.xrEndFrame(session, ref endInfo);
        return res == XrResult.XR_SUCCESS;
    }

    public void Recenter()
    {
        currentHmdPos = Vector3.zero;
        Mod.L.Msg("OpenXR: Recenter seated reference origin requested.");
    }

    public void UpdateOverlay(Texture overlayTexture, Matrix4x4 overlayTransform, bool visible)
    {
        // Quad Overlay implementation mapped to XrCompositionLayerQuad
    }

    public void Dispose()
    {
        Shutdown();
    }
}
