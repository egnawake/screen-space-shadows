﻿using System;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace OpenTKBase
{
    public class RPS : RenderPipeline
    {
        static private Material shadowmapMaterial;
        static private Material depthMaterial;
        static private Texture  defaultShadowmap;

        private Material GetShadowmapMaterial()
        {
            if (shadowmapMaterial == null)
            {
                shadowmapMaterial = new Material(Shader.Find("shaders/std_shadowmap"));
            }

            return shadowmapMaterial;
        }

        private Material GetDepthMaterial()
        {
            if (depthMaterial == null)
            {
                depthMaterial = new Material(Shader.Find("shaders/std_shadowmap"));
            }

            return depthMaterial;
        }

        private Texture GetDefaultShadowmap()
        {
            if (defaultShadowmap == null)
            {
                defaultShadowmap = new Texture();
                defaultShadowmap.CreateDepth(1, 1, true);
            }

            return defaultShadowmap;
        }

        public override void Render(Scene scene)
        {
            if (scene == null) return;

            var allCameras = scene.FindObjectsOfType<Camera>();
            var allRender = scene.FindObjectsOfType<Renderable>();
            var allLights = scene.FindObjectsOfType<Light>();

            Material depthMaterial = GetDepthMaterial();

            // Render depth textures
            foreach (var camera in allCameras)
            {
                // Set as render target
                camera.depthTex.Set(-1);

                GL.ClearDepth(camera.GetClearDepth());
                GL.Clear(camera.GetClearFlags());

                Shader.SetMatrix(Shader.MatrixType.Camera, camera.transform.worldToLocalMatrix);
                Shader.SetMatrix(Shader.MatrixType.InvCamera, camera.transform.localToWorldMatrix);
                Shader.SetMatrix(Shader.MatrixType.Projection, camera.projection);

                foreach (var render in allRender)
                {
                    render.Render(camera, depthMaterial);
                }

                camera.depthTex.Unset();
            }

            Material envMaterial = OpenTKApp.APP.mainScene.environment;
            envMaterial.Set("Depth", allCameras[0].depthTex.GetDepthTexture());

            // Invert cull mode for shadowmap rendering (only works if objects are all "solid")
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Front);

            // Render shadowmaps, if needed
            foreach (var light in allLights)
            {
                if (light.HasShadowmap())
                {
                    // Get shadowmap material
                    var shadowmapMaterial = GetShadowmapMaterial();

                    if (light.type == Light.Type.Spot)
                    {
                        // Setup rendertarget
                        var shadowmap = light.GetShadowmap();
                        shadowmap.Set(-1);

                        GL.ClearDepth(1.0f);
                        GL.Clear(ClearBufferMask.DepthBufferBit);

                        Shader.SetMatrix(Shader.MatrixType.Camera, light.transform.worldToLocalMatrix);
                        Shader.SetMatrix(Shader.MatrixType.InvCamera, light.transform.localToWorldMatrix);
                        Shader.SetMatrix(Shader.MatrixType.Projection, light.GetSpotlightProjection());

                        foreach (var render in allRender)
                        {
                            render.Render(null, shadowmapMaterial);
                        }

                        shadowmap.Unset();
                    }
                    else
                    {
                        Console.WriteLine($"Unsupported light type {light.type} for shadowmap!");
                    }
                }
            }

            // Restore cull mode to normal
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            envMaterial.Set("LightCount", allLights.Count);
            for (int i = 0; i < Math.Min(allLights.Count, 8); i++)
            {
                var light = allLights[i];
                envMaterial.Set($"Lights[{i}].type", (int)light.type);
                envMaterial.Set($"Lights[{i}].position", light.transform.position);
                envMaterial.Set($"Lights[{i}].direction", light.transform.forward);
                envMaterial.Set($"Lights[{i}].color", light.lightColor);
                envMaterial.Set($"Lights[{i}].intensity", light.intensity);
                envMaterial.Set($"Lights[{i}].spot", light.cone.X * 0.5f, light.cone.Y * 0.5f, MathF.Cos(light.cone.X * 0.5f), MathF.Cos(light.cone.Y * 0.5f));
                envMaterial.Set($"Lights[{i}].range", light.range);
                envMaterial.Set($"Lights[{i}].shadowmapEnable", light.HasShadowmap());
                if (light.HasShadowmap())
                {
                    envMaterial.Set($"Lights[{i}].shadowmap", light.GetShadowmap().GetDepthTexture());
                }
                else
                {
                    envMaterial.Set($"Lights[{i}].shadowmap", GetDefaultShadowmap().GetDepthTexture());
                }
                envMaterial.Set($"Lights[{i}].shadowMatrix", light.GetShadowMatrix());
            }
            for (int i = Math.Min(allLights.Count, 8); i < 8; i++)
            {
                envMaterial.Set($"Lights[{i}].shadowmap", GetDefaultShadowmap().GetDepthTexture());
            }

            GL.Viewport(0, 0, OpenTKApp.APP.resX, OpenTKApp.APP.resY);

            foreach (var camera in allCameras)
            {
                // Clear color buffer and the depth buffer
                GL.ClearColor(camera.GetClearColor());
                GL.ClearDepth(camera.GetClearDepth());
                GL.Clear(camera.GetClearFlags());

                Vector4 zParams = new Vector4();
                zParams.Y = camera.farPlane / camera.nearPlane;
                zParams.X = 1.0f - zParams.Y;
                zParams.Z = zParams.X / camera.farPlane;
                zParams.W = zParams.Y / camera.farPlane;
                envMaterial.Set("ZBufferParams", zParams);

                Vector2 cameraParams = new Vector2(camera.nearPlane, camera.farPlane);
                envMaterial.Set("CameraParams", cameraParams);

                Shader.SetMatrix(Shader.MatrixType.Camera, camera.transform.worldToLocalMatrix);
                Shader.SetMatrix(Shader.MatrixType.InvCamera, camera.transform.localToWorldMatrix);
                Shader.SetMatrix(Shader.MatrixType.Projection, camera.projection);

                foreach (var render in allRender)
                {
                    render.Render(camera, null);
                }
            }
        }
    }
}
