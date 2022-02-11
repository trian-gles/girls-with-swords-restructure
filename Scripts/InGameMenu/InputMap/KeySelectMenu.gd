extends Panel

signal key_selected(scancode)

func _ready():
	set_process_input(false)

func _input(event):
	if not event.is_pressed():
		return
	if event is InputEventJoypadButton:
		emit_signal("key_selected", event.button_index)
	elif event is InputEventKey:
		emit_signal("key_selected", event.scancode)

	close()

func open():
	show()
	set_process_input(true)

func close():
	hide()
	set_process_input(false)
	

