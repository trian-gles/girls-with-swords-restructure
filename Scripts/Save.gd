extends Node

export (NodePath) var dropdown_path
onready var dropdown = get_node(dropdown_path)

onready var FriendName = $FriendName
onready var OpponentPort =$OpponentPort
onready var OpponentIp = $OpponentIp
onready var LocalPort = $LocalPort

const SAVE_DIR = "user://friends/"
var save_path = "user://friends/friendlist.txt"

func _ready():
	dropdown.connect("item_selected", self, "on_item_selected")
	loadfriendlist()
			
	pass
	
func loadfriendlist():
	dropdown.clear()
	var file = File.new()
	if file.file_exists(save_path):
		var error = file.open(save_path, File.READ)
		if error == OK:
			var player_data = parse_json(file.get_line())
			player_data = player_data.keys()
			dropdown.add_item(player_data[0])

	else:
		var defaultfriend = {"Your friends" : "will appear here" }
		file.open(save_path, File.WRITE)
		file.store_line(to_json(defaultfriend))
	file.close()
	
func _on_AddFriend_button_down():
	
	var friendtext = FriendName.text
	
	var addedfriend = {
		"Friend Name" : FriendName.text,
		"Opponent Port" : OpponentPort.text,
		"Opponent Ip" : OpponentIp.text,
		"Local Port" : LocalPort.text,
	}
		
	var dir = Directory.new()
	if !dir.dir_exists(SAVE_DIR):
		dir.make_dir_recursive(SAVE_DIR)

	var file = File.new()
	file.open(save_path, File.READ)
	var friendlist = parse_json(file.get_line())
	friendlist[friendtext] = addedfriend
	
	var error = file.open(save_path, File.WRITE)
	if error == OK:
		file.store_line(to_json(friendlist))
		file.close()
#
#	console_write("Friend", "added","have","fun!")
	
	loadfriendlist()

func _on_LoadFriend_button_down(id):
	var file = File.new()

	if file.file_exists(save_path):
		var error = file.open(save_path, File.READ)
		if error == OK:
			var player_data = parse_json(file.get_line())
			print("This is player_data printing ", str(player_data))
			print("This is the passed argument ", str(id))
			var loadedfriend = player_data[id]
			file.close()
			console_write(loadedfriend["Friend Name"],loadedfriend["Opponent Port"],loadedfriend["Opponent Ip"],loadedfriend["Local Port"])

func console_write(friendname,port,ip,homeport):
	FriendName.clear()
	OpponentPort.clear()
	OpponentIp.clear()
	LocalPort.clear()
	FriendName.text += str(friendname)
	OpponentPort.text += str(port) 
	OpponentIp.text += str(ip)
	LocalPort.text += str(homeport)
	
func on_item_selected(id):
#	print(str(dropdown.get_item_text(id)))
	var selectedfriend = (dropdown.get_item_text(id))
#	print(str(selectedfriend))
	_on_LoadFriend_button_down(selectedfriend)
