using System;
using System.Runtime.InteropServices;

namespace OverTheHillVR.OpenXR;

public static class OpenXR
{
    private const string LibName = "openxr_loader";

    public static ulong XR_MAKE_VERSION(uint major, uint minor, uint patch)
    {
        return ((ulong)(major & 0xFFFF) << 48) | ((ulong)(minor & 0xFFFF) << 32) | (ulong)(patch & 0xFFFFFFFF);
    }

    public static ulong XR_CURRENT_API_VERSION => XR_MAKE_VERSION(1, 0, 34);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrGetInstanceProcAddr(ulong instance, [MarshalAs(UnmanagedType.LPStr)] string name, out IntPtr function);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrCreateInstance(ref XrInstanceCreateInfo createInfo, out ulong instance);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrDestroyInstance(ulong instance);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrGetSystem(ulong instance, ref XrSystemGetInfo getInfo, out ulong systemId);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrCreateSession(ulong instance, ref XrSessionCreateInfo createInfo, out ulong session);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrDestroySession(ulong session);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrBeginSession(ulong session, ref XrSessionBeginInfo beginInfo);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrEndSession(ulong session);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrCreateReferenceSpace(ulong session, ref XrReferenceSpaceCreateInfo createInfo, out ulong space);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrDestroySpace(ulong space);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrEnumerateViewConfigurationViews(
        ulong instance, ulong systemId, XrViewConfigurationType viewConfigurationType,
        uint viewCapacityInput, out uint viewCountOutput, IntPtr views);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrCreateSwapchain(ulong session, ref XrSwapchainCreateInfo createInfo, out ulong swapchain);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrDestroySwapchain(ulong swapchain);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrEnumerateSwapchainImages(ulong swapchain, uint imageCapacityInput, out uint imageCountOutput, IntPtr images);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrAcquireSwapchainImage(ulong swapchain, ref XrSwapchainImageAcquireInfo acquireInfo, out uint index);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrWaitSwapchainImage(ulong swapchain, ref XrSwapchainImageWaitInfo waitInfo);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrReleaseSwapchainImage(ulong swapchain, ref XrSwapchainImageReleaseInfo releaseInfo);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrWaitFrame(ulong session, ref XrFrameWaitInfo frameWaitInfo, ref XrFrameState frameState);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrBeginFrame(ulong session, ref XrFrameBeginInfo frameBeginInfo);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrEndFrame(ulong session, ref XrFrameEndInfo frameEndInfo);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern XrResult xrLocateViews(
        ulong session, ref XrViewLocateInfo viewLocateInfo,
        ref XrViewState viewState, uint viewCapacityInput, out uint viewCountOutput, IntPtr views);
}
