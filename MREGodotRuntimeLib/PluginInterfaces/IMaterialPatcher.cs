// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using Texture = Godot.Texture2D;
using Material = Godot.ShaderMaterial;
using MixedRealityExtension.Patching.Types;

namespace MixedRealityExtension.PluginInterfaces
{
	/// <summary>
	/// Responsible for translating between the host's material properties and the API properties
	/// </summary>
	public interface IMaterialPatcher
	{
		/// <summary>
		/// Apply the patch from the app to the material
		/// </summary>
		/// <param name="app">The app context of the material</param>
		/// <param name="material">An instance of the default MRE material provided on load</param>
		/// <param name="patch">The update from the app. Unmodified properties will be null.</param>
		void ApplyMaterialPatch(IMixedRealityExtensionApp app, Material material, MaterialPatch patch);

		/// <summary>
		/// Generate an API patch from the Godot material's current state
		/// </summary>
		/// <param name="app">The app context of the material</param>
		/// <param name="material">An instance of the default MRE material provided on load</param>
		/// <returns>A full definition of the given material</returns>
		MaterialPatch GeneratePatch(IMixedRealityExtensionApp app, Material material);
	}
}
