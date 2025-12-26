using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace Basic
{
    [System.Serializable]
    public class SceneReference
    {
#if UNITY_EDITOR
        [SerializeField]
        private SceneAsset sceneAsset;
#endif

        [SerializeField]
        private string sceneName;

        public string SceneName => sceneName;

        public static implicit operator string(SceneReference sceneReference)
        {
            return sceneReference.sceneName;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty sceneAssetProperty = property.FindPropertyRelative("sceneAsset");
            SerializedProperty sceneNameProperty = property.FindPropertyRelative("sceneName");

            EditorGUI.BeginChangeCheck();
            SceneAsset newSceneAsset =
                EditorGUI.ObjectField(
                    position,
                    label,
                    sceneAssetProperty.objectReferenceValue,
                    typeof(SceneAsset),
                    false
                ) as SceneAsset;

            if (EditorGUI.EndChangeCheck())
            {
                sceneAssetProperty.objectReferenceValue = newSceneAsset;

                if (newSceneAsset != null)
                {
                    sceneNameProperty.stringValue = newSceneAsset.name;
                }
                else
                {
                    sceneNameProperty.stringValue = string.Empty;
                }
            }

            EditorGUI.EndProperty();
        }
    }
#endif
}
