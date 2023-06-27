using Assets.Scripts.User;
using Godot;

namespace Assets.Scripts.Control
{
    public partial class HandController : BaseController
    {
        public enum Hands
        {
            Left = 0,
            Right = 1,
        }

        private bool isPinching;
        private bool pinchChanged;
        private Hands hands;
        private string handString;
        private Camera3D mainCamera;

        public HandController(Hands hands)
        {
            this.hands = hands;
            handString = hands == Hands.Left ? "left" : "right";
        }

        public bool IsPinching
        {
            get => isPinching;
            set
            {
                PinchChanged = isPinching != value;
                isPinching = value;
            }
        }

        public bool PinchChanged
        {
            get
            {
                var ret = pinchChanged;
                if (pinchChanged) pinchChanged = false;
                return ret;
            }
            private set
            {
                pinchChanged = value;
            }
        }

        public Node3D ThumbTip { get; private set; }

        public Node3D IndexTip { get; private set; }

        private Node3D CreateHandNode(string scenePath)
        {
            var handScene = ResourceLoader.Load<PackedScene>(scenePath);
            var hand = handScene.Instantiate<Node3D>();
            return hand;
        }


        private void AddProximityLights(Node3D parent)
        {
            var proximityLight = new OmniLight3D()
            {
                OmniRange = 0.0339852f,
                OmniAttenuation = 1.46409f,
                LightEnergy = 0.66f,
                LightIndirectEnergy = 0,
                ShadowEnabled = true,
                LightCullMask = 4,
            };
            parent.AddChild(proximityLight);
            var proximityVisibleLight = new OmniLight3D()
            {
                OmniRange = 0.0923046f,
                LightEnergy = 0.04f,
                LightCullMask = 2,
            };
            parent.AddChild(proximityVisibleLight);
        }

        public override void _Ready()
        {
            var player = GetParent<Player>();
            mainCamera = player.FindChild("MainCamera") as Camera3D;

            ThumbTip = player.FindChild($"socket-{handString}-thumb") as Node3D;
            IndexTip = player.FindChild($"socket-{handString}-index") as Node3D;
            AddInputSource(IndexTip, player, player.CursorScenePath, player.RayScenePath);

            AddProximityLights(IndexTip);

            /*
            ┖╴SceneRoot
                ┠╴Player(or MainCamera)
                ┃  ┠╴HandNode
                ┃  ┃ ┖╴BoneAttachment(Index_Tip_R)
                ┃  ┃   ┖╴RemoteTransform3D -> socket-right
                ┃  ┠╴XRHand
                ┃  ┠╴HandController(left)
                ┃  ┠╴HandController
                ┠╴socket-left-index
                ┃ ┖╴InputSource
                ┃   ┖╴Cursor
                ┠╴socket-left-thumb
                ┠╴socket-right-index
                ┃ ┖╴InputSource
                ┃   ┖╴Cursor
                ┠╴socket-right-thumb
                ┖╴Ray
            */
        }

        public override void _Process(double delta)
        {
            if ((ThumbTip.GlobalTransform.Origin.DistanceTo(IndexTip.GlobalTransform.Origin) < 0.03f) ^ isPinching)
            {
                IsPinching = !isPinching;
                InputSource.IsPinching = IsPinching;
            }

            InputSource.RayCastDirection = -InputSource.GlobalTransform.Basis.Z.Normalized();
            InputSource.RayCastBegin = InputSource.GlobalTransform.Origin - InputSource.RayCastDirection * 0.05f;
            InputSource.RayCastDistance = 1.55f;
        }
    }
}