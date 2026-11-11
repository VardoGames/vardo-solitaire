extends SceneTree
func _init():
	var args = OS.get_cmdline_user_args()
	var src_dir = args[0]; var out_dir = args[1]; var scale = float(args[2])
	for f in DirAccess.get_files_at(src_dir):
		if not f.ends_with(".svg"): continue
		var txt = FileAccess.get_file_as_string(src_dir.path_join(f))
		var img = Image.new()
		var err = img.load_svg_from_string(txt, scale)
		if err != OK:
			print("fail ", f, " ", err); continue
		img.save_png(out_dir.path_join(f.get_basename() + ".png"))
		print(f, " ", img.get_size())
	quit()
