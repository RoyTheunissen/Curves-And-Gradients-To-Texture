using System;
using System.IO;
using RoyTheunissen.CurvesAndGradientsToTexture.Extensions;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.CurvesAndGradientsToTexture.Gradients
{
    /// <summary>
    /// Draws the gradient itself but also allows you to unfold it and tweak some of the more advanced settings.
    ///
    /// NOTE: Despite the apparent similarity, this code is not shared with AnimationCurveTexturePropertyDrawer because
    /// I expect these two utilities to be diverging a lot, so any effort to consolidate the two will likely be undone.
    /// </summary>
    [CustomPropertyDrawer(typeof(GradientTexture))]
    public class GradientTexturePropertyDrawer : PropertyDrawer
    {
        private const string GradientPropertyAsset = "gradientAsset";
        private const string GradientPropertyLocal = "gradientLocal";
        private const string GradientPropertyTexture = "texture";
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUI.GetPropertyHeight(property, label);

            if (property.isExpanded)
            {
                // One line for the preview.
                height += EditorGUIUtility.singleLineHeight;
                
                // And another for the edit or save button.
                if (property.FindPropertyRelative("mode").enumValueIndex != (int)GradientTexture.Modes.Texture)
                {
                    // There's an edit/save button and then also an Export To Texture button.
                    height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;
                }
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
            string gradientPropertyName = null;
            switch (gradientTexture.Mode)
            {
                case GradientTexture.Modes.Asset:
                    gradientPropertyName = GradientPropertyAsset;
                    break;
                case GradientTexture.Modes.Local:
                    gradientPropertyName = GradientPropertyLocal;
                    break;
                case GradientTexture.Modes.Texture:
                    gradientPropertyName = GradientPropertyTexture;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            SerializedProperty gradientProperty = property.FindPropertyRelative(gradientPropertyName);
            EditorGUI.PropertyField(gradientRect, gradientProperty, GUIContent.none);
            
            // Draw the children, too.
            if (property.isExpanded)
            {
                Rect childrenRect = position.GetSubRectFromBottom(position.height - foldoutRect.height);
                property.PropertyFieldForChildren(childrenRect);
            }

            bool shouldUpdateTexture = EditorGUI.EndChangeCheck();
            
            bool isTextureMode = gradientTexture.Mode == GradientTexture.Modes.Texture;
            
            // Draw some of the more internal properties for you to tweak once.
            if (property.isExpanded)
            {
                // Draw the current texture.
                Texture2D texture = gradientTexture.Texture;
                Rect previewRect = position.GetControlLastRect();
                if (!isTextureMode)
                    previewRect = previewRect.GetControlPreviousRect().GetControlPreviousRect();

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
                    (GradientAsset)property.FindPropertyRelative(GradientPropertyAsset).objectReferenceValue;
                
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
                else if (gradientTexture.Mode == GradientTexture.Modes.Asset)
                {
                    // Draw a button to start editing a local copy of this asset.
                    bool wasGuiEnabled = GUI.enabled;
                    GUI.enabled = asset != null;
                    bool editLocalCopy = GUI.Button(buttonRect, "Edit Local Copy");
                    GUI.enabled = wasGuiEnabled;
                    if (editLocalCopy)
                    {
                        // Make a copy of the asset.
                        property.FindPropertyRelative(GradientPropertyLocal).SetGradient(asset.Gradient);
                        
                        // Switch to local mode.
                        property.FindPropertyRelative("mode").enumValueIndex = (int)GradientTexture.Modes.Local;
                    }
                }

                if (gradientTexture.Mode != GradientTexture.Modes.Texture)
                {
                    buttonRect = buttonRect.GetControlNextRect();
                    bool wasGuiEnabled = GUI.enabled;
                    GUI.enabled = asset != null || gradientTexture.Mode == GradientTexture.Modes.Local;
                    bool exportToTexture = GUI.Button(buttonRect, "Export To Texture");
                    string pathToSaveTo = asset == null ? "" : AssetDatabase.GetAssetPath(asset);
                    if (exportToTexture)
                        ExportToTexture(gradientTexture, property, null, pathToSaveTo);
                    GUI.enabled = wasGuiEnabled;
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

        private void ExportToTexture(
            GradientTexture gradientTexture, SerializedProperty owner, string path = null, string startingPath = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = EditorUtility.SaveFilePanelInProject(
                    "Export Gradient To Texture", "Gradient", "png", "Export this gradient as a texture.",
                    startingPath);
            }

            if (string.IsNullOrEmpty(path))
                return;

            // Write the texture to a PNG file.
            byte[] bytes = gradientTexture.Texture.EncodeToPNG();
            const string assetsFolder = "Assets";
            string absolutePath = path.Substring(assetsFolder.Length + 1);
            absolutePath = Application.dataPath + Path.AltDirectorySeparatorChar + absolutePath;
            File.WriteAllBytes(absolutePath, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            
            // Let's make sure we assign the right settings.
            TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(path);
            textureImporter.wrapMode = gradientTexture.WrapMode;
            textureImporter.filterMode = gradientTexture.FilterMode;
            textureImporter.mipmapEnabled = false;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            // Set the GradientTexture to Texture mode and assign the created texture.
            Texture2D textureAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            owner.serializedObject.Update();
            owner.FindPropertyRelative("mode").enumValueIndex = (int)GradientTexture.Modes.Texture;
            owner.FindPropertyRelative("texture").objectReferenceValue = textureAsset;
            owner.serializedObject.ApplyModifiedProperties();
        }
    }
}
