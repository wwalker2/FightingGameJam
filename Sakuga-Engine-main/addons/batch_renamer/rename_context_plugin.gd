@tool
extends EditorContextMenuPlugin

# 存储选中的文件夹路径
var current_path: String = ""

# 用于记录重命名映射，以便后续修复场景文件
var rename_mappings: Dictionary = {}

# UI 组件
var dialog: ConfirmationDialog
var include_subdirs_check: CheckBox
var dry_run_check: CheckBox

func _init() -> void:
	# 纯代码构建弹窗 UI
	dialog = ConfirmationDialog.new()
	dialog.title = "批量重命名为 snake_case"
	dialog.size = Vector2(400, 200)

	var vbox: VBoxContainer = VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 10)

	# 说明标签
	var desc_label: Label = Label.new()
	desc_label.text = "将选中的文件夹及其子文件夹中的所有文件和文件夹\n名称转换为 snake_case 格式。\n同时也会修复 .tscn 和 .tres 文件内部的路径引用。"
	desc_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(desc_label)

	# 包含子文件夹选项
	include_subdirs_check = CheckBox.new()
	include_subdirs_check.text = "包含子文件夹"
	include_subdirs_check.button_pressed = true
	vbox.add_child(include_subdirs_check)

	# 预览模式（不实际执行重命名）
	dry_run_check = CheckBox.new()
	dry_run_check.text = "预览模式（不实际重命名）"
	dry_run_check.button_pressed = false
	vbox.add_child(dry_run_check)

	# 警告标签
	var warning_label: Label = Label.new()
	warning_label.text = "⚠️ 建议先提交 Git 再执行此操作"
	warning_label.modulate = Color(1.0, 0.8, 0.2)
	warning_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(warning_label)

	dialog.add_child(vbox)
	dialog.confirmed.connect(_on_dialog_confirmed)

	# 将弹窗添加到编辑器主界面节点下
	EditorInterface.get_base_control().add_child(dialog)


# 当你在 FileSystem 中右键时触发
func _popup_menu(paths: PackedStringArray) -> void:
	# 只在选中单个文件夹时显示该选项
	if paths.size() == 1 and DirAccess.dir_exists_absolute(paths[0]):
		current_path = paths[0]
		# 添加右键菜单项
		add_context_menu_item("批量重命名为 snake_case...", _show_dialog)


func _show_dialog(_paths: PackedStringArray) -> void:
	# 清空上次的重命名映射
	rename_mappings.clear()
	# 弹出窗口
	dialog.popup_centered()


func _on_dialog_confirmed() -> void:
	var include_subdirs: bool = include_subdirs_check.button_pressed
	var dry_run: bool = dry_run_check.button_pressed

	rename_mappings.clear()

	# 第一步：收集所有需要重命名的文件
	print("【批量重命名】开始扫描文件夹: %s" % current_path)
	print("【批量重命名】模式: %s" % ("预览" if dry_run else "实际执行"))

	if include_subdirs:
		print("【批量重命名】包含子文件夹: 是")
	else:
		print("【批量重命名】包含子文件夹: 否")

	_collect_renames(current_path, include_subdirs)

	if rename_mappings.is_empty():
		print("【批量重命名】没有发现需要重命名的文件或文件夹。")
		return

	print("【批量重命名】发现 %d 个需要重命名的项目" % rename_mappings.size())

	# 打印预览
	if dry_run:
		print("\n===== 预览模式 =====")
		for old_name in rename_mappings.keys():
			print("%s -> %s" % [old_name, rename_mappings[old_name]])
		print("===== 预览结束 =====\n")
		return

	# 第二步：执行重命名
	print("\n===== 开始重命名 =====")
	_execute_renames()
	print("===== 重命名完成 =====\n")

	# 第三步：修复场景文件中的路径引用
	print("\n===== 开始修复场景文件引用 =====")
	_fix_scene_file_references(current_path, include_subdirs)
	print("===== 场景文件引用修复完成 =====\n")

	# 刷新 Godot 文件系统
	EditorInterface.get_resource_filesystem().scan()

	print("【批量重命名】所有操作已完成！")


