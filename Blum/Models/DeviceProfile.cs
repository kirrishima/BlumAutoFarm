namespace Blum.Models;
public class DeviceProfile
{
    public string DeviceName { get; }
    public string OperatingSystem { get; }
    public string Browser { get; }
    public string UserAgent { get; }

    public DeviceProfile(string deviceName, string operatingSystem, string browser, string userAgent)
    {
        DeviceName = deviceName;
        OperatingSystem = operatingSystem;
        Browser = browser;
        UserAgent = userAgent;
    }
}

public static class DeviceProfiles
{
    public static readonly DeviceProfile AndroidGalaxyS21 = new DeviceProfile(
        "Samsung Galaxy S21",
        "Android 12.0",
        "Chrome",
        "Mozilla/5.0 (Linux; Android 12.0; SM-G991B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.5735.110 Mobile Safari/537.36"
    );

    public static readonly DeviceProfile AndroidPocoX5Pro5G = new DeviceProfile(
        "Poco X5 Pro 5G",
        "Android 14.0",
        "Chrome",
        "Mozilla/5.0 (Linux; Android 14; 22101320G) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.5735.110 Mobile Safari/537.36"
    );

    public static readonly DeviceProfile AndroidPixel5 = new DeviceProfile(
        "Google Pixel 5",
        "Android 11.0",
        "Chrome",
        "Mozilla/5.0 (Linux; Android 11.0; Pixel 5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/113.0.5672.92 Mobile Safari/537.36"
    );

    public static readonly DeviceProfile AndroidOnePlus9 = new DeviceProfile(
        "OnePlus 9",
        "Android 11.0",
        "Chrome",
        "Mozilla/5.0 (Linux; Android 11.0; OnePlus 9) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.5615.137 Mobile Safari/537.36"
    );

    public static readonly DeviceProfile iPhone12 = new DeviceProfile(
        "iPhone 12",
        "iOS 14.0",
        "Safari",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.0 Mobile/15E148 Safari/604.1"
    );

    public static readonly DeviceProfile iPhone13Pro = new DeviceProfile(
        "iPhone 13 Pro",
        "iOS 15.0",
        "Safari",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 15_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.0 Mobile/15E148 Safari/604.1"
    );

    public static readonly DeviceProfile iPhoneSE = new DeviceProfile(
        "iPhone SE (2nd generation)",
        "iOS 14.2",
        "Safari",
        "Mozilla/5.0 (iPhone; CPU iPhone OS 14_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.0 Mobile/15E148 Safari/604.1"
    );

    private static readonly DeviceProfile[] deviceProfiles =
    [
        AndroidGalaxyS21,
        AndroidPocoX5Pro5G,
        AndroidPixel5,
        AndroidOnePlus9,
        iPhone12,
        iPhone13Pro,
        iPhoneSE
    ];

    public static DeviceProfile[] Profiles => deviceProfiles;
}
