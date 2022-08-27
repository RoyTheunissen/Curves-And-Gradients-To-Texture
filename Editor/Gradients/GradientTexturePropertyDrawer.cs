using System.IO;
using RoyTheunissen.CurvesAndGradientsToTexture.Extensions;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.CurvesAndGradientsToTexture.Gradients
{
    /// <summary>
    /// Draws the gradient itself but also allows you to unfold it and tweak some of the more advanced settings.
    /// </summary>
    [CustomPropertyDrawer(typeof(GradientTexture))]
    public class GradientTexturePropertyDrawer : PropertyDrawer
    {
        private const string GradientAsset = "gradientAsset";
        private const string GradientLocal = "gradientLocal";
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUI.GetPropertyHeight(property, label);

            if (property.isExpanded)
            {
                // One line for the preview.
                height += EditorGUIUtility.singleLineHeight;
                
                // And another for the edit or save button.
                height += EditorGUIUtility.singleLineHeight;
            }
            
            return height;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GradientTexture gradientTexture = this.GetActualObject<GradientTexture>(fieldInfo, property);

            EditorGUI.BeginChangeCheck();

            // Draw the header.
            Rect foldoutRect = position.GetControlFirstRect().GetLabelRect(out Rect gradientRect);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);
            
            // Draw a field next to the label so you can edit it straight away.
            string gradientPropertyName = gradientTexture.Mode == GradientTexture.Modes.Asset
                ? GradientAsset
                : GradientLocal;
            SerializedProperty gradientProperty = property.FindPropertyRelative(gradientPropertyName);
            EditorGUI.PropertyField(gradientRect, gradientProperty, GUIContent.none);
            
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
                Texture2D texture = gradientTexture.Texture;
                Rect previewRect = position.GetControlLastRect().GetControlPreviousRect();

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
                    gradientTexture.GenerateTexture();
                    shouldUpdateTexture = false;
                }
                    
                // Draw the texture itself.
                if (texture == null)
                    EditorGUI.DrawRect(textureRect, Color.black);
                else
                    GUI.DrawTexture(textureRect, texture, ScaleMode.StretchToFill);
                
                GradientAsset asset =
                    (GradientAsset)property.FindPropertyRelative(GradientAsset).objectReferenceValue;
                
                Rect buttonRect = previewRect.GetControlNextRect().Indent(1);
                if (gradientTexture.Mode == GradientTexture.Modes.Local)
                {
                    // Draw a button to save the local gradient to an asset.
                    string pathToSaveTo = asset == null ? "" : AssetDatabase.GetAssetPath(asset);
                    bool hasPathToSaveTo = !string.IsNullOrEmpty(pathToSaveTo);
                    Rect saveRect = buttonRect;
                    Rect saveAsRect = hasPathToSaveTo
                        ? buttonRect.GetSubRectFromRight(70, out saveRect)
                        : buttonRect;
                    
                    // Big button to save to the current asset (only shows up if an asset is selected)
                    if (hasPathToSaveTo)
                    {
                        string fileName = Path.GetFileName(pathToSaveTo);
                        bool save = GUI.Button(saveRect, $"Save ({fileName})");
                        if (save)
                            SaveToAsset(gradientProperty, property, pathToSaveTo);
                    }
                    
                    // Smaller button to pick a new asset to save to.
                    bool saveAs = GUI.Button(saveAsRect, "Save As...");
                    if (saveAs)
                        SaveToAsset(gradientProperty, property, null, pathToSaveTo);
                }
                else
                {
                    // Draw a button to start editing a local copy of this asset.
                    bool wasGuiEnabled = GUI.enabled;
                    GUI.enabled = asset != null;
                    bool editLocalCopy = GUI.Button(buttonRect, "Edit Local Copy");
                    GUI.enabled = wasGuiEnabled;
                    if (editLocalCopy)
                    {
                        // Make a copy of the asset.
                        property.FindPropertyRelative(GradientLocal).SetGradient(asset.Gradient);
                        
                        // Switch to local mode.
                        property.FindPropertyRelative("mode").enumValueIndex = (int)GradientTexture.Modes.Local;
                    }
                }
            }

            // Re-bake the texture again if needed.
            if (shouldUpdateTexture)
            {
                // NOTE: Need to apply the modified properties otherwise it will generate a texture using old data.
                property.serializedObject.ApplyModifiedProperties();
                gradientTexture.GenerateTexture();
            }
        }
        
        private void SaveToAsset(
            SerializedProperty gradientProperty, SerializedProperty owner, string path = null, string startingPath = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = EditorUtility.SaveFilePanelInProject(
                    "Save Gradient Asset", "Gradient", "asset",
                    "Save this local gradient to a re-usable gradient asset.", startingPath);
            }

            if (string.IsNullOrEmpty(path))
                return;

            // First check if that asset existed already.
            GradientAsset asset = AssetDatabase.LoadAssetAtPath<GradientAsset>(path);
            bool existedAlready = asset != null;

            if (!existedAlready)
            {
                // Create a new asset for this gradient.
                asset = ScriptableObject.CreateInstance<GradientAsset>();
                asset.Gradient = gradientProperty.GetGradient();
                AssetDatabase.CreateAsset(asset, path);
            }
            else
            {
                // Update the existing asset with this gradient.
                using SerializedObject so = new SerializedObject(asset);
                so.Update();
                so.FindProperty("gradient").SetGradient(gradientProperty.GetGradient());
                so.ApplyModifiedProperties();
            }

            // Set the gradient texture to Asset mode and assign the selected asset.
            owner.serializedObject.Update();
            owner.FindPropertyRelative("mode").enumValueIndex = (int)GradientTexture.Modes.Asset;
            owner.FindPropertyRelative("gradientAsset").objectReferenceValue = asset;
            owner.serializedObject.ApplyModifiedProperties();
        }
    }
}