# 将字符串转换为 snake_case
func _to_snake_case(text: String) -> String:
	var result: String = ""
	var prev_was_upper: bool = false
	var prev_was_alnum: bool = false

	for i in range(text.length()):
		var char: String = text[i]

		# 跳过已存在的下划线
		if char == "_":
			# 避免连续下划线
			if result.length() > 0 and result[result.length() - 1] != "_":
				result += "_"
			prev_was_upper = false
			prev_was_alnum = false
			continue

		# 处理空格和连字符
		if char == " " or char == "-":
			if result.length() > 0 and result[result.length() - 1] != "_":
				result += "_"
			prev_was_upper = false
			prev_was_alnum = false
			continue

		# 检查是否为字母数字
		var is_upper: bool = char.to_upper() == char and char.to_lower() != char
		var is_lower: bool = char.to_lower() == char and char.to_upper() != char
		var is_digit = char.is_valid_int()

		# 大写字母处理
		if is_upper:
			if prev_was_alnum and not prev_was_upper:
				# 在小写字母后的大写字母前加下划线
				result += "_"
			result += char.to_lower()
			prev_was_upper = true
			prev_was_alnum = true
		# 小写字母处理
		elif is_lower:
			if prev_was_upper:
				# 在连续大写字母后的小写字母前加下划线（除了第一个大写字母）
				if result.length() >= 2 and result[result.length() - 2] != "_":
					# 检查是否前一个是大写
					var prev_char: String = text[i - 1]
					if prev_char.to_upper() == prev_char:
						result = result.substr(0, result.length() - 1) + "_" + result[result.length() - 1]
			result += char
			prev_was_upper = false
			prev_was_alnum = true
		# 数字处理
		elif is_digit:
			if prev_was_alnum and (prev_was_upper or result[result.length() - 1].is_valid_int() == false):
				# 在字母后的数字前加下划线
				if result.length() > 0 and result[result.length() - 1] != "_":
					result += "_"
			result += char
			prev_was_upper = false
			prev_was_alnum = true

	# 清理首尾下划线
	while result.begins_with("_"):
		result = result.substr(1)
	while result.ends_with("_"):
		result = result.substr(0, result.length() - 1)

	return result


# 获取不带扩展名的文件名
func _get_filename_without_extension(filename: String) -> String:
	var dot_index: int = filename.rfind(".")
	if dot_index > 0:
		return filename.substr(0, dot_index)
	return filename


# 获取扩展名（包括点）
func _get_extension(filename: String) -> String:
	var dot_index: int = filename.rfind(".")
	if dot_index > 0:
		return filename.substr(dot_index)
	return ""


# 收集需要重命名的文件
func _collect_renames(target_path: String, include_subdirs: bool) -> void:
	var dir: DirAccess = DirAccess.open(target_path)
	if not dir:
		push_error("无法打开目录: %s" % target_path)
		return

	dir.list_dir_begin()
	var file_name: String = dir.get_next()

	while file_name != "":
		if file_name == "." or file_name == "..":
			file_name = dir.get_next()
			continue

		var full_path: String = target_path + "/" + file_name

		# 计算新文件名
		var new_name: String = ""

		if dir.current_is_dir():
			# 递归处理子文件夹
			if include_subdirs:
				_collect_renames(full_path, include_subdirs)

			# 文件夹重命名（不处理 .godot 等特殊文件夹）
			if file_name.begins_with(".") or file_name == "addons":
				file_name = dir.get_next()
				continue

			new_name = _to_snake_case(file_name)
		else:
			# 文件重命名
			if file_name.ends_with(".import"):
				file_name = dir.get_next()
				continue

			# 分离文件名和扩展名
			var base_name: String = _get_filename_without_extension(file_name)
			var extension: String = _get_extension(file_name)
			var new_base: String = _to_snake_case(base_name)
			new_name = new_base + extension

		# 如果名称有变化，记录映射
		if new_name != file_name and new_name != "":
			var old_full_path: String = full_path
			var new_full_path: String = target_path + "/" + new_name
			rename_mappings[old_full_path] = new_full_path
			print("将要重命名: %s -> %s" % [old_full_path, new_full_path])

		file_name = dir.get_next()

	dir.list_dir_end()


