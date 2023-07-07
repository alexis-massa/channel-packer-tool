using System;
using System.ComponentModel.Composition.Primitives;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static Codice.Client.BaseCommands.Import.Commit;
using Color = UnityEngine.Color;
using Object = UnityEngine.Object;

namespace AlexisMassa
{
    public class ChannelPackerTool : EditorWindow
    {
        private ObjectField RInTex, GInTex, BInTex, AInTex;
        private Texture2D RSelected, GSelected, BSelected, ASelected, outTex;
        private VisualElement RPreviewImg, GPreviewImg, BPreviewImg, APreviewImg, outputPreview;
        private Button packBtn, exportBtn;
        private string outName;

        [MenuItem("Tools/Channel Packer Tool")]
        public static void OpenEditorWindow()
        {
            ChannelPackerTool cpt = GetWindow<ChannelPackerTool>();
            cpt.titleContent = new GUIContent("ChannelPackerTool Packer Tool");
            cpt.maxSize = new Vector2(666, 850);
            cpt.minSize = cpt.maxSize;
        }

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/ChannelPackerTool/Resources/UI Documents/ChannelPackerToolWindow.uxml");
            VisualElement tree = visualTree.Instantiate();
            root.Add(tree);

            // Assign elements
            RInTex = root.Q<ObjectField>("r-input");
            GInTex = root.Q<ObjectField>("g-input");
            BInTex = root.Q<ObjectField>("b-input");
            AInTex = root.Q<ObjectField>("a-input");
            RPreviewImg = root.Q<VisualElement>("r-preview-img");
            GPreviewImg = root.Q<VisualElement>("g-preview-img");
            BPreviewImg = root.Q<VisualElement>("b-preview-img");
            APreviewImg = root.Q<VisualElement>("a-preview-img");
            outputPreview = root.Q<VisualElement>("out-preview-img");
            packBtn = root.Q<Button>("pack-btn");
            exportBtn = root.Q<Button>("export-btn");

            //Assign callbacks
            RInTex.RegisterValueChangedCallback<Object>(RTextureSelected);
            GInTex.RegisterValueChangedCallback<Object>(GTextureSelected);
            BInTex.RegisterValueChangedCallback<Object>(BTextureSelected);
            AInTex.RegisterValueChangedCallback<Object>(ATextureSelected);

            RPreviewImg.style.backgroundImage = null;
            GPreviewImg.style.backgroundImage = null;
            BPreviewImg.style.backgroundImage = null;
            APreviewImg.style.backgroundImage = null;
            outTex = null;
            outName = "packed_image";
            outputPreview.style.backgroundImage = null;

            exportBtn.clicked += () => ExportImage(outTex, outName);
            packBtn.clicked += PackImages;

        }

        private void PackImages()
        {
            // Add better check, maybe allow packing of x images and leave 4 - x blank channels ?
            if (RSelected == null || GSelected == null || BSelected == null || ASelected == null) { Debug.LogError("All images aren't imported"); return; }

            // TODO : Import Textures first
            int maxWidth = new[] { RSelected.width, GSelected.width, BSelected.width, ASelected.width }.Max();
            int maxHeight = new[] { RSelected.height, GSelected.height, BSelected.height, ASelected.height }.Max();

            Texture2D packedImage = new Texture2D(maxWidth, maxHeight, TextureFormat.RGBA32, false);
            // For each pixel
            for (int y = 0; y < maxHeight; y++)
            {
                for (int x = 0; x < maxWidth; x++)
                {
                    // Get pixel and convert to grayscale
                    float rc = RSelected.GetPixel(x, y).r;
                    float gc = GSelected.GetPixel(x, y).g;
                    float bc = BSelected.GetPixel(x, y).b;
                    float ac = ASelected.GetPixel(x, y).a;

                    // Create color with values
                    Color packedColor = new Color(rc, gc, bc, ac);

                    // Add pixel to image
                    packedImage.SetPixel(x, y, packedColor);
                }
            }
            // Apply and preview
            packedImage.Apply();
            outTex = packedImage;
            outputPreview.style.backgroundImage = packedImage;
        }


        private void ExportImage(Texture2D outTex, string outName)
        {
            // Stop if outTexture hasn'hasn't been generated
            if (outTex == null) { Debug.Log("Pack an image before trying to export it !"); return; }

            // Prompt to select a saving path
            string savePath = EditorUtility.SaveFilePanel("Save packed image", "", outName, "png");

            // Check is user cancelled the dialogue
            if (string.IsNullOrEmpty(savePath)) { return; }

            // write data
            byte[] pngData = outTex.EncodeToPNG();
            System.IO.File.WriteAllBytes(savePath, pngData);

        }

        #region texture selection events
        private void RTextureSelected(ChangeEvent<Object> evt)
        {
            if (evt.newValue == null)
            {
                RSelected = null;
                RPreviewImg.style.backgroundImage = null;
                return;
            }
            RSelected = evt.newValue as Texture2D;
            RPreviewImg.style.backgroundImage = RSelected;
        }
        private void GTextureSelected(ChangeEvent<Object> evt)
        {
            if (evt.newValue == null)
            {
                GSelected = null;
                GPreviewImg.style.backgroundImage = null;
                return;
            }
            GSelected = evt.newValue as Texture2D;
            GPreviewImg.style.backgroundImage = GSelected;

        }
        private void BTextureSelected(ChangeEvent<Object> evt)
        {
            if (evt.newValue == null)
            {
                BSelected = null;
                BPreviewImg.style.backgroundImage = null;
                return;
            }
            BSelected = evt.newValue as Texture2D;
            BPreviewImg.style.backgroundImage = BSelected;

        }
        private void ATextureSelected(ChangeEvent<Object> evt)
        {
            if (evt.newValue == null)
            {
                ASelected = null;
                APreviewImg.style.backgroundImage = null;
                return;
            }
            ASelected = evt.newValue as Texture2D;
            APreviewImg.style.backgroundImage = ASelected;

        }
        #endregion
    }

}