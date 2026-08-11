/// <summary>
/// Resolves the two protocol data types used by one media device.
/// </summary>
public static class DeviceDataTypeBinding
{
    public const int GeneratedMediaDataTypeBase = 10000;
    public const int GeneratedPadDataTypeBase = 11000;
    public const int MaxDeviceDataTypeID = 1000;

    public static int GetMediaDataTypeID(int deviceDataTypeID)
    {
        EnsureValidDeviceDataTypeID(deviceDataTypeID);
        return GeneratedMediaDataTypeBase + deviceDataTypeID;
    }

    public static int GetPadDataTypeID(int deviceDataTypeID)
    {
        EnsureValidDeviceDataTypeID(deviceDataTypeID);
        return GeneratedPadDataTypeBase + deviceDataTypeID;
    }

    public static bool IsValidDeviceDataTypeID(int deviceDataTypeID)
    {
        return deviceDataTypeID >= 1 && deviceDataTypeID <= MaxDeviceDataTypeID;
    }

    private static void EnsureValidDeviceDataTypeID(int deviceDataTypeID)
    {
        if (!IsValidDeviceDataTypeID(deviceDataTypeID))
        {
            throw new System.ArgumentOutOfRangeException(
                "deviceDataTypeID",
                "DeviceDataTypeID must be between 1 and 1000.");
        }
    }
}
