using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;

namespace OpenTKBase
{
    public static class ModelLoader
    {
        /// <summary>
        /// Creates a GameObject with a loaded 3D model. Uses the Assimp library to read the
        /// supplied 3D data file.
        /// </summary>
        /// <param name="modelPath">Path to the 3D data file.</param>
        /// <param name="baseColorPath">Path to the base color texture file.</param>
        /// <param name="normalMapPath">Path to the normal map texture file.</param>
        /// <returns>A GameObject with MeshFilter and MeshRenderer components.</returns>
        public static GameObject Load(string modelPath, string baseColorPath = "",
            string normalMapPath = "")
        {
            // Import model file
            Assimp.AssimpContext importer = new Assimp.AssimpContext();
            Assimp.Scene scene = importer.ImportFile(modelPath, Assimp.PostProcessSteps.FlipUVs);

            Console.WriteLine($"[Assimp] Loaded {modelPath}");

            // Create engine mesh
            Assimp.Mesh assimpMesh = scene.Meshes[0];
            Mesh m = ConvertMesh(assimpMesh);

            // Set up game object
            GameObject go = new GameObject();

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.mesh = m;

            // Set up material
            Material material = new Material(Shader.Find("Shaders/phong_pp_sss"));

            material.Set("Color", Color4.White);
            material.Set("Specular", Vector2.UnitY);
            material.Set("ColorEmissive", Color4.Black);
            material.Set("Tiling", new Vector2(1f, 1f));

            // Load textures
            if (baseColorPath.Length > 0)
            {
                Texture baseColor = new Texture(OpenTK.Graphics.OpenGL.TextureWrapMode.Repeat,
                    OpenTK.Graphics.OpenGL.TextureMinFilter.Linear, true);
                baseColor.Load(baseColorPath);
                material.Set("BaseColor", baseColor);
            }

            if (normalMapPath.Length > 0)
            {
                Texture normalMap = new Texture(OpenTK.Graphics.OpenGL.TextureWrapMode.Repeat, OpenTK.Graphics.OpenGL.TextureMinFilter.Linear, true);
                normalMap.Load(normalMapPath);
                material.Set("NormalMap", normalMap);
            }

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.material = material;

            return go;
        }

        private static Mesh ConvertMesh(Assimp.Mesh src)
        {
            Mesh m = new Mesh();
            m.SetVertices(src.Vertices
                .Select((Assimp.Vector3D v) => new Vector3(v.X, v.Y, v.Z))
                .ToList());
            m.SetIndices(new List<uint>(src.GetUnsignedIndices()));
            m.SetNormals(src.Normals
                .Select((Assimp.Vector3D n) => new Vector3(n.X, n.Y, n.Z))
                .ToList());
            m.SetUVs(src.TextureCoordinateChannels[0]
                .Select((Assimp.Vector3D uv) => new Vector2(uv.X, uv.Y))
                .ToList());

            return m;
        }
    }
}
