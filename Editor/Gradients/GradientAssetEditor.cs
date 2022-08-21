using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.CurvesAndGradientsToTexture.Gradients
{
    /// <summary>
    /// Draws the gradient of a gradient asset a little nicer.
    /// </summary>
    [CustomEditor(typeof(GradientAsset))]
    public class GradientAssetEditor : Editor
    {
        private SerializedProperty gradientProperty;

        private void OnEnable()
        {
            gradientProperty = serializedObject.FindProperty("gradient");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(gradientProperty, GUIContent.none);
            serializedObject.ApplyModifiedProperties();
        }
        
    }
}
