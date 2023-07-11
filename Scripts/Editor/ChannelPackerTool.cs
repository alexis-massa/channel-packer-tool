using Codice.Client.BaseCommands.Import;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Color = UnityEngine.Color;
using Object = UnityEngine.Object;

namespace AlexisMassa
{
    public class ChannelPackerTool : EditorWindow
    {
        private ObjectField RInTex, GInTex, BInTex, AInTex;

        IDictionary<char, int> selectedChannels = new Dictionary<char, int>() { { 'r', 0 }, { 'g', 0 }, { 'b', 0 }, { 'a', 0 } };
        IDictionary<int, Texture2D> selectedTextures = new Dictionary<int, Texture2D>() { { 0, null }, { 1, null }, { 2, null }, { 3, null } };
        IDictionary<char, char> selectedOutputChannels = new Dictionary<char, char>() { { 'r', 'r' }, { 'g', 'g' }, { 'b', 'b' }, { 'a', 'a' } };

        private string[] radioNames = { "r0-radio", "g0-radio", "b0-radio", "a0-radio", "r1-radio", "g1-radio", "b1-radio", "a1-radio", "r2-radio", "g2-radio", "b2-radio", "a2-radio", "r3-radio", "g3-radio", "b3-radio", "a3-radio" };

        DropdownField ROutChannel, GOutChannel, BOutChannel, AOutChannel;
        private Texture2D outTex;
        private VisualElement RRadioGroup, GRadioGroup, BRadioGroup, ARadioGroup, outputPreview;
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

            // Input textures
            RInTex = root.Q<ObjectField>("input-img-0");
            GInTex = root.Q<ObjectField>("input-img-1");
            BInTex = root.Q<ObjectField>("input-img-2");
            AInTex = root.Q<ObjectField>("input-img-3");

            // Ouput channel dropdowns
            ROutChannel = root.Q<DropdownField>("r-out-channel");
            GOutChannel = root.Q<DropdownField>("g-out-channel");
            BOutChannel = root.Q<DropdownField>("b-out-channel");
            AOutChannel = root.Q<DropdownField>("a-out-channel");

            // Radio groups
            RRadioGroup = root.Q<VisualElement>("r-radio-group");
            GRadioGroup = root.Q<VisualElement>("g-radio-group");
            BRadioGroup = root.Q<VisualElement>("b-radio-group");
            ARadioGroup = root.Q<VisualElement>("a-radio-group");

            // Radio buttons and add them to their group
            for (int i = 0; i < radioNames.Length; i++)
            {
                string btnId = radioNames[i];
                RadioButton radioButton = root.Q<RadioButton>(btnId);
                radioButton.RegisterValueChangedCallback(evt => OnRadioButtonValueChanged(evt, btnId));
                radioButton.viewDataKey = btnId;

                switch (btnId[0])
                {
                    case 'r': RRadioGroup.Add(radioButton); break;
                    case 'g': GRadioGroup.Add(radioButton); break;
                    case 'b': BRadioGroup.Add(radioButton); break;
                    case 'a': ARadioGroup.Add(radioButton); break;
                    default: break;
                }

            }

            //Assign callbacks on input selection
            RInTex.RegisterValueChangedCallback<Object>(evt => TextureSelected(evt, 0));
            GInTex.RegisterValueChangedCallback<Object>(evt => TextureSelected(evt, 1));
            BInTex.RegisterValueChangedCallback<Object>(evt => TextureSelected(evt, 2));
            AInTex.RegisterValueChangedCallback<Object>(evt => TextureSelected(evt, 3));

            //Assign callbacks on output channel selection
            ROutChannel.RegisterValueChangedCallback(evt => OutputChannelSelected(evt, 'r'));
            GOutChannel.RegisterValueChangedCallback(evt => OutputChannelSelected(evt, 'g'));
            BOutChannel.RegisterValueChangedCallback(evt => OutputChannelSelected(evt, 'b'));
            AOutChannel.RegisterValueChangedCallback(evt => OutputChannelSelected(evt, 'a'));


            outputPreview = root.Q<VisualElement>("out-preview-img");
            packBtn = root.Q<Button>("pack-btn");
            exportBtn = root.Q<Button>("export-btn");
            outTex = null;
            outName = "packed_image";
            outputPreview.style.backgroundImage = null;

