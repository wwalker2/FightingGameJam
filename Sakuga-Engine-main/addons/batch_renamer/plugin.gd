@tool
extends EditorPlugin

var rename_context: EditorContextMenuPlugin

func _enter_tree() -> void:
	# 实例化我们的右键菜单插件
	rename_context = preload("res://addons/batch_renamer/rename_context_plugin.gd").new()
	
	# 注册到 FileSystem 的右键菜单槽位
	add_context_menu_plugin(EditorContextMenuPlugin.CONTEXT_SLOT_FILESYSTEM, rename_context)

func _exit_tree() -> void:
	# 禁用插件时清理内存和菜单
	if rename_context:
		remove_context_menu_plugin(rename_context)
		# 清理挂载在编辑器上的弹窗 UI，防止内存泄漏
		if rename_context.dialog:
			rename_context.dialog.queue_free()
