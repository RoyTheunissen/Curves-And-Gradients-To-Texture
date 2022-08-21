using UnityEngine;

namespace RoyTheunissen.CurvesAndGradientsToTexture.Curves
{
    /// <summary>
    /// An asset that wraps an animation curve so that the same one can be re-used across multiple scripts.
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationCurveAsset", menuName = "ScriptableObject/AnimationCurveAsset")]
    public class AnimationCurveAsset : ScriptableObject
    {
        [SerializeField]
        private AnimationCurve animationCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
        public AnimationCurve AnimationCurve => animationCurve;

        public float Evaluate(float time)
        {
            return animationCurve.Evaluate(time);
        }

        public Keyframe[] keys
        {
            get => animationCurve.keys;
            set => animationCurve.keys = value;
        }

        public Keyframe this[int index] => animationCurve[index];

        public int length => animationCurve.length;

        public WrapMode preWrapMode
        {
            get => animationCurve.preWrapMode;
            set => animationCurve.preWrapMode = value;
        }

        public WrapMode postWrapMode
        {
            get => animationCurve.postWrapMode;
            set => animationCurve.postWrapMode = value;
        }

        public static implicit operator AnimationCurve(AnimationCurveAsset animationCurveAsset)
        {
            return animationCurveAsset == null ? null : animationCurveAsset.AnimationCurve;
        }
    }
}
