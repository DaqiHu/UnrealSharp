namespace UnrealSharpBuildTool;

public static class DotNetHelper
{
    /// <summary>
    /// Gets the host runtime identifier for a given platform (eg. linux-arm64).
    /// </summary>
    /// <param name="platform">Target platform.</param>
    /// <param name="architecture">Target architecture.</param>
    /// <returns>Runtime identifier.</returns>
    public static string GetHostRuntimeIdentifier(TargetPlatform platform, TargetArchitecture architecture)
    {
        string result;
        switch (platform)
        {
        case TargetPlatform.Windows:
        case TargetPlatform.XboxOne:
        case TargetPlatform.XboxScarlett:
        case TargetPlatform.UWP:
            result = "win";
            break;
        case TargetPlatform.Linux:
            result = "linux";
            break;
        case TargetPlatform.PS4:
            result = "ps4";
            break;
        case TargetPlatform.PS5:
            result = "ps5";
            break;
        case TargetPlatform.Android:
            result = "android";
            break;
        case TargetPlatform.Switch:
            result = "switch";
            break;
        case TargetPlatform.Mac:
            result = "osx";
            break;
        case TargetPlatform.iOS:
            result = "ios";
            break;
        default: throw new ArgumentException("Invalid platform: " + platform);
        }
        switch (architecture)
        {
        case TargetArchitecture.x64:
            result += "-x64";
            break;
        case TargetArchitecture.ARM64:
            result += "-arm64";
            break;
        default: throw new ArgumentException("Invalid architecture: " + architecture);
        }
        return result;
    }
}