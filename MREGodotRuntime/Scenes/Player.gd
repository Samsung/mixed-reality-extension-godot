extends ARVROrigin

export (NodePath) var viewport = null

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
	if (Input.is_key_pressed(KEY_SPACE)):
		ARVRServer.center_on_hmd(true, true)

	if (Input.is_key_pressed(KEY_LEFT)):
		rotation.y += delta
	elif (Input.is_key_pressed(KEY_RIGHT)):
		rotation.y -= delta

	if (Input.is_key_pressed(KEY_UP)):
		translation -= transform.basis.z * delta;
	elif (Input.is_key_pressed(KEY_DOWN)):
		translation += transform.basis.z * delta;
	
