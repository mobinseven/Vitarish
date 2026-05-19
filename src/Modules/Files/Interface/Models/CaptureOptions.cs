namespace Vitarish.Files.Interface.Models;

public sealed record CaptureOptions(
    bool EnableImageCapture = false,
    CaptureDeviceMode CaptureDevice = CaptureDeviceMode.Environment,
    bool AllowMultipleSelection = true);
