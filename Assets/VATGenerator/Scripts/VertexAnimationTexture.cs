using UnityEngine;
using System.Collections.Generic;

namespace VATGenerator
{
    public class VertexAnimationTexture : System.IDisposable
    {
        #region Field

        public const float      COLOR_DEPTH     = 255f;
        public const float      COLOR_DEPTH_INV = 1f / COLOR_DEPTH;

        public readonly Vector2[] uv2;
        public readonly Vector4   scale;
        public readonly Vector4   offset;
        public readonly Texture2D positionTex;
        public readonly Texture2D normalTex;
        public readonly float     frameEnd;

        public readonly List<Vector3[]> verticesList;
        public readonly List<Vector3[]> normalsList;

        #endregion Field

        #region Constructor

        public VertexAnimationTexture(IMeshSampler sampler, float fps = 30f)
        {
            // NOTE:
            // Get Verticies & Normals.

            this.verticesList = new List<Vector3[]>();
            this.normalsList  = new List<Vector3[]>();

            float deltaTime = 1f / fps;

            for (float t = 0; t < sampler.Length + deltaTime; t += deltaTime)
            {
                Matrix4x4 meshPosition, meshNormal;

                Mesh      mesh     = sampler.Sample(t, out meshPosition, out meshNormal);
                Vector3[] vertices = mesh.vertices;
                Vector3[] normals  = mesh.normals;

                for (var i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = meshPosition.MultiplyPoint3x4(vertices[i]);
                    normals[i]  = meshNormal.MultiplyVector(normals[i]);
                }

                this.verticesList.Add(vertices);
                this.normalsList.Add(mesh.normals);
            }

            Vector3[] firstVertices = verticesList[0];
            Vector3   firstVertex   = firstVertices[0];
            int       vertexCount   = firstVertices.Length;

            this.frameEnd = vertexCount - 1;

            // NOTE:
            // Get Min/Max coord.

            float minX = firstVertex.x;
            float minY = firstVertex.y;
            float minZ = firstVertex.z;
            float maxX = firstVertex.x;
            float maxY = firstVertex.y;
            float maxZ = firstVertex.z;

            foreach (Vector3[] vertices in this.verticesList)
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 vertex = vertices[i];
                    minX = Mathf.Min(minX, vertex.x);
                    minY = Mathf.Min(minY, vertex.y);
                    minZ = Mathf.Min(minZ, vertex.z);
                    maxX = Mathf.Max(maxX, vertex.x);
                    maxY = Mathf.Max(maxY, vertex.y);
                    maxZ = Mathf.Max(maxZ, vertex.z);
                }
            }

            this.scale  = new Vector4(maxX - minX, maxY - minY, maxZ - minZ, 1f);
            this.offset = new Vector4(minX, minY, minZ, 1f);

            // NOTE:
            // Generate Texture2D

            int texWidth  = NearPow2(vertexCount);
            int texHeight = NearPow2(verticesList.Count * 2);

            positionTex = new Texture2D(texWidth, texHeight, TextureFormat.RGB24, false, true);
            normalTex   = new Texture2D(texWidth, texHeight, TextureFormat.RGB24, false, true);

            this.uv2 = new Vector2[vertexCount];

            var texSize       = new Vector2(1f / texWidth, 1f / texHeight);
            var halfTexOffset = 0.5f * texSize;

            for (int i = 0; i < uv2.Length; i++)
            {
                uv2[i] = new Vector2((float)i * texSize.x, 0f) + halfTexOffset;
            }

            for (int y = 0; y < verticesList.Count; y++)
            {
                Vector3[] vertices = verticesList[y];
                Vector3[] normals  = normalsList[y];

                for (int x = 0; x < vertices.Length; x++)
                {
                    var pos = Normalize(vertices [x], offset, scale);

                    Color c0, c1;
                    Encode(pos, out c0, out c1);
                    positionTex.SetPixel(x, y, c0);
                    positionTex.SetPixel(x, y + (texHeight >> 1), c1);

                    var normal = 0.5f * (normals [x].normalized + Vector3.one);

                    Encode(normal, out c0, out c1);
                    normalTex.SetPixel(x, y, c0);
                    normalTex.SetPixel(x, y + (texHeight >> 1), c1);
                }
            }

            positionTex.Apply();
            normalTex.Apply();
        }

        #endregion Constructor

        #region Method

        public Vector3 Position(int vid, float frame)
        {
            // NOTE:
            // for Debug.

            frame = Mathf.Clamp(frame, 0f, frameEnd);

            var uv    = uv2[vid];
                uv.y += frame * positionTex.texelSize.y;

            var pos1 = positionTex.GetPixelBilinear(uv.x, uv.y);
            var pos2 = positionTex.GetPixelBilinear(uv.x, uv.y + 0.5f);

            return new Vector3
                ((pos1.r + pos2.r / COLOR_DEPTH) * scale.x + offset.x,
                 (pos1.g + pos2.g / COLOR_DEPTH) * scale.y + offset.y,
                 (pos1.b + pos2.b / COLOR_DEPTH) * scale.z + offset.z);
        }

        public Bounds Bounds()
        {
            return new Bounds (0.5f * this.scale + this.offset, this.scale);
        }

        public Vector3[] Vertices(float frame)
        {
            frame = Mathf.Clamp(frame, 0f, frameEnd);

            var index    = Mathf.Clamp((int)frame, 0, this.verticesList.Count - 1);
            var vertices = this.verticesList[index];

            return vertices;
        }

        public static Vector3 Normalize(Vector3 position, Vector3 offset, Vector3 scale)
        {
            return new Vector3 ((position.x - offset.x) / scale.x,
                                (position.y - offset.y) / scale.y,
                                (position.z - offset.z) / scale.z);
        }

        public static void Encode(float v01, out float c0, out float c1)
        {
            c0 = Mathf.Clamp01(Mathf.Floor(v01 * COLOR_DEPTH) * COLOR_DEPTH_INV);
            c1 = Mathf.Clamp01(Mathf.Round((v01 - c0) * COLOR_DEPTH * COLOR_DEPTH) * COLOR_DEPTH_INV);
        }

        public static void Encode(Vector3 v01, out Color c0, out Color c1)
        {
            float c0x, c0y, c0z, c1x, c1y, c1z;

            Encode(v01.x, out c0x, out c1x);
            Encode(v01.y, out c0y, out c1y);
            Encode(v01.z, out c0z, out c1z);

            c0 = new Color(c0x, c0y, c0z, 1f);
            c1 = new Color(c1x, c1y, c1z, 1f);
        }

        static int NearPow2(int width)
        {
            width--;
            int digits = 0;

            while (width > 0)
            {
                width >>= 1;
                digits++;
            }

            return 1 << digits;
        }

        public void Dispose()
        {
            GameObject.Destroy(positionTex);
            GameObject.Destroy(normalTex);
        }

        #endregion Method
    }
}