using OpenTK.Mathematics;
using OpenTKBase;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SDLBase
{
    public static class OpenTKProgram
    {
        public static void Main()
        {
            OpenTKApp app = new OpenTKApp(1280, 720, "Screen space shadows", true);

            app.Initialize();
            app.LockMouse(true);

            ExecuteApp_SSS(app);

            app.Shutdown();
        }

        static GameObject CreateGround(float size)
        {
            Mesh mesh = GeometryFactory.AddPlane(size, size, 128, true);
            mesh.ComputeNormalsAndTangentSpace();

            Texture grassTexture = new Texture(OpenTK.Graphics.OpenGL.TextureWrapMode.Repeat, OpenTK.Graphics.OpenGL.TextureMinFilter.Linear, true);
            grassTexture.Load("Textures/dirt_floor_diff_1k.png");
            Texture grassNormal = new Texture(OpenTK.Graphics.OpenGL.TextureWrapMode.Repeat, OpenTK.Graphics.OpenGL.TextureMinFilter.Linear, true);
            grassNormal.Load("Textures/dirt_floor_nor_gl_1k.png");

            Material material = new Material(Shader.Find("Shaders/phong_pp_sss"));
            material.Set("Color", Color4.White);
            material.Set("ColorEmissive", Color4.Black);
            material.Set("Specular", new Vector2(2.0f, 128.0f));
            material.Set("BaseColor", grassTexture);
            material.Set("NormalMap", grassNormal);
            material.Set("Tiling", new Vector2(10f, 10f));

            GameObject go = new GameObject();
            go.transform.position = new Vector3(0, 0, 0);
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.material = material;

            return go;
        }

        static float Range(this Random rnd, float a, float b)
        {
            return rnd.NextSingle() * (b - a) + a;
        }

        static void CreateRandomTree(Random rnd, float forestSize)
        {
            float s = forestSize * 0.2f;

            // Trunk
            float heightTrunk = rnd.Range(0.5f, 1.5f);
            float widthTrunk = rnd.Range(0.7f, 1.25f);

            Mesh mesh = GeometryFactory.AddCylinder(widthTrunk, heightTrunk, 8, true);

            Material material = new Material(Shader.Find("Shaders/phong_pp_sss"));
            material.Set("Color", new Color4(rnd.Range(0.6f, 0.9f), rnd.Range(0.4f, 0.6f), rnd.Range(0.15f, 0.35f), 1.0f));
            material.Set("ColorEmissive", Color4.Black);
            material.Set("Specular", Vector2.UnitY);

            GameObject mainObject = new GameObject();
            mainObject.transform.position = new Vector3(rnd.Range(-s, s), 0, rnd.Range(-s, s));
            MeshFilter mf = mainObject.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            MeshRenderer mr = mainObject.AddComponent<MeshRenderer>();
            mr.material = material;

            // Leaves
            mesh = GeometryFactory.AddCylinder(rnd.Range(widthTrunk * 1.5f, widthTrunk * 4.0f), rnd.Range(heightTrunk * 2.0f, heightTrunk * 8.0f), 16, true);

            material = new Material(Shader.Find("Shaders/phong_pp_sss"));
            material.Set("Color", new Color4(rnd.Range(0.0f, 0.2f), rnd.Range(0.6f, 0.8f), rnd.Range(0.0f, 0.2f), 1.0f));
            material.Set("ColorEmissive", Color4.Black);
            material.Set("Specular", Vector2.UnitY);

            GameObject leaveObj = new GameObject();
            leaveObj.transform.position = mainObject.transform.position + Vector3.UnitY * heightTrunk;
            leaveObj.transform.SetParent(mainObject.transform);
            mf = leaveObj.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            mr = leaveObj.AddComponent<MeshRenderer>();
            mr.material = material;
        }

        static void SetupEnvironment()
        {
            var cubeMap = new Texture();
            cubeMap.LoadCube("Textures/arid2/arid2_*.jpg");

            var env = OpenTKApp.APP.mainScene.environment;

            env.Set("Color", new Color4(0.2f, 0.2f, 0.2f, 1.0f));
            env.Set("ColorTop", new Color4(0.0f, 1.0f, 1.0f, 1.0f));
            env.Set("ColorMid", new Color4(1.0f, 1.0f, 1.0f, 1.0f));
            env.Set("ColorBottom", new Color4(0.0f, 0.25f, 0.0f, 1.0f));
            env.Set("FogDensity", 0.000001f);
            env.Set("FogColor", Color.DarkCyan);
            env.Set("CubeMap", cubeMap);
        }

        static void SetupLights()
        {
            GameObject l = CreateSpotLight(2.0f);
            l.transform.rotation = Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(30f))
                * Quaternion.FromAxisAngle(Vector3.UnitX, MathHelper.DegreesToRadians(-45f));
            l.transform.position = new Vector3(2f, 4f, 2f);
        }

        static GameObject CreateSpotLight(float intensity)
        {
            GameObject go = new GameObject();
            Light light = go.AddComponent<Light>();
            light.type = Light.Type.Spot;
            light.lightColor = Color.White;
            light.intensity = intensity;
            light.range = 200;
            light.cone = new Vector2(0.0f, MathF.PI / 2.0f);
            light.SetShadow(true, 2048);

            (GameObject sphere, Material sphereMaterial) = CreateSphere();
            sphereMaterial.Set("ColorEmissive", Color4.White);
            sphere.transform.parent = go.transform;
            sphere.transform.localPosition = Vector3.Zero;

            return go;
        }

        static GameObject CreateDirectionalLight(float intensity)
        {
            GameObject go = new GameObject();
            Light light = go.AddComponent<Light>();
            light.type = Light.Type.Directional;
            light.lightColor = Color.White;
            light.intensity = intensity;

            return go;
        }

        static GameObject CreatePointLight(float intensity)
        {
            GameObject go = new GameObject();
            Light light = go.AddComponent<Light>();
            light.type = Light.Type.Point;
            light.lightColor = Color.White;
            light.intensity = intensity;
            light.range = 200;

            (GameObject sphere, Material sphereMaterial) = CreateSphere();
            sphereMaterial.Set("ColorEmissive", Color4.White);
            sphere.transform.parent = go.transform;
            sphere.transform.localPosition = Vector3.Zero;

            return go;
        }

        static (GameObject, Material) CreateSphere()
        {
            Mesh mesh = GeometryFactory.AddSphere(0.5f, 32, true);

            Material material = new Material(Shader.Find("Shaders/phong_pp_sss"));
            material.Set("Color", Color4.White);
            material.Set("ColorEmissive", Color4.Black);
            material.Set("Specular", Vector2.UnitY);

            GameObject go = new GameObject();
            go.transform.position = new Vector3(0, 2, -5);
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.material = material;

            return (go, material);
        }

        static void CreateSkysphere(float radius)
        {
            Mesh mesh = GeometryFactory.AddSphere(radius, 64, true);

            Material material = new Material(Shader.Find("Shaders/skysphere_envmap"));

            GameObject go = new GameObject();
            go.transform.position = new Vector3(0, 0, 0);
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.material = material;
        }

        static GameObject CreateForest(GameObject light)
        {
            float forestSize = 120.0f;

            // Create skysphere
            CreateSkysphere(forestSize * 4.0f);

            // Create ground
            var ret = CreateGround(forestSize);

            // Create a sphere in the middle of the forest
            /*var (reflectSphere, reflectMaterial) = CreateSphere();
            var (glowSphere, glowMaterial) = CreateSphere();
            glowSphere.transform.position = light.transform.position;
            glowMaterial.Set("Color", Color4.Black);
            glowMaterial.Set("ColorEmissive", Color4.Yellow);*/

            // Create trees
            Random rnd = new Random(1);
            for (int i = 0; i < 50; i++)
            {
                CreateRandomTree(rnd, forestSize);
            }

            return ret;
        }

        static Mesh ConvertMesh(Assimp.Mesh src)
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

        static GameObject LoadModel(string modelPath, string baseColorPath = "",
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

        static void ExecuteApp_SSS(OpenTKApp app)
        {
            SetupEnvironment();

            SetupLights();

            float groundSize = 100f;
            CreateGround(groundSize);
            CreateSkysphere(groundSize * 4f);

            GameObject mech = LoadModel("Models/fishie/scene.gltf",
                "Models/fishie/textures/Material_baseColor.png",
                "Models/fishie/textures/Material_normal.png");
            mech.transform.rotation = Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(20f));

            // Create camera
            GameObject cameraObject = new GameObject();
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 1.2f, 2.75f);
            camera.ortographic = false;
            camera.InitDepthTexture(app.resX, app.resY);
            FirstPersonController fps = cameraObject.AddComponent<FirstPersonController>();

            // Create pipeline
            RPS renderPipeline = new RPS();

            app.Run(() =>
            {
                app.Render(renderPipeline);
            });
        }
    }
}
