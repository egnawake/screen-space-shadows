using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace OpenTKBase
{
    public class FirstPersonController : Component
    {
        public float moveSpeed = 1f;
        public float rotateSpeed = MathF.PI / 2.0f;
        public float boostFactor = 3.0f;

        private Vector2 rotation = Vector2.Zero;

        public override void Update()
        {
            Vector3 moveDir = Vector3.Zero;
            float boost = 1.0f;

            if (OpenTKApp.APP.GetKey(Keys.LeftShift))
            {
                boost = boostFactor;
            }

            if (OpenTKApp.APP.GetKey(Keys.W)) moveDir.Z = 1.0f;
            if (OpenTKApp.APP.GetKey(Keys.S)) moveDir.Z = -1.0f;
            if (OpenTKApp.APP.GetKey(Keys.A)) moveDir.X = -1.0f;
            if (OpenTKApp.APP.GetKey(Keys.D)) moveDir.X = 1.0f;
            if (OpenTKApp.APP.GetKey(Keys.Q)) moveDir.Y = -1.0f;
            if (OpenTKApp.APP.GetKey(Keys.E)) moveDir.Y = 1.0f;

            var tf = transform.forward; tf.Y = 0.0f; tf.Normalize();
            var tr = Vector3.Cross(tf, Vector3.UnitY); tr.Y = 0.0f; tr.Normalize();

            moveDir = moveDir.X * tr + moveDir.Z * tf + moveDir.Y * Vector3.UnitY;
            moveDir *= moveSpeed * boost * OpenTKApp.APP.timeDeltaTime;

            transform.position += moveDir;

            var mouseDelta = OpenTKApp.APP.GetMouseDelta();

            mouseDelta.X = MathF.Sign(mouseDelta.X);
            mouseDelta.Y = MathF.Sign(mouseDelta.Y);

            float   angleY = -rotateSpeed * mouseDelta.X * OpenTKApp.APP.timeDeltaTime;
            float   angleX = -rotateSpeed * mouseDelta.Y * OpenTKApp.APP.timeDeltaTime;

            rotation.X += angleX;
            rotation.Y += angleY;
            rotation.X = Math.Clamp(rotation.X, -1.0f, 1.0f);
            var xQuat = Quaternion.FromAxisAngle(Vector3.UnitX, rotation.X);
            var yQuat = Quaternion.FromAxisAngle(Vector3.UnitY, rotation.Y);

            transform.rotation = yQuat * xQuat;
        }
    }
}
