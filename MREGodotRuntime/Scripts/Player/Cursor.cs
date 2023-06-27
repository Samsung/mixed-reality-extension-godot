using Godot;

namespace Assets.Scripts.User
{
    public partial class Cursor : Node3D
    {
        public GeometryInstance3D Mesh { get; set; }

        public Color Color {
            get => ((StandardMaterial3D)Mesh.MaterialOverride).AlbedoColor;
            set => ((StandardMaterial3D)Mesh.MaterialOverride).AlbedoColor = value;
        }

        public void SetCursorTransform(Vector3 hitPoint, Vector3 hitPointNormal)
        {
            var basis = Basis.Identity;
            basis.Y = hitPointNormal;
            basis.X = basis.Z.Cross(basis.Y);
            basis = basis.Orthonormalized();

            GlobalTransform = new Transform3D(basis, hitPoint);
        }

        public override void _Ready()
        {
            Mesh = GetNode<GeometryInstance3D>("Mesh");
        }
    }
}