extends Node2D

var background = preload("res://Scenes/Background.tscn")
var desired_pos = -145;
var last_stage = 270


# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.

func _physics_process(delta):
	if position.y < desired_pos:
		position.y += 5

func level_up():
	# Prevents this from happening multiple times during rollbacks
	if position.y >= desired_pos:
		var new_bkg = background.instance()
		add_child(new_bkg)
		new_bkg.position.x = 0
		new_bkg.position.y = last_stage - 270
		last_stage -= 270
		desired_pos += 270
		
func rollback():
	desired_pos -= 270
	position.y = desired_pos
	print("Rolling back level up")
