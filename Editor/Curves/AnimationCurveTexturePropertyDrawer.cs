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
                
                AnimationCurveTexture animationCurveTexture =
                    this.GetActualObject<AnimationCurveTexture>(fieldInfo, property);
                
                // And another for local curve's Save To Asset button.
                if (animationCurveTexture.CurveMode == AnimationCurveTexture.CurveModes.Local)
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
            bool saveToAsset = false;
            
            // Draw some of the more internal properties for you to tweak once.
            if (property.isExpanded)
            {
                // Draw the current texture.
                Texture2D texture = animationCurveTexture.Texture;
                Rect previewRect = position.GetControlLastRect();
                if (animationCurveTexture.CurveMode == AnimationCurveTexture.CurveModes.Local)
                    previewRect = previewRect.GetControlPreviousRect();

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

                // Draw a button to save the local curve to an asset.
                if (animationCurveTexture.CurveMode == AnimationCurveTexture.CurveModes.Local)
                {
                    Rect saveToAssetRect = previewRect.GetControlNextRect().Indent(1);
                    saveToAsset = GUI.Button(saveToAssetRect, "Save To Asset");
                }
            }

            // Re-bake the texture again if needed.
            if (shouldUpdateTexture)
            {
                // NOTE: Need to apply the modified properties otherwise it will generate a texture using old data.
                property.serializedObject.ApplyModifiedProperties();
                animationCurveTexture.GenerateTextureForCurve();
            }
            
            // NOTE: We need to do this at the end apparently otherwise an exception is thrown.
            if (saveToAsset)
                SaveToAsset(curveProperty, property);
        }

        private void SaveToAsset(SerializedProperty curveProperty, SerializedProperty owner)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Animation Curve Asset", "Animation Curve", "asset",
                "Save this local animation curve to a re-usable animation curve asset.");
            if (string.IsNullOrEmpty(path))
                return;

            // First check if that asset existed already.
            AnimationCurveAsset asset = AssetDatabase.LoadAssetAtPath<AnimationCurveAsset>(path);
            bool existedAlready = asset != null;

            if (!existedAlready)
            {
                // Create a new animation curve asset for this curve.
                asset = ScriptableObject.CreateInstance<AnimationCurveAsset>();
                asset.AnimationCurve = curveProperty.animationCurveValue;
                AssetDatabase.CreateAsset(asset, path);
            }
            else
            {
                // Update the existing asset with this animation curve.
                using SerializedObject so = new SerializedObject(asset);
                so.Update();
                so.FindProperty("animationCurve").animationCurveValue = curveProperty.animationCurveValue;
                so.ApplyModifiedProperties();
            }

            // Set the animation curve texture to Asset mode and assign the selected asset.
            owner.serializedObject.Update();
            owner.FindPropertyRelative("curveMode").enumValueIndex =
                (int)AnimationCurveTexture.CurveModes.Asset;
            owner.FindPropertyRelative("animationCurveAsset").objectReferenceValue = asset;
            owner.serializedObject.ApplyModifiedProperties();
        }
    }
}
