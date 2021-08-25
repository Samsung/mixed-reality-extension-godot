extends ARVROrigin

export (NodePath) var viewport = null
var mouse_delta = Vector2()
var camera_move = false
var camera_speed = 0.003

func initialise() -> bool:
	var interface = ARVRServer.find_interface("OpenXR")
	if interface and interface.initialize():
		print("OpenXR Interface initialized")

		var vp : Viewport = null
		if viewport:
			vp = get_node(viewport)

		if !vp:
			vp = get_viewport()

		vp.arvr = true
		vp.keep_3d_linear = $Configuration.keep_3d_linear()

		Engine.iterations_per_second = 144

		return true
	else:
		return false

func _ready():
	initialise()

func _process(delta):
	if !camera_move: return;
	if (Input.is_key_pressed(KEY_SPACE)):
		ARVRServer.center_on_hmd(true, true)

	if (Input.is_action_pressed("move_forward")):
		translation -= transform.basis.z * delta
	elif (Input.is_action_pressed("move_back")):
		translation += transform.basis.z * delta
	if (Input.is_action_pressed("move_right")):
		translation += transform.basis.x * delta
	elif (Input.is_action_pressed("move_left")):
		translation -= transform.basis.x * delta;

	rotation.x -= mouse_delta.y * camera_speed
	rotation.y -= mouse_delta.x * camera_speed

	mouse_delta = Vector2();

func _input(inputEvent):
	if inputEvent is InputEventMouseMotion:
		mouse_delta = inputEvent.get_relative();
	elif inputEvent is InputEventMouseButton:
		if !camera_move && inputEvent.pressed:
			camera_move = true;
	elif (Input.is_action_pressed("ui_cancel")):
		camera_move = false;

