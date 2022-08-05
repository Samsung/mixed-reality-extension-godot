using Assets.Scripts.User;
using Godot;

namespace Assets.Scripts.Control
{
    public partial class HandController : BaseController
    {
        private bool isPinching;
        private bool pinchChanged;
        private Node3D handNode;
        private Camera3D mainCamera;

        public HandController(string scenePath)
        {
            handNode = CreateHandNode(scenePath);
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

        private void AddHandNodes(Player player)
        {
            var isOpenXRInitialized = XRServer.FindInterface("OpenXR")?.IsInitialized();
            if (!isOpenXRInitialized.HasValue)
                return;

            if (isOpenXRInitialized.Value)
            {
                player.AddChild(handNode);
            }
            else
            {
                handNode.Position = new Vector3(0.081f, -0.006f, -0.151f);
                mainCamera.AddChild(handNode);
            }
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
                LightEnergy = 1.46f,
                LightCullMask = 2,
            };
            parent.AddChild(proximityVisibleLight);
        }

        public override void _Ready()
        {
            var player = GetParent<Player>();
            mainCamera = player.FindChild("MainCamera") as Camera3D;

            AddHandNodes(player);
            ThumbTip = handNode?.FindChild("ThumbTip") as Node3D;
            IndexTip = handNode?.FindChild("IndexTip") as Node3D;
            AddInputSource(IndexTip, mainCamera, player.CursorScenePath, player.RayScenePath);

            AddProximityLights(IndexTip);

            /*
              Reparent HandController node. Node tree will be:
              ┖╴SceneRoot
                 ┠╴Player(or MainCamera)
                 ┃  ┖╴HandNode
                 ┃     ┠╴HandController
                 ┃     ┖╴InputSource
                 ┃        ┖╴Cursor
                 ┖╴Ray
            */
            player.RemoveChild(this);
            handNode.AddChild(this);
        }

        public override void _Process(float delta)
        {
            if ((ThumbTip.GlobalTransform.origin.DistanceTo(IndexTip.GlobalTransform.origin) < 0.03f) ^ isPinching)
            {
                IsPinching = !isPinching;
                InputSource.IsPinching = IsPinching;
            }

            InputSource.RayCastDirection = -InputSource.GlobalTransform.basis.z.Normalized();
            InputSource.RayCastBegin = InputSource.GlobalTransform.origin - InputSource.RayCastDirection * 0.05f;
            InputSource.RayCastDistance = 1.55f;
        }
    }
}