# 执行重命名操作
func _execute_renames() -> void:
	# 按照路径深度从深到浅排序，确保先重命名深层文件
	var paths: Array = rename_mappings.keys()
	paths.sort_custom(func(a, b): return a.count("/") > b.count("/"))

	for old_path in paths:
		var new_path = rename_mappings[old_path]

		# 检查是否是文件夹
		var is_dir: bool = DirAccess.dir_exists_absolute(old_path)

		var dir: DirAccess = DirAccess.open(old_path.get_base_dir())
		if not dir:
			push_error("无法打开目录: %s" % old_path.get_base_dir())
			continue

		var old_name = old_path.get_file()
		var new_name = new_path.get_file()

		# 执行重命名
		var err: int = dir.rename(old_name, new_name)
		if err != OK:
			push_error("重命名失败: %s -> %s (错误码: %d)" % [old_name, new_name, err])
		else:
			print("✓ 已重命名: %s" % old_name)

		# 如果是文件，同时重命名对应的 .import 文件
		if not is_dir:
			if FileAccess.file_exists(old_path + ".import"):
				var import_rename_err: int = dir.rename(old_name + ".import", new_name + ".import")
				if import_rename_err != OK:
					push_error("重命名 .import 文件失败: %s.import" % old_name)


# 修复场景文件 (.tscn) 和资源文件 (.tres) 中的路径引用
func _fix_scene_file_references(target_path: String, include_subdirs: bool) -> void:
	var dir: DirAccess = DirAccess.open(target_path)
	if not dir:
		return

	dir.list_dir_begin()
	var file_name: String = dir.get_next()

	while file_name != "":
		if file_name == "." or file_name == "..":
			file_name = dir.get_next()
			continue

		var full_path: String = target_path + "/" + file_name

		if dir.current_is_dir():
			if include_subdirs:
				_fix_scene_file_references(full_path, include_subdirs)
		else:
			# 只处理 .tscn 和 .tres 文件
			if file_name.ends_with(".tscn") or file_name.ends_with(".tres"):
				_fix_single_scene_file(full_path)

		file_name = dir.get_next()

	dir.list_dir_end()


# 修复单个场景文件中的路径引用
func _fix_single_scene_file(file_path: String) -> void:
	var file: FileAccess = FileAccess.open(file_path, FileAccess.READ)
	if not file:
		push_error("无法打开文件: %s" % file_path)
		return

	var content: String = file.get_as_text()
	file.close()

	var original_content: String = content
	var has_changes: bool = false

	# 遍历所有重命名映射，替换文件中的旧路径
	for old_path in rename_mappings.keys():
		var new_path = rename_mappings[old_path]

		# 获取相对路径（从 res:// 开始）
		var old_relative = old_path.trim_prefix("res://")
		var new_relative = new_path.trim_prefix("res://")

		# 提取文件名（用于处理只包含文件名的情况）
		var old_filename = old_path.get_file()
		var new_filename = new_path.get_file()

		# 处理 .import 文件的引用
		if old_relative.ends_with(".import"):
			var old_import_base = old_relative.trim_suffix(".import")
			var new_import_base = new_relative.trim_suffix(".import")
			if content.contains(old_import_base):
				content = content.replace(old_import_base, new_import_base)
				has_changes = true
			continue

		# 替换完整路径
		if content.contains(old_relative):
			content = content.replace(old_relative, new_relative)
			has_changes = true

		# 替换只有文件名的情况（在一些 ext_resource 中使用）
		# 但需要注意避免错误替换（例如替换到不应替换的地方）
		# 这里我们使用更保守的策略，只替换路径中的文件名部分
		var old_base = old_filename.get_basename()
		var new_base = new_filename.get_basename()

		# 如果文件名基础部分发生变化，尝试在路径上下文中替换
		if old_base != new_base:
			# 使用正则表达式风格的替换，只替换作为路径一部分的文件名
			# 例如: path="res://folder/OldName.png" -> path="res://folder/new_name.png"
			var patterns: Array[Variant] = [
				"path=\"%s/" % file_path.get_base_dir().trim_prefix("res://"),
				"uid://",
			]

			# 查找包含旧文件名的引用
			var search_pattern: String = '/%s' % old_filename
			var replace_pattern: String = '/%s' % new_filename

			if content.contains(search_pattern):
				content = content.replace(search_pattern, replace_pattern)
				has_changes = true

	# 如果有变化，写回文件
	if has_changes and content != original_content:
		var write_file: FileAccess = FileAccess.open(file_path, FileAccess.WRITE)
		if write_file:
			write_file.store_string(content)
			write_file.close()
			print("✓ 已修复引用: %s" % file_path)
		else:
			push_error("无法写入文件: %s" % file_path)
