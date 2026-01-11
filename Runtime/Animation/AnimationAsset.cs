using UnityEngine;

namespace Basic
{
    [CreateAssetMenu(fileName = "AnimationAsset", menuName = "Animation Asset")]
    public class AnimationAsset : ScriptableObject
    {
        public AnimationClip Clip;
        public float Speed = 1f;
    }
}
