using System;
using System.IO;
using RoyTheunissen.CurvesAndGradientsToTexture.Extensions;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.CurvesAndGradientsToTexture.Curves
{
    /// <summary>
    /// Draws the curve itself but also allows you to unfold the curve and tweak some of the more advanced settings.
    ///
    /// NOTE: Despite the apparent similarity, this code is not shared with GradientTexturePropertyDrawer because
    /// I expect these two utilities to be diverging a lot, so any effort to consolidate the two will likely be undone. 
    /// </summary>
    [CustomPropertyDrawer(typeof(AnimationCurveTexture))]
    public class AnimationCurveTexturePropertyDrawer : PropertyDrawer
    {
        private const string CurvePropertyAsset = "animationCurveAsset";
        private const string CurvePropertyLocal = "animationCurveLocal";
        private const string CurvePropertyTexture = "texture";
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUI.GetPropertyHeight(property, label);

            if (property.isExpanded)
            {
                // One line for the preview.
                height += EditorGUIUtility.singleLineHeight;
                
                // And another for the edit or save button.
                if (property.FindPropertyRelative("mode").enumValueIndex != (int)AnimationCurveTexture.Modes.Texture)
                {
                    // There's an edit/save button and then also an Export To Texture button.
                    height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;
                }
            }
            
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            AnimationCurveTexture animationCurveTexture =
                this.GetActualObject<AnimationCurveTexture>(fieldInfo, property);

            EditorGUI.BeginChangeCheck();
            
            // Figure out what the curve property is.
            string curvePropertyName = null;
            switch (animationCurveTexture.Mode)
            {
                case AnimationCurveTexture.Modes.Asset:
                    curvePropertyName = CurvePropertyAsset;
                    break;
                case AnimationCurveTexture.Modes.Local:
                    curvePropertyName = CurvePropertyLocal;
                    break;
                case AnimationCurveTexture.Modes.Texture:
                    curvePropertyName = CurvePropertyTexture;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            SerializedProperty curveProperty = property.FindPropertyRelative(curvePropertyName);

            // Draw the header.
            Rect foldoutRect = position.GetControlFirstRect().GetLabelRect(out Rect curveRect);
            EditorGUI.BeginProperty(foldoutRect, label, curveProperty);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);
            EditorGUI.EndProperty();
            
            // Draw a field next to the label so you can edit it straight away.
            EditorGUI.PropertyField(curveRect, curveProperty, GUIContent.none);
            
            // Draw the children, too.
            if (property.isExpanded)
            {
                Rect childrenRect = position.GetSubRectFromBottom(position.height - foldoutRect.height);
                property.PropertyFieldForChildren(childrenRect);
            }

            bool shouldUpdateTexture = EditorGUI.EndChangeCheck();

            bool isTextureMode = animationCurveTexture.Mode == AnimationCurveTexture.Modes.Texture;

            // Draw some of the more internal properties for you to tweak once.
            if (property.isExpanded)
            {
                // Draw the current texture.
                Texture2D texture = animationCurveTexture.Texture;
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
                    animationCurveTexture.GenerateTextureForCurve();
                    shouldUpdateTexture = false;
                }
                    
                // Draw the texture itself.
                if (texture == null)
                    EditorGUI.DrawRect(textureRect, Color.black);
                else
                    GUI.DrawTexture(textureRect, texture, ScaleMode.StretchToFill);
                
                AnimationCurveAsset asset =
                    (AnimationCurveAsset)property.FindPropertyRelative(CurvePropertyAsset).objectReferenceValue;
                
                Rect buttonRect = previewRect.GetControlNextRect().Indent(1);
                if (animationCurveTexture.Mode == AnimationCurveTexture.Modes.Local)
                {
                    // Draw a button to save the local curve to an asset.
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
                            SaveToAsset(curveProperty, property, pathToSaveTo);
                    }
                    
                    // Smaller button to pick a new asset to save to.
                    bool saveAs = GUI.Button(saveAsRect, "Save As...");
                    if (saveAs)
                        SaveToAsset(curveProperty, property, null, pathToSaveTo);
                }
                else if (animationCurveTexture.Mode == AnimationCurveTexture.Modes.Asset)
                {
                    // Draw a button to start editing a local copy of this asset.
                    bool wasGuiEnabled = GUI.enabled;
                    GUI.enabled = asset != null;
                    bool editLocalCopy = GUI.Button(buttonRect, "Edit Local Copy");
                    GUI.enabled = wasGuiEnabled;
                    if (editLocalCopy)
                    {
                        // Make a copy of the animation curve.
                        property.FindPropertyRelative(CurvePropertyLocal).animationCurveValue = asset.AnimationCurve;
                        
                        // Switch to local mode.
                        property.FindPropertyRelative("mode").enumValueIndex =
                            (int)AnimationCurveTexture.Modes.Local;
                    }
                }
                
                if (animationCurveTexture.Mode != AnimationCurveTexture.Modes.Texture)
                {
                    buttonRect = buttonRect.GetControlNextRect();
                    bool wasGuiEnabled = GUI.enabled;
                    GUI.enabled = asset != null || animationCurveTexture.Mode == AnimationCurveTexture.Modes.Local;
                    bool exportToTexture = GUI.Button(buttonRect, "Export To Texture");
                    string pathToSaveTo = asset == null ? "" : AssetDatabase.GetAssetPath(asset);
                    if (exportToTexture)
                        ExportToTexture(animationCurveTexture, property, null, pathToSaveTo);
                    GUI.enabled = wasGuiEnabled;
                }
            }

            // Re-bake the texture again if needed.
            if (shouldUpdateTexture)
            {
                // NOTE: Need to apply the modified properties otherwise it will generate a texture using old data.
                property.serializedObject.ApplyModifiedProperties();
                animationCurveTexture.GenerateTextureForCurve();
            }
        }

        private void SaveToAsset(
            SerializedProperty curveProperty, SerializedProperty owner, string path = null, string startingPath = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = EditorUtility.SaveFilePanelInProject(
                    "Save Animation Curve Asset", "Animation Curve", "asset",
                    "Save this local animation curve to a re-usable animation curve asset.", startingPath);
            }

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
            owner.FindPropertyRelative("mode").enumValueIndex =
                (int)AnimationCurveTexture.Modes.Asset;
            owner.FindPropertyRelative("animationCurveAsset").objectReferenceValue = asset;
            owner.serializedObject.ApplyModifiedProperties();
        }

        private void ExportToTexture(
            AnimationCurveTexture animationCurveTexture, SerializedProperty owner, string path = null,
            string startingPath = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = EditorUtility.SaveFilePanelInProject(
                    "Export AnimationCurve To Texture", "AnimationCurve", "png",
                    "Export this animationCurve as a texture.",
                    startingPath);
            }

            if (string.IsNullOrEmpty(path))
                return;

            // Write the texture to a PNG file.
            byte[] bytes = animationCurveTexture.Texture.EncodeToPNG();
            const string assetsFolder = "Assets";
            string absolutePath = path.Substring(assetsFolder.Length + 1);
            absolutePath = Application.dataPath + Path.AltDirectorySeparatorChar + absolutePath;
            File.WriteAllBytes(absolutePath, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            // Let's make sure we assign the right settings.
            TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(path);
            textureImporter.wrapMode = animationCurveTexture.WrapMode;
            textureImporter.filterMode = animationCurveTexture.FilterMode;
            textureImporter.mipmapEnabled = false;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            // Set the AnimationCurveTexture to Texture mode and assign the created texture.
            Texture2D textureAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            owner.serializedObject.Update();
            owner.FindPropertyRelative("mode").enumValueIndex = (int)AnimationCurveTexture.Modes.Texture;
            owner.FindPropertyRelative("texture").objectReferenceValue = textureAsset;
            owner.serializedObject.ApplyModifiedProperties();
        }
    }
}
