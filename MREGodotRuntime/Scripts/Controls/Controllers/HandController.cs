using Assets.Scripts.User;
using Godot;

namespace Assets.Scripts.Control
{
    public class HandController : BaseController
    {
        private bool isPinching;
        private bool pinchChanged;
        private Spatial handNode;
        private Camera mainCamera;

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

        public Spatial ThumbTip { get; private set; }

        public Spatial IndexTip { get; private set; }

        private Spatial CreateHandNode(string scenePath)
        {
            var handScene = ResourceLoader.Load<PackedScene>(scenePath);
            var hand = handScene.Instance<Spatial>();
            return hand;
        }

        private void AddHandNodes(Player player)
        {
            var isOpenXRInitialized = ARVRServer.FindInterface("OpenXR")?.InterfaceIsInitialized;
            if (!isOpenXRInitialized.HasValue)
                return;

            if (isOpenXRInitialized.Value)
            {
                player.AddChild(handNode);
            }
            else
            {
                handNode.Translation = new Vector3(0.081f, -0.006f, -0.151f);
                mainCamera.AddChild(handNode);
            }
        }

        private void AddProximityLights(Spatial parent)
        {
            var proximityLight = new OmniLight()
            {
                OmniRange = 0.0339852f,
                OmniAttenuation = 1.46409f,
                LightEnergy = 0.66f,
                LightIndirectEnergy = 0,
                ShadowEnabled = true,
                LightCullMask = 4,
            };
            parent.AddChild(proximityLight);
            var proximityVisibleLight = new OmniLight()
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
            mainCamera = player.FindNode("MainCamera") as Camera;

            AddHandNodes(player);
            ThumbTip = handNode?.FindNode("ThumbTip") as Spatial;
            IndexTip = handNode?.FindNode("IndexTip") as Spatial;
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