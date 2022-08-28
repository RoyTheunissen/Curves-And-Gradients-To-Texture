using System;
using RoyTheunissen.CurvesAndGradientsToTexture.Curves;
using UnityEngine;
using UnityEngine.Serialization;

namespace RoyTheunissen.CurvesAndGradientsToTexture.Curves
{
    /// <summary>
    /// Caches a texture for an Animation Curve. Helps pass easily tweakable curve data on to a shader.
    ///
    /// NOTE: Despite the apparent similarity, this code is not shared with GradientTexture because
    /// I expect these two utilities to be diverging a lot, so any effort to consolidate the two will likely be undone.
    /// </summary>
    [Serializable]
    public class AnimationCurveTexture
    {
        public enum Modes
        {
            Asset,
            Local,
        }
        
        private const float DefaultValueMultiplier = 1.0f;
        private const int DefaultResolution = 512;
        private const TextureWrapMode DefaultWrapMode = TextureWrapMode.Clamp;
        private const FilterMode DefaultFilterMode = FilterMode.Bilinear;

        [FormerlySerializedAs("curveMode")]
        [SerializeField] private Modes mode = Modes.Local;
        public Modes Mode => mode;

        [SerializeField, HideInInspector] private AnimationCurveAsset animationCurveAsset;

        [SerializeField, HideInInspector]
        private AnimationCurve animationCurveLocal = AnimationCurve.Linear(0, 0, 1, 1);
        
        [SerializeField] private float valueMultiplier = DefaultValueMultiplier;
        [SerializeField] private int resolution = DefaultResolution;
        [SerializeField] private TextureWrapMode wrapMode = DefaultWrapMode;
        [SerializeField] private FilterMode filterMode = DefaultFilterMode;
        
        [NonSerialized] private Texture2D cachedTexture;

        private AnimationCurve Curve => mode == Modes.Asset ? animationCurveAsset : animationCurveLocal;

        public AnimationCurveTexture()
        {
            resolution = DefaultResolution;
            wrapMode = DefaultWrapMode;
            filterMode = DefaultFilterMode;
        }

        public AnimationCurveTexture(AnimationCurveAsset animationCurveAsset) : this()
        {
            mode = Modes.Asset;
            this.animationCurveAsset = animationCurveAsset;
        }

        public AnimationCurveTexture(AnimationCurve animationCurve) : this()
        {
            mode = Modes.Local;
            animationCurveLocal = animationCurve;
        }

        public Texture2D Texture
        {
            get
            {
                if (cachedTexture == null)
                    GenerateTextureForCurve();
                
                return cachedTexture;
            }
        }

        public void GenerateTextureForCurve()
        {
            if (cachedTexture == null)
                cachedTexture = new Texture2D(resolution, 1, TextureFormat.RFloat, false);

            if (cachedTexture.width != resolution)
                cachedTexture.Reinitialize(resolution, 1);

            cachedTexture.wrapMode = wrapMode;
            cachedTexture.filterMode = filterMode;

            Color[] colors = new Color[resolution];
            bool hasCurve = Curve != null;
            float duration = hasCurve ? Curve.GetDuration() : 0;
            for(int i = 0; i < resolution; ++i)
            {
                if (!hasCurve)
                {
                    colors[i] = new Color(0, 0, 0, 1);
                    continue;
                }
                
                float t = (float)i / resolution;

                float x = t * duration;

                float value = Curve == null ? 0 : Curve.Evaluate(x) * valueMultiplier;

                colors[i] = new Color(value, value, value, 1);
            }

            cachedTexture.SetPixels(colors);
            cachedTexture.Apply(false);
        }
    }
}
