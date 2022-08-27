using System;
using UnityEngine;

namespace RoyTheunissen.CurvesAndGradientsToTexture.Gradients
{
    /// <summary>
    /// Re-usable gradient.
    /// </summary>
    [CreateAssetMenu(fileName = "GradientAsset", menuName = "ScriptableObject/GradientAsset")]
    public class GradientAsset : ScriptableObject
    {
        [SerializeField, GradientUsage(true)] private Gradient gradient = new Gradient
        {
            alphaKeys = new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) },
            colorKeys = new[] { new GradientColorKey(Color.black, 0), new GradientColorKey(Color.white, 1) },
        };
        public Gradient Gradient
        {
            get => gradient;
            set
            {
                gradient = new Gradient();
                GradientColorKey[] colorKeys = new GradientColorKey[value.colorKeys.Length];
                Array.Copy(value.colorKeys, colorKeys, colorKeys.Length);
                
                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[value.alphaKeys.Length];
                Array.Copy(value.alphaKeys, alphaKeys, alphaKeys.Length);
                
                gradient.SetKeys(colorKeys, alphaKeys);
            }
        }

        public Color Evaluate(float time)
        {
            return gradient.Evaluate(time);
        }

        public void SetKeys(GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys)
        {
            gradient.SetKeys(colorKeys, alphaKeys);
        }

        public bool Equals(Gradient other)
        {
            return gradient.Equals(other);
        }

        public GradientColorKey[] colorKeys
        {
            get => gradient.colorKeys;
            set => gradient.colorKeys = value;
        }

        public GradientAlphaKey[] alphaKeys
        {
            get => gradient.alphaKeys;
            set => gradient.alphaKeys = value;
        }

        public GradientMode mode
        {
            get => gradient.mode;
            set => gradient.mode = value;
        }
        
        public static implicit operator Gradient(GradientAsset gradientAsset)
        {
            return gradientAsset == null ? null : gradientAsset.gradient;
        }
    }
}
