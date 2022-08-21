using RoyTheunissen.CurvesAndGradientsToTexture.Extensions;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.CurvesAndGradientsToTexture.Curves
{
    /// <summary>
    /// Draws the curve itself but also allows you to unfold the curve and tweak some of the more advanced settings.
    /// </summary>
    [CustomPropertyDrawer(typeof(AnimationCurveTexture))]
    public class AnimationCurveTexturePropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUI.GetPropertyHeight(property, label);

            if (property.isExpanded)
            {
                // One line for the preview.
                height += EditorGUIUtility.singleLineHeight;
            }
            
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            AnimationCurveTexture animationCurveTexture =
                this.GetActualObject<AnimationCurveTexture>(fieldInfo, property);

            EditorGUI.BeginChangeCheck();

            // Draw the header.
            Rect foldoutRect = position.GetControlFirstRect().GetLabelRect(out Rect curveRect);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);
            
            // Draw a field next to the label so you can edit it straight away.
            string curvePropertyName = animationCurveTexture.CurveMode == AnimationCurveTexture.CurveModes.Asset
                ? "animationCurveAsset"
                : "animationCurveLocal";
            SerializedProperty curveProperty = property.FindPropertyRelative(curvePropertyName);
            EditorGUI.PropertyField(curveRect, curveProperty, GUIContent.none);
            
            // Draw the children, too.
            if (property.isExpanded)
            {
                Rect childrenRect = position.GetSubRectFromBottom(position.height - foldoutRect.height);
                property.PropertyFieldForChildren(childrenRect);
            }

            bool shouldUpdateTexture = EditorGUI.EndChangeCheck();
            
            // Draw some of the more internal properties for you to tweak once.
            if (property.isExpanded)
            {
                // Draw the current texture.
                Texture2D texture = animationCurveTexture.Texture;
                Rect previewRect = position.GetControlLastRect();

                Rect labelRect = previewRect.GetLabelRect(out Rect textureRect).Indent(1);
                EditorGUI.LabelField(labelRect, new GUIContent("Preview"));

                // Draw an update button.
                Rect generateButtonRect = textureRect.GetSubRectFromRight(70, out textureRect);
                bool forceUpdate = GUI.Button(generateButtonRect, "Update");
                
                // Update now so that it's done BEFORE we draw the texture, so you immediately see the result.
                if (forceUpdate || shouldUpdateTexture)
                {
                    // NOTE: Need to apply the modified properties otherwise it will generate a texture using old data.
                    property.serializedObject.ApplyModifiedProperties();
                    animationCurveTexture.GenerateTextureForCurve();
                    shouldUpdateTexture = false;
                }
                    
                // Draw the texture itself.
                if (texture == null)
                    EditorGUI.DrawRect(textureRect, Color.black);
                else
                    GUI.DrawTexture(textureRect, texture, ScaleMode.StretchToFill);
            }

            // Re-bake the texture again if needed.
            if (shouldUpdateTexture)
            {
                // NOTE: Need to apply the modified properties otherwise it will generate a texture using old data.
                property.serializedObject.ApplyModifiedProperties();
                animationCurveTexture.GenerateTextureForCurve();
            }
        }
        
    }
}
