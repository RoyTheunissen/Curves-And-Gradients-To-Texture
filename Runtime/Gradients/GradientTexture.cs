using System;
using UnityEngine;

namespace RoyTheunissen.CurvesAndGradientsToTexture.Gradients
{
    /// <summary>
    /// Caches a texture for a Gradient. Helps pass easily tweakable gradient data on to a shader.
    ///
    /// NOTE: Despite the apparent similarity, this code is not shared with AnimationCurveTexture because
    /// I expect these two utilities to be diverging a lot, so any effort to consolidate the two will likely be undone.
    /// </summary>
    [Serializable]
    public class GradientTexture 
    {
        public enum Modes
        {
            Asset,
            Local,
            Texture,
        }

        private const int DefaultResolution = 512;
        private const TextureWrapMode DefaultWrapMode = TextureWrapMode.Clamp;
        private const FilterMode DefaultFilterMode = FilterMode.Bilinear;

        [SerializeField] private Modes mode = Modes.Local;
        public Modes Mode => mode;

        [SerializeField, HideInInspector] private GradientAsset gradientAsset;
        
        [SerializeField, HideInInspector] private Texture2D texture;

        [SerializeField, HideInInspector, GradientUsage(true)]
        private Gradient gradientLocal = new Gradient
        {
            alphaKeys = new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) },
            colorKeys = new[] { new GradientColorKey(Color.black, 0), new GradientColorKey(Color.white, 1) },
        };

        public Gradient GradientLocal => gradientLocal;
        
        [SerializeField] private int resolution = DefaultResolution;
        [SerializeField] private TextureWrapMode wrapMode = DefaultWrapMode;
        public TextureWrapMode WrapMode => wrapMode;
        
        [SerializeField] private FilterMode filterMode = DefaultFilterMode;
        public FilterMode FilterMode => filterMode;

        [NonSerialized] private Texture2D cachedTexture;

        private Gradient Gradient => mode == Modes.Asset ? gradientAsset : gradientLocal;

        public GradientTexture()
        {
            resolution = DefaultResolution;
            wrapMode = DefaultWrapMode;
            filterMode = DefaultFilterMode;
        }

        public GradientTexture(GradientAsset gradientAsset) : this()
        {
            mode = Modes.Asset;
            this.gradientAsset = gradientAsset;
        }

        public GradientTexture(Gradient gradient) : this()
        {
            mode = Modes.Local;
            gradientLocal = gradient;
        }

        public Texture2D Texture
        {
            get
            {
                if (mode == Modes.Texture)
                    return texture == null ? DefaultTexture : texture;

                if (cachedTexture == null)
                    GenerateTexture();
                
                return cachedTexture;
            }
        }
        
        private static Texture2D cachedDefaultTexture;
        private static bool didCacheDefaultTexture;
        private static Texture2D DefaultTexture
        {
            get
            {
                if (!didCacheDefaultTexture)
                {
                    didCacheDefaultTexture = true;
                    cachedDefaultTexture = new Texture2D(1, 1);
                    cachedDefaultTexture.SetPixels(new[] { Color.white });
                }
                return cachedDefaultTexture;
            }
        }

        public void GenerateTexture()
        {
            if (cachedTexture == null)
                cachedTexture = new Texture2D(resolution, 1, TextureFormat.RGBA32, false);

            if (cachedTexture.width != resolution)
                cachedTexture.Reinitialize(resolution, 1);

            cachedTexture.wrapMode = wrapMode;
            cachedTexture.filterMode = filterMode;

            Color[] colors = new Color[resolution];
            bool hasGradient = Gradient != null;
            for(int i = 0; i < resolution; ++i)
            {
                if (!hasGradient)
                {
                    colors[i] = new Color(0, 0, 0, 1);
                    continue;
                }
                
                float t = (float)i / resolution;

                Color color = Gradient == null ? Color.black : Gradient.Evaluate(t);

                colors[i] = color;
            }

            cachedTexture.SetPixels(colors);
            cachedTexture.Apply(false);
        }
    }
}
