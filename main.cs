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

            return go;
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

        static void ExecuteApp_SSS(OpenTKApp app)
        {
            SetupEnvironment();

            SetupLights();

            float groundSize = 100f;
            CreateGround(groundSize);
            CreateSkysphere(groundSize * 4f);

            GameObject mech = ModelLoader.Load("Models/fishie/scene.gltf",
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
