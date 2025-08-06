using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Unity.Editor.Example
{
    static class AddPackageExample
    {
        static AddRequest Request;

        [MenuItem("Tools/Basic/UpdateLocal #&r")]
        static void Add()
        {
            // Add a package to the project
            Request = Client.Add("file:C:/work/Nikita/unity_basic/Assets/Basic");
            EditorApplication.update += Progress;
        }

        static void Progress()
        {
            if (Request.IsCompleted)
            {
                if (Request.Status == StatusCode.Success)
                    Debug.Log("Installed: " + Request.Result.packageId);
                else if (Request.Status >= StatusCode.Failure)
                    Debug.Log(Request.Error.message);

                EditorApplication.update -= Progress;
            }
        }
    }
}
