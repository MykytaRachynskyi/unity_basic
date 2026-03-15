using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.Toolbars;

namespace Basic.UnityEditorTools
{
    public class UpdatePackage
    {
        private const string PackageGitUrl = "https://github.com/MykytaRachynskyi/unity_basic.git";

        private static AddRequest _request;

        [MainToolbarElement("Test", defaultDockPosition = MainToolbarDockPosition.Right)]
        private static MainToolbarElement CreateAnalysisWindowsBar()
        {
            static void UpdatePackage()
            {
                if (_request != null && !_request.IsCompleted)
                {
                    Log.Info("[ArcticLime] Package update already in progress...");
                    return;
                }

                Log.Info("[ArcticLime] Updating Unity Basic package...");
                _request = Client.Add(PackageGitUrl);
                EditorApplication.update += TrackRequest;
            }

            static void TrackRequest()
            {
                if (_request == null || !_request.IsCompleted)
                    return;

                EditorApplication.update -= TrackRequest;

                if (_request.Status == StatusCode.Success)
                    Log.Info(
                        $"[ArcticLime] Unity Basic updated to {_request.Result.version} ({_request.Result.git?.hash?[..8] ?? "?"})"
                    );
                else if (_request.Status >= StatusCode.Failure)
                    Log.Error(
                        $"[ArcticLime] Failed to update Unity Basic: {_request.Error?.message}"
                    );

                _request = null;
            }

            return new MainToolbarButton(new("Update Unity Basic"), UpdatePackage);
        }
    }
}
