﻿namespace UnityEngine
{
    public static class AnimationCurveExtensions
    {
        public static void Copy(this AnimationCurve animationCurve, AnimationCurve from)
        {
            Keyframe[] keyframes = new Keyframe[from.length];

            for (int i = 0; i < keyframes.Length; i++)
            {
                Keyframe k = new Keyframe
                {
                    time = from[i].time,
                    value = from[i].value,
                    inTangent = from[i].inTangent,
                    outTangent = from[i].outTangent,
                };
                
                keyframes[i] = k;
            }

            animationCurve.keys = keyframes;
        }

        public static void Lerp(
            this AnimationCurve animationCurve, AnimationCurve from, AnimationCurve to, float fraction)
        {
            if (from.length != to.length)
                return;

            Keyframe[] keyframes = new Keyframe[from.length];

            for (int i = 0; i < keyframes.Length; i++)
            {
                Keyframe k = new Keyframe
                {
                    time = Mathf.Lerp(from[i].time, to[i].time, fraction),
                    value = Mathf.Lerp(from[i].value, to[i].value, fraction),
                    inTangent = Mathf.Lerp(from[i].inTangent, to[i].inTangent, fraction),
                    outTangent = Mathf.Lerp(from[i].outTangent, to[i].outTangent, fraction),
                };

                keyframes[i] = k;
            }

            animationCurve.keys = keyframes;
        }

        public static float GetDuration(this AnimationCurve animationCurve)
        {
            if (animationCurve.length < 2)
                return 0.0f;
            
            return animationCurve[animationCurve.length - 1].time;
        }
    }
}
