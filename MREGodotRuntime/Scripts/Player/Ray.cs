using Godot;

namespace Assets.Scripts.User
{
    public class partial Ray : ImmediateGeometry
    {
        internal Camera3D Camera3D { get; set; }

        public Color Color {
            get {
                var gradientTexture = ((StandardMaterial3D)MaterialOverride).AlbedoTexture as GradientTexture;
                var gradient = gradientTexture.Gradient;
                return gradient.GetColor(1);
            }
            set {
                var gradientTexture = ((StandardMaterial3D)MaterialOverride).AlbedoTexture as GradientTexture;
                var gradient = gradientTexture.Gradient;
                gradient.SetColor(0, new Color(1, 1, 1, 0));
                gradient.SetColor(1, value);
                gradient.SetColor(2, value);
                gradient.SetColor(3, new Color(1, 1, 1, 0));
                gradientTexture.Gradient = gradient;
            }
        }

        public void DrawRay(Vector3 rayBegin, Vector3 rayEnd)
        {
            var width = 1.6f;
            var startDepth = ToLocal(rayBegin).Project(Camera3D.ProjectLocalRayNormal(OS.WindowSize / 2)).Length();
            var endDepth = ToLocal(rayEnd).Project(Camera3D.ProjectLocalRayNormal(OS.WindowSize / 2)).Length();
            var startPoint = Camera3D.UnprojectPosition(rayBegin);
            var endPoint = Camera3D.UnprojectPosition(rayEnd);
            var normal = endPoint - startPoint;
            normal = new Vector2(-normal.y, normal.x).Normalized();
            // p# variable is a point in the 2D coordinate.
            // v# variable is a vector in the 3D coordinate.
            /*  p4(v4)    p3(v3)
                    -----
                    |   |
                    |   |
                    |   |
                    -----
                p1(v1)    p2(v2)
            */
            var p1 = startPoint + normal * width;
            var p2 = startPoint - normal * width;
            var p3 = endPoint - normal * width;
            var p4 = endPoint + normal * width;

            var v1 = Camera3D.ProjectPosition(p1, startDepth);
            var v2 = Camera3D.ProjectPosition(p2, startDepth);
            var v3 = Camera3D.ProjectPosition(p3, endDepth);
            var v4 = Camera3D.ProjectPosition(p4, endDepth);

            Clear();
            Begin(Mesh.PrimitiveType.TriangleStrip);

            SetUv(new Vector2(0, 0));
            AddVertex(v1);
            SetUv(new Vector2(0, 1));
            AddVertex(v2);
            SetUv(new Vector2(1, 0));
            AddVertex(v4);
            SetUv(new Vector2(1, 1));
            AddVertex(v3);
            End();
        }
    }
}
