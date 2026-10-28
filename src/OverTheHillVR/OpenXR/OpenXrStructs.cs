using System;
using System.Runtime.InteropServices;

namespace OverTheHillVR.OpenXR;

[StructLayout(LayoutKind.Sequential)]
public struct XrVector3f
{
    public float x;
    public float y;
    public float z;

    public XrVector3f(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
}

[StructLayout(LayoutKind.Sequential)]
public struct XrQuaternionf
{
    public float x;
    public float y;
    public float z;
    public float w;

    public XrQuaternionf(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
}

[StructLayout(LayoutKind.Sequential)]
public struct XrPosef
{
    public XrQuaternionf orientation;
    public XrVector3f position;

    public static XrPosef Identity => new XrPosef
    {
        orientation = new XrQuaternionf(0, 0, 0, 1),
        position = new XrVector3f(0, 0, 0)
    };
}

[StructLayout(LayoutKind.Sequential)]
public struct XrFovf
{
    public float angleLeft;
    public float angleRight;
    public float angleUp;
    public float angleDown;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrExtent2Di
{
    public int width;
    public int height;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrOffset2Di
{
    public int x;
    public int y;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrRect2Di
{
    public XrOffset2Di offset;
    public XrExtent2Di extent;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct XrApplicationInfo
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string applicationName;
    public uint applicationVersion;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string engineName;
    public uint engineVersion;
    public ulong apiVersion;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrInstanceCreateInfo
{
    public XrStructureType type;
    public IntPtr next;
    public ulong createFlags;
    public XrApplicationInfo applicationInfo;
    public uint enabledApiLayerCount;
    public IntPtr enabledApiLayerNames;
    public uint enabledExtensionCount;
    public IntPtr enabledExtensionNames;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrSystemGetInfo
{
    public XrStructureType type;
    public IntPtr next;
    public XrFormFactor formFactor;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrSessionCreateInfo
{
    public XrStructureType type;
    public IntPtr next;
    public ulong createFlags;
    public ulong systemId;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrSessionBeginInfo
{
    public XrStructureType type;
    public IntPtr next;
    public XrViewConfigurationType primaryViewConfigurationType;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrGraphicsBindingD3D11KHR
{
    public XrStructureType type;
    public IntPtr next;
    public IntPtr device; // ID3D11Device*
}

[StructLayout(LayoutKind.Sequential)]
public struct XrGraphicsRequirementsD3D11KHR
{
    public XrStructureType type;
    public IntPtr next;
    public long adapterLuid;
    public uint minFeatureLevel;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrSwapchainImageD3D11KHR
{
    public XrStructureType type;
    public IntPtr next;
    public IntPtr texture; // ID3D11Texture2D*
}

[StructLayout(LayoutKind.Sequential)]
public struct XrReferenceSpaceCreateInfo
{
    public XrStructureType type;
    public IntPtr next;
    public XrReferenceSpaceType referenceSpaceType;
    public XrPosef poseInReferenceSpace;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrViewConfigurationView
{
    public XrStructureType type;
    public IntPtr next;
    public uint recommendedImageRectWidth;
    public uint maxImageRectWidth;
    public uint recommendedImageRectHeight;
    public uint maxImageRectHeight;
    public uint recommendedSwapchainSampleCount;
    public uint maxSwapchainSampleCount;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrSwapchainCreateInfo
{
    public XrStructureType type;
    public IntPtr next;
    public ulong createFlags;
    public ulong usageFlags;
    public long format;
    public uint sampleCount;
    public uint width;
    public uint height;
    public uint faceCount;
    public uint arraySize;
    public uint mipCount;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrSwapchainImageAcquireInfo
{
    public XrStructureType type;
    public IntPtr next;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrSwapchainImageWaitInfo
{
    public XrStructureType type;
    public IntPtr next;
    public long timeout;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrSwapchainImageReleaseInfo
{
    public XrStructureType type;
    public IntPtr next;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrFrameWaitInfo
{
    public XrStructureType type;
    public IntPtr next;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrFrameState
{
    public XrStructureType type;
    public IntPtr next;
    public long predictedDisplayTime;
    public long predictedDisplayPeriod;
    public bool shouldRender;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrFrameBeginInfo
{
    public XrStructureType type;
    public IntPtr next;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrFrameEndInfo
{
    public XrStructureType type;
    public IntPtr next;
    public long displayTime;
    public XrEnvironmentBlendMode environmentBlendMode;
    public uint layerCount;
    public IntPtr layers; // const XrCompositionLayerBaseHeader* const*
}

[StructLayout(LayoutKind.Sequential)]
public struct XrSwapchainSubImage
{
    public ulong swapchain;
    public XrRect2Di imageRect;
    public uint imageArrayIndex;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrCompositionLayerProjectionView
{
    public XrStructureType type;
    public IntPtr next;
    public XrPosef pose;
    public XrFovf fov;
    public XrSwapchainSubImage subImage;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrCompositionLayerProjection
{
    public XrStructureType type;
    public IntPtr next;
    public ulong layerFlags;
    public ulong space;
    public uint viewCount;
    public IntPtr views; // XrCompositionLayerProjectionView*
}

[StructLayout(LayoutKind.Sequential)]
public struct XrCompositionLayerQuad
{
    public XrStructureType type;
    public IntPtr next;
    public ulong layerFlags;
    public ulong space;
    public XrEyeVisibility eyeVisibility;
    public XrSwapchainSubImage subImage;
    public XrPosef pose;
    public XrExtent2Di size;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrViewLocateInfo
{
    public XrStructureType type;
    public IntPtr next;
    public XrViewConfigurationType viewConfigurationType;
    public long displayTime;
    public ulong space;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrViewState
{
    public XrStructureType type;
    public IntPtr next;
    public ulong viewStateFlags;
}

[StructLayout(LayoutKind.Sequential)]
public struct XrView
{
    public XrStructureType type;
    public IntPtr next;
    public XrPosef pose;
    public XrFovf fov;
}
