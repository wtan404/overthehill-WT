using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using Valve.VR;

namespace OverTheHillVR;

public sealed class OpenVrBackend : IVrBackend
{
    private CVRSystem sys;
    private CVRCompositor compositor;
    private CVROverlay overlay;
    private ulong overlayHandle;
    private bool active;
    private TrackedDevicePose_t[] poses = new TrackedDevicePose_t[16];

    public string Name => "SteamVR (Valve OpenVR)";
    public bool IsActive => active && sys != null;

    public bool Initialize(ref string errorMessage)
    {
        try
        {
            string openVrPath = Path.Combine(Mod.ModDir, "OverTheHillVR", "openvr_api.dll");
            if (!File.Exists(openVrPath))
            {
                openVrPath = Path.Combine(Mod.ModDir, "openvr_api.dll");
            }

            if (!File.Exists(openVrPath))
            {
                errorMessage = "openvr_api.dll missing. Put it in Mods\\OverTheHillVR\\.";
                return false;
            }

            EVRInitError peError = EVRInitError.None;
            sys = OpenVR.Init(ref peError, EVRApplicationType.VRApplication_Scene);
            if (peError != EVRInitError.None || sys == null)
            {
                errorMessage = $"SteamVR init failed: {peError}. Is SteamVR running with the headset connected?";
                return false;
            }

            compositor = OpenVR.Compositor;
            if (compositor == null)
            {
                errorMessage = "SteamVR compositor unavailable.";
                Shutdown();
                return false;
            }

            overlay = OpenVR.Overlay;
            active = true;
            Mod.L.Msg("OpenVR: Initialized successfully with active SteamVR compositor.");
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"OpenVR init exception: {ex.Message}";
            Shutdown();
            return false;
        }
    }

    public void Shutdown()
    {
        if (overlayHandle != 0 && overlay != null)
        {
            overlay.DestroyOverlay(overlayHandle);
            overlayHandle = 0;
        }

        if (sys != null)
        {
            OpenVR.Shutdown();
            sys = null;
            compositor = null;
            overlay = null;
        }

        active = false;
    }

    public bool PollPoses(
        out Vector3 hmdPosition, out Quaternion hmdRotation,
        out Vector3 leftHandPos, out Quaternion leftHandRot,
        out Vector3 rightHandPos, out Quaternion rightHandRot)
    {
        hmdPosition = Vector3.zero;
        hmdRotation = Quaternion.identity;
        leftHandPos = Vector3.zero;
        leftHandRot = Quaternion.identity;
        rightHandPos = Vector3.zero;
        rightHandRot = Quaternion.identity;

        if (!active || compositor == null) return false;

        compositor.WaitGetPoses(poses, new TrackedDevicePose_t[0]);

        if (poses.Length > 0 && poses[0].bPoseIsValid)
        {
            Matrix4x4 m = VrDriver.ToMatrix4x4(poses[0].mDeviceToAbsoluteTracking);
            hmdPosition = m.GetColumn(3);
            hmdRotation = Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
            return true;
        }

        return false;
    }

    public void GetEyeProjection(int eye, float nearClip, float farClip, out Matrix4x4 projectionMatrix, out Vector3 eyeOffset)
    {
        if (sys == null)
        {
            projectionMatrix = Matrix4x4.identity;
            eyeOffset = Vector3.zero;
            return;
        }

        EVREye evrEye = (eye == 0) ? EVREye.Eye_Left : EVREye.Eye_Right;
        HmdMatrix44_t proj = sys.GetProjectionMatrix(evrEye, nearClip, farClip);
        projectionMatrix = VrDriver.ToMatrix4x4(proj);

        HmdMatrix34_t eyeToHead = sys.GetEyeToHeadTransform(evrEye);
        Matrix4x4 mHead = VrDriver.ToMatrix4x4(eyeToHead);
        eyeOffset = mHead.GetColumn(3);
    }

    public Vector2Int GetRecommendedEyeResolution()
    {
        if (sys == null) return new Vector2Int(1920, 1080);
        uint w = 0, h = 0;
        sys.GetRecommendedRenderTargetSize(ref w, ref h);
        return new Vector2Int((int)w, (int)h);
    }

    public bool BeginFrame()
    {
        return active;
    }

    public bool SubmitFrame(RenderTexture leftEye, RenderTexture rightEye, bool flipY)
    {
        if (!active || compositor == null) return false;

        Texture_t texLeft = new Texture_t
        {
            handle = leftEye.GetNativeTexturePtr(),
            eType = ETextureType.DirectX,
            eColorSpace = EColorSpace.Auto
        };

        VRTextureBounds_t bounds = new VRTextureBounds_t
        {
            uMin = 0f,
            uMax = 1f,
            vMin = flipY ? 1f : 0f,
            vMax = flipY ? 0f : 1f
        };

        EVRCompositorError err1 = compositor.Submit(EVREye.Eye_Left, ref texLeft, ref bounds, EVRSubmitFlags.Submit_Default);

        Texture_t texRight = new Texture_t
        {
            handle = rightEye.GetNativeTexturePtr(),
            eType = ETextureType.DirectX,
            eColorSpace = EColorSpace.Auto
        };

        EVRCompositorError err2 = compositor.Submit(EVREye.Eye_Right, ref texRight, ref bounds, EVRSubmitFlags.Submit_Default);

        return err1 == EVRCompositorError.None && err2 == EVRCompositorError.None;
    }

    public void Recenter()
    {
        if (sys != null)
        {
            sys.ResetSeatedZeroPose();
            Mod.L.Msg("OpenVR: Seated zero pose reset via SteamVR.");
        }
    }

    public void UpdateOverlay(Texture overlayTexture, Matrix4x4 overlayTransform, bool visible)
    {
        if (overlay == null || overlayTexture == null) return;

        if (overlayHandle == 0)
        {
            overlay.CreateOverlay("OTHVR_HUD", "Over The Hill HUD", ref overlayHandle);
        }

        if (overlayHandle != 0)
        {
            if (!visible)
            {
                overlay.HideOverlay(overlayHandle);
                return;
            }

            Texture_t tex = new Texture_t
            {
                handle = overlayTexture.GetNativeTexturePtr(),
                eType = ETextureType.DirectX,
                eColorSpace = EColorSpace.Auto
            };

            overlay.SetOverlayTexture(overlayHandle, ref tex);
            overlay.ShowOverlay(overlayHandle);
        }
    }

    public void Dispose()
    {
        Shutdown();
    }
}
