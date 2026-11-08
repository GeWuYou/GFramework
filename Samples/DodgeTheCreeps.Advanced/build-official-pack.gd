# Copyright (c) 2025-2026 GeWuYou
# SPDX-License-Identifier: Apache-2.0

extends SceneTree
## Rebuilds the small official resource fixture and pins its digest in compiled host code.
## Run with the sample's supported Godot version; no downloaded or third-party PCK is trusted.

func _initialize() -> void:
	var output := "res://content_sources/moon/moon.pck"
	var packer := PCKPacker.new()
	var error := packer.pck_start(output)
	if error == OK:
		error = packer.add_file("res://content/moon/arena.tscn", "res://official-pack/arena.tscn")
	if error == OK:
		error = packer.flush()
	if error != OK:
		printerr("OFFICIAL_PACK_BUILD FAIL ", error)
		quit(1)
		return
	var digest := FileAccess.get_sha256(output)
	var source := FileAccess.open("res://OfficialPackTrust.cs", FileAccess.WRITE)
	if source == null:
		printerr("OFFICIAL_PACK_BUILD FAIL cannot write host hash")
		quit(1)
		return
	source.store_string("// Copyright (c) 2025-2026 GeWuYou\n// SPDX-License-Identifier: Apache-2.0\n\nnamespace DodgeTheCreeps.Advanced;\n\n/// <summary>Host-compiled trust for the reproducible official PCK fixture.</summary>\ninternal static class OfficialPackTrust\n{\n    /// <summary>SHA256 pinned by the official pack build; never read from a mod directory.</summary>\n    internal const string MoonSha256 = \"" + digest + "\";\n}\n")
	source.close()
	print("OFFICIAL_PACK_BUILD PASS ", digest)
	quit(0)
