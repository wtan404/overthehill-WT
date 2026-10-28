using System;
using UnityEngine;

namespace OverTheHillVR;

/// <summary>
/// Universal interface abstracting VR runtime engines (OpenVR / SteamVR and OpenXR).
/// </summary>
public interface IVrBackend : IDisposable
{
    /// <summary>Display name of the active VR runtime (e.g. "SteamVR (OpenVR)" or "Native OpenXR").</summary>
    string Name { get; }

    /// <summary>Whether the runtime is currently initialized, active, and tracking.</summary>
    bool IsActive { get; }

    /// <summary>Initializes connection to the runtime compositor/session.</summary>
    bool Initialize(ref string errorMessage);

    /// <summary>Gracefully shuts down the session and releases swapchains/handles.</summary>
    void Shutdown();

    /// <summary>Polls current tracking poses for the HMD and optional motion controllers.</summary>
    bool PollPoses(
        out Vector3 hmdPosition, out Quaternion hmdRotation,
        out Vector3 leftHandPos, out Quaternion leftHandRot,
        out Vector3 rightHandPos, out Quaternion rightHandRot);

    /// <summary>Calculates perspective projection matrix and IPD eye offset for the given eye (0 = Left, 1 = Right).</summary>
    void GetEyeProjection(int eye, float nearClip, float farClip, out Matrix4x4 projectionMatrix, out Vector3 eyeOffset);

    /// <summary>Queries recommended per-eye render target resolution from the runtime.</summary>
    Vector2Int GetRecommendedEyeResolution();

    /// <summary>Signals beginning of the frame render loop (e.g. xrWaitFrame / xrBeginFrame).</summary>
    bool BeginFrame();

    /// <summary>Submits stereoscopic eye textures to the compositor/swapchain.</summary>
    bool SubmitFrame(RenderTexture leftEye, RenderTexture rightEye, bool flipY);

    /// <summary>Recenters seated zero pose / reference space origin.</summary>
    void Recenter();

    /// <summary>Submits or updates floating quad overlay (HUD, speedometer, menus).</summary>
    void UpdateOverlay(Texture overlayTexture, Matrix4x4 overlayTransform, bool visible);
}