            exportBtn.clicked += () => ExportImage(outTex, outName);
            packBtn.clicked += PackImages;

        }


        private void PackImages()
        {
            // If channel selected on a null image
            foreach (var channel in selectedChannels) { if (selectedTextures[channel.Value] == null) { Debug.LogError("Do not select channels from images that are not imported."); return; } }

            // If multiple out put channels have been selected
            char[] alreadySelected = new char[4];
            foreach (var selectedOutputChannel in selectedOutputChannels)
            {
                if (Array.IndexOf(alreadySelected, selectedOutputChannel.Value) > -1) { Debug.LogError("Do not select an output channel more than once."); return; }
                else { Array.Resize(ref alreadySelected, alreadySelected.Length + 1); alreadySelected[alreadySelected.GetUpperBound(0)] = selectedOutputChannel.Value; }
            }

            // Get maxwidth & max height
            int maxWidth = new[] {
                    selectedTextures[0] != null ? selectedTextures[0].width : 0,
                    selectedTextures[1] != null ? selectedTextures[1].width : 0,
                    selectedTextures[2] != null ? selectedTextures[2].width : 0,
                    selectedTextures[3] != null ? selectedTextures[3].width : 0,
                }.Max();
            int maxHeight = new[] {
                    selectedTextures[0] != null ? selectedTextures[0].height : 0,
                    selectedTextures[1] != null ? selectedTextures[1].height : 0,
                    selectedTextures[2] != null ? selectedTextures[2].height : 0,
                    selectedTextures[3] != null ? selectedTextures[3].height : 0,
                }.Max();


            // create new Texture2D(maxwidth, maxheight)
            Texture2D packedImage = new Texture2D(maxWidth, maxHeight, TextureFormat.RGBA32, false);

            // foreach pixel set color to color of selected image's channel
            for (int y = 0; y < maxHeight; y++)
            {
                for (int x = 0; x < maxWidth; x++)
                {
                    float rc = 0;
                    float gc = 0;
                    float bc = 0;
                    float ac = 0;

                    // Get pixel from selected image's selected channel if [x,y] is in its range
                    // selected texture that has the selected channel that is the selected output corresponding to the channel
                    Texture2D image_r = selectedTextures[selectedChannels[selectedOutputChannels['r']]];
                    if (x <= image_r.width && y <= image_r.height) rc = image_r.GetPixel(x, y).r;
                    
                    Texture2D image_g = selectedTextures[selectedChannels[selectedOutputChannels['g']]];
                    if (x <= image_g.width && y <= image_g.height) gc = image_g.GetPixel(x, y).g;
                    
                    Texture2D image_b = selectedTextures[selectedChannels[selectedOutputChannels['b']]];
                    if (x <= image_b.width && y <= image_b.height) bc = image_b.GetPixel(x, y).b;
                    bc = selectedTextures[selectedChannels[selectedOutputChannels['b']]].GetPixel(x, y).b;
                    
                    Texture2D image_a = selectedTextures[selectedChannels[selectedOutputChannels['a']]];
                    if (x <= image_a.width && y <= image_a.height) ac = image_a.GetPixel(x, y).a;


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
            if (outTex == null) { Debug.LogWarning("Pack an image before trying to export it !"); return; }

            // Prompt to select a saving path
            string savePath = EditorUtility.SaveFilePanel("Save packed image", "", outName, "png");

            // Check if user cancelled the dialogue
            if (string.IsNullOrEmpty(savePath)) { return; }

            // write data
            byte[] pngData = outTex.EncodeToPNG();
            System.IO.File.WriteAllBytes(savePath, pngData);

        }

        #region Callbacks
        private void OnRadioButtonValueChanged(ChangeEvent<bool> evt, string btnId) { if (evt.newValue) { selectedChannels[btnId[0]] = (int)Char.GetNumericValue(btnId[1]); } }

        private void TextureSelected(ChangeEvent<Object> evt, int id)
        {
            if (evt.newValue == null) { selectedTextures[id] = null; return; }
            selectedTextures[id] = evt.newValue as Texture2D;
        }

        private void OutputChannelSelected(ChangeEvent<string> evt, char selectedChannel) { selectedOutputChannels[selectedChannel] = Char.ToLower(evt.newValue[0]); }
        #endregion
    }

}