using Godot;

namespace Assets.Scripts.User
{
    public partial class Cursor : Spatial
    {
        public GeometryInstance Mesh { get; set; }

        public Color Color {
            get => ((SpatialMaterial)Mesh.MaterialOverride).AlbedoColor;
            set => ((SpatialMaterial)Mesh.MaterialOverride).AlbedoColor = value;
        }

        public void SetCursorTransform(Vector3 hitPoint, Vector3 hitPointNormal)
        {
            var basis = Basis.Identity;
            basis.y = hitPointNormal;
            basis.x = basis.z.Cross(basis.y);
            basis = basis.Orthonormalized();

            GlobalTransform = new Transform(basis, hitPoint);
        }

        public override void _Ready()
        {
            Mesh = GetNode<GeometryInstance>("Mesh");
        }
    }
}