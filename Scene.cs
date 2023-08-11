﻿using OpenTK.Mathematics;
using System.Collections.Generic;

namespace OpenTKBase
{
    public class Scene
    {
        private List<GameObject> objects = new List<GameObject>();

        public Material environment { get; private set; }

        public Scene()
        {
            environment = new Material(null);
            environment.Set("LightCount", 0);
            for (int i = 0; i < 8; i++)
            {
                environment.Set($"Lights[{i}].type", 0);
                environment.Set($"Lights[{i}].position", Vector3.Zero);
                environment.Set($"Lights[{i}].direction", Vector3.UnitZ);
                environment.Set($"Lights[{i}].color", Color4.White);
                environment.Set($"Lights[{i}].intensity", 1.0f);
                environment.Set($"Lights[{i}].spot", Vector2.Zero);
            }
        }

        public void Add(GameObject go)
        {
            objects.Add(go);            
        }

        public List<T> FindObjectsOfType<T>() where T: Component
        {
            List<T> ret = new List<T>();

            foreach (GameObject go in objects)
            {
                ret.AddRange(go.GetComponents<T>());
            }

            return ret;
        }

        public List<GameObject> GetAllObjects() => objects;
    }
}
