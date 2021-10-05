using System.Collections;
using System.IO;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

namespace VATGenerator
{
    public class VATGeneratorEditor : EditorWindow
    {
        #region Field

        private float fps        = 30f;
        private bool  singleMesh = true;

        private const FilterMode ANIM_TEX_FILTER = FilterMode.Bilinear;
        private const string     DIR_ASSETS      = "Assets";
        private const string     DIR_ROOT        = "VAT";

        #endregion Field

        #region Method

        [MenuItem("Custom/VATGenerator")]
        static void Init()
        {
            EditorWindow.GetWindow<VATGeneratorEditor>("VATGenerator");
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("FPS");
            this.fps = EditorGUILayout.FloatField(this.fps);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("SingleMesh");
            this.singleMesh = EditorGUILayout.Toggle(this.singleMesh);

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Generate"))
            {
                GameObject selectedGameObject = Selection.activeGameObject;

                if (selectedGameObject == null)
                {
                    Debug.Log("No Active GameObject");
                    return;
                }

                EditorCoroutineUtility.StartCoroutine(GenerateVertexTexture(selectedGameObject, fps, singleMesh), this);
            }
        }

        public static IEnumerator GenerateVertexTexture(GameObject selectedGameObject,
                                                        float      fps,
                                                        bool       singleMesh)
        {
            IMeshSampler sampler = singleMesh ?
                                   (IMeshSampler)new CombinedMeshSampler(selectedGameObject):
                                   (IMeshSampler)new SingleMeshSampler  (selectedGameObject);

            VertexAnimationTexture vat = new VertexAnimationTexture(sampler, fps);
        
            string directoryPath = DIR_ASSETS + "/" + DIR_ROOT;

            if (!Directory.Exists(directoryPath))
            {
                AssetDatabase.CreateFolder(DIR_ASSETS, DIR_ROOT);
            }

            string guid = AssetDatabase.CreateFolder(directoryPath, selectedGameObject.name);

            directoryPath = AssetDatabase.GUIDToAssetPath(guid);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string posPngPath = directoryPath + "/" + selectedGameObject.name + ".png";
            string nrmPngPath = directoryPath + "/" + selectedGameObject.name + "_normal.png";

            Texture2D posTex  = SaveTexture(vat.positionTex, posPngPath);
            Texture2D normTex = SaveTexture(vat.normalTex,   nrmPngPath);

            var renderer = selectedGameObject.GetComponentInChildren<Renderer>();

            Material material = new Material(Shader.Find(VATShader.NAME));

            if (renderer != null && renderer.sharedMaterial != null)
            {
                material.mainTexture = renderer.sharedMaterial.mainTexture;
            }

            material.SetTexture(VATShader.ANIMTEX,           posTex);
            material.SetTexture(VATShader.ANIMTEX_NORMALTEX, normTex);
            material.SetVector (VATShader.ANIMTEX_SCALE,     vat.scale);
            material.SetVector (VATShader.ANIMTEX_OFFSET,    vat.offset);
            material.SetVector (VATShader.ANIMTEX_ANIMEND,   new Vector4(sampler.Length, vat.verticesList.Count - 1, 0f, 0f));
            material.SetFloat  (VATShader.ANIMTEX_FPS,       fps);

            AssetDatabase.CreateAsset(material, directoryPath + "/" + selectedGameObject.name + "Mat.mat");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Mesh mesh = sampler.Output;
                 mesh.bounds = vat.Bounds();
            AssetDatabase.CreateAsset(mesh, directoryPath + "/" + selectedGameObject.name + ".asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameObject vatGameObject = new GameObject(selectedGameObject.name);
            vatGameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            vatGameObject.AddComponent<MeshFilter>().sharedMesh       = mesh;

            PrefabUtility.SaveAsPrefabAsset(vatGameObject, directoryPath + "/" + selectedGameObject.name + ".prefab");

            yield return 0;
        }

        static Texture2D SaveTexture(Texture2D texture, string texturePath)
        {
            File.WriteAllBytes(texturePath, texture.EncodeToPNG());

            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);

            var textureImporter         = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            var textureImporterSettings = new TextureImporterSettings();

            textureImporter.ReadTextureSettings(textureImporterSettings);

            textureImporterSettings.filterMode    = ANIM_TEX_FILTER;
            textureImporterSettings.mipmapEnabled = false;
            textureImporterSettings.sRGBTexture   = false;
            textureImporterSettings.wrapMode      = TextureWrapMode.Clamp;

            var platformTextureSettings                    = textureImporter.GetDefaultPlatformTextureSettings();
                platformTextureSettings.format             = TextureImporterFormat.RGB24;
                platformTextureSettings.textureCompression = TextureImporterCompression.Uncompressed;
                platformTextureSettings.maxTextureSize     = Mathf.Max(platformTextureSettings.maxTextureSize,
                                                             Mathf.Max(texture.width,
                                                                       texture.height));

            textureImporter.SetTextureSettings (textureImporterSettings);
            textureImporter.SetPlatformTextureSettings(platformTextureSettings);
            textureImporter.SaveAndReimport();

            return (Texture2D)AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D));
        }

        #endregion Method
    }
}