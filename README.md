# Mixed Reality Extension SDK Godot

The Mixed Reality Extension SDK Godot library makes it easy for any Godot app or game to support user generated content (UGC) built with the [Mixed Reality Extension SDK](
https://github.com/Microsoft/mixed-reality-extension-sdk). It also makes it easy to test an MRE within a simple environment inside the Godot editor.

This project was forked from [Mixed Reality Extension Unity](https://github.com/Microsoft/mixed-reality-extension-unity).


## Prerequisites
* Install Git
* Use [GodotEngine](https://godotengine.org/) to Install Godot 3.


## How to build and run Hello World in the Godot editor
From command prompt:
* `git clone http://github.com/dasg34/mixed-reality-extension-godot`
* Open Godot editor with `project.godot`
* Open the scene `MREGodotRuntime/Scenes/HelloWorld.tscn`
* Play Scene(F6)

You should now see a slowly spinning Hello World label.


## Scene Descriptions
The MRETestbed project contains 4 Godot scenes set up for different testing purposes
* `HelloWorld.tscn`: Connects to a single MRE in the cloud on startup, no interaction needed.
* `Standalone.tscn`: Connects to a localhost.
* `TriggerVolumeTestBed-localhost.tscn`: Connects to a localhost MRE when you walk close - useful for testing user join/leave.
* `SynchronizationTest-localhost.tscn`: Connects twice to twice to a localhost MREs with the same session ID. When you click on the two spheres you will see 2 different connections to the same server, so you can perform basic multiuser testing without multiple machines or multiple unity instances.

The Localhost samples requires a local node server running, see the [Sample repository](
https://github.com/Microsoft/mixed-reality-extension-sdk-samples#How-to-Build-and-Run-the-Hello-World-sample) for localhost deployment.


## Overview
* For more information, please see 
the [Mixed Reality Extension SDK](
https://github.com/Microsoft/mixed-reality-extension-sdk) repository's [README.md](https://github.com/Microsoft/mixed-reality-extension-sdk/blob/master/README.md) is the best source of information about features, current state, limitations, goal, major known issues, and roadmap.

## Getting In Touch
To report issues and feature requests: [Github issues page](
https://github.com/dasg34/mixed-reality-extension-godot/issues).


## License
Code licensed under the [MIT License](https://github.com/dasg34/mixed-reality-extension-godot/blob/main/LICENSE).

