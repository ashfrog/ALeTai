using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SDCardPath
{
    #region 获取外置外置sd卡的路径
    private static AndroidJavaObject GetReflectMethod(string className, string methodName)
    {
        AndroidJavaObject method;
        using (AndroidJavaClass Class = new AndroidJavaClass("java.lang.Class"))
        using (AndroidJavaObject classObject = Class.CallStatic<AndroidJavaObject>("forName", className))
        {
            method = classObject.Call<AndroidJavaObject>("getDeclaredMethod", methodName, null);
            method.Call("setAccessible", true);
        }
        return method;
    }
    public static string GetExternalSDCardRootPath()
    {
        AndroidJavaClass buildVersion = new AndroidJavaClass("android.os.Build$VERSION");

        if (buildVersion.GetStatic<int>("SDK_INT") >= 23)
        {
            AndroidJavaObject method_getExternalDirs = GetReflectMethod("android.os.Environment$UserEnvironment", "getExternalDirs");
            AndroidJavaObject method_myUserId = GetReflectMethod("android.os.UserHandle", "myUserId");

            AndroidJavaClass class_UserHandle = new AndroidJavaClass("android.os.UserHandle");

            AndroidJavaObject mUserId = method_myUserId.Call<AndroidJavaObject>("invoke", class_UserHandle, null);
            int userId = mUserId.Call<int>("intValue");

            AndroidJavaObject env = new AndroidJavaObject("android.os.Environment$UserEnvironment", userId);

            AndroidJavaObject rawfilesArray = method_getExternalDirs.Call<AndroidJavaObject>("invoke", env, null);

            AndroidJavaObject[] fileArray = AndroidJNIHelper.ConvertFromJNIArray<AndroidJavaObject[]>(rawfilesArray.GetRawObject());

            AndroidJavaClass Environment = new AndroidJavaClass("android.os.Environment");

            for (int i = 0; i < fileArray.Length; i++)
            {
                AndroidJavaObject file = fileArray[i];
                bool removable = Environment.CallStatic<bool>("isExternalStorageRemovable", file);
                if (removable)
                {
                    string path = file.Call<string>("getAbsolutePath");
                    return path;
                }
            }
        }
        else
        {
            using (AndroidJavaClass system = new AndroidJavaClass("java.lang.System"))
            {
                string secondaryStoragePath = system.CallStatic<string>("getenv", "SECONDARY_STORAGE");

                if (secondaryStoragePath != null)
                {
                    AndroidJavaObject file = new AndroidJavaObject("java.io.File", secondaryStoragePath);
                    bool canRead = file.Call<bool>("canRead");
                    if (canRead)
                    {
                        return secondaryStoragePath;
                    }
                }
            }
        }
        return null;
    }
    #endregion

    #region 返回手机的内置存储路径
    public static string GetInternalPersisteDataPath()
    {
        string storagePath = GetStoragePathCS(false);
        if (!string.IsNullOrEmpty(storagePath))
        {
            string forceInternalForcePath = storagePath + "/Android/Data/" + Application.identifier + "/files";
            AndroidJavaObject file = new AndroidJavaObject("java.io.File", forceInternalForcePath);
            if (!file.Call<bool>("exists"))
            {
                file.Call<bool>("mkdirs");
            }

            if (file.Call<bool>("canRead") && file.Call<bool>("canWrite"))
            {
                return file.Call<string>("getAbsolutePath");
            }
        }
        return null;
    }
    private static string GetStoragePathCS(bool is_removale)
    {
        using (AndroidJavaClass playerCls = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject activity = playerCls.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaObject mStorageManager = activity.Call<AndroidJavaObject>("getSystemService", "storage"))
        {
            AndroidJavaObject method_getVolumeList = GetReflectMethod("android.os.storage.StorageManager", "getVolumeList");
            AndroidJavaObject method_getPath = GetReflectMethod("android.os.storage.StorageVolume", "getPath");
            AndroidJavaObject method_isRemovable = GetReflectMethod("android.os.storage.StorageVolume", "isRemovable");
            AndroidJavaObject rawVolumeList = method_getVolumeList.Call<AndroidJavaObject>("invoke", mStorageManager, null);
            AndroidJavaObject[] VolumeList = AndroidJNIHelper.ConvertFromJNIArray<AndroidJavaObject[]>(rawVolumeList.GetRawObject());

            for (int i = 0; i < VolumeList.Length; i++)
            {
                AndroidJavaObject storageVolumeElement = VolumeList[i];
                string path = method_getPath.Call<string>("invoke", storageVolumeElement, null);
                AndroidJavaObject java_removable = method_isRemovable.Call<AndroidJavaObject>("invoke", storageVolumeElement, null);
                bool removable = java_removable.Call<bool>("booleanValue");

                if (is_removale == removable)
                {
                    return path;
                }
            }
        }
        return null;
    }
    #endregion
}
