using Godot;
using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;

public class MainScene : Node2D
{
	
	public Player P1;
	public Player P2;
	private Label P1Combo;
	private Label P2Combo;
	private TextureProgress P1Health;
	private TextureProgress P2Health;
	private Camera2D camera;
	private GameStateObject gsObj;
	private Label timer;
	private Label centerText;
	private Label statsText;
	private Node GGRS;

	private const int MAXPLAYERS = 2;
	private const int PLAYERNUMBERS = 2;
	private int localPlayerHandle;
	private int localHand = 1;
	private int otherHand = 2;
	private int waitFrames = 0;


	// Can be used to store inputs for synctesting, maybe later for training mode?
	[Export]
	private int[] p2InputLoop;

	[Export]
	public bool halfSpeed = false;

	[Export]
	public int countDownSpeed = 30;
	private int gameFinishFrame;
	public bool displayFrame = true;

	[Signal]
	public delegate void LobbyReturn();

	private int inputs = 0; //Store all inputs on this frame as a single int because that's what GGPO accepts.
	private int p2inputs = 0; //used only in local mode for local p2 inputs

	private bool roundFinished = false; // this really should be in GameStateObject, but because we interface with GGPO, I've put it here.  Allows inputs through
	private bool roundStarted = false;

	private int frameAhead = 0; //prevents one sided rollbacks
	
	/// <summary>
	/// Godot doesn't allow constructors so I have to do stuff like this instead
	/// </summary>
	/// <param name="ip"></param>
	/// <param name="localPort"></param>
	/// <param name="remotePort"></param>
	/// <param name="hosting"></param>
	public void Begin(string ip, int localPort, int remotePort, bool hosting)
	{
		GD.Print("Starting Mainscene Config");
		//Basic config
		camera = GetNode<Camera2D>("Camera2D");

		P1 = GetNode<Player>("P1");
		P2 = GetNode<Player>("P2");
		
		P1.Connect("ComboChanged", this, nameof(OnPlayerComboChange));
		P2.Connect("ComboChanged", this, nameof(OnPlayerComboChange));
		P1.Connect("ComboSet", this, nameof(OnPlayerComboSet));
		P2.Connect("ComboSet", this, nameof(OnPlayerComboSet));
		P1.Connect("HealthChanged", this, nameof(OnPlayerHealthChange));
		P2.Connect("HealthChanged", this, nameof(OnPlayerHealthChange));
		P1.Connect("HadoukenEmitted", this, nameof(OnHadoukenEmitted));
		P2.Connect("HadoukenEmitted", this, nameof(OnHadoukenEmitted));
		P1.Connect("HadoukenRemoved", this, nameof(OnHadoukenRemoved));
		P2.Connect("HadoukenRemoved", this, nameof(OnHadoukenRemoved));
		GGRS = GetNode("GodotGGRS");
		P1Combo = GetNode<Label>("HUD/P1Combo");
		P2Combo = GetNode<Label>("HUD/P2Combo");
		P1Health = GetNode<TextureProgress>("HUD/P1Health");
		P2Health = GetNode<TextureProgress>("HUD/P2Health");
		timer = GetNode<Label>("HUD/Timer");
		centerText = GetNode<Label>("HUD/CenterText");
		statsText = GetNode<Label>("HUD/NetStats");
		centerText.Visible = true;
		
		P1Combo.Text = "";
		P2Combo.Text = "";

		gsObj = new GameStateObject();
		gsObj.config(P1, P2, this, hosting);

		if (Globals.mode == Globals.Mode.GGPO)
		{
			//GGPO Config
			//int errorcode = GGPO.StartSession("ark", PLAYERNUMBERS, localPort);
			//GD.Print($"Starting GGPO session, errorcode {errorcode}");

			GGRS.Call("create_session", localPort, PLAYERNUMBERS);

			localPlayerHandle = (int)GGRS.Call("add_local_player");
			GD.Print($"added local player with handle {localPlayerHandle}");
			//ConnectEvents();
			//Godot.Collections.Dictionary localHandle = GGPO.AddPlayer(GGPO.PlayertypeLocal, localHand, "127.0.0.1", 7000);
			//localPlayerHandle = (int)localHandle["playerHandle"];
			//GD.Print($"Local add result: {localHandle["result"]}");


			var otherPlayerHandle = (int)GGRS.Call("add_remote_player", $"{ip}:{remotePort}");
			GD.Print($"added other player with handle {otherPlayerHandle}");
			GD.Print("Setting callback node");
			GGRS.Call("set_callback_node", this);
			GGRS.Call("set_frame_delay", 2, localPlayerHandle);
			GGRS.Call("start_session");
			
			//int frameDelayError = GGPO.SetFrameDelay(localPlayerHandle, 2);
			//GD.Print($"Frame delay error code: {frameDelayError}");
			//GGPO.CreateInstance(gsObj, nameof(gsObj.SaveGameState));
			//Godot.Collections.Dictionary remoteHandle = GGPO.AddPlayer(GGPO.PlayertypeRemote, otherHand, ip, remotePort);
			//GD.Print($"Remote add result:{remoteHandle["result"]}");

			WaitForConnectionDisplay();
		}

		else if (Globals.mode == Globals.Mode.TRAINING || Globals.mode == Globals.Mode.SYNCTEST )
		{
			roundStarted = true;
			centerText.Visible = false;
		}
		
	}



	/// <summary>
	/// Connect GGPO callbacks
	/// </summary>
	private void ConnectEvents()
	{
		//GGPO.Singleton.Connect("advance_frame", this, nameof(OnAdvanceFrame));
		//GGPO.Singleton.Connect("load_game_state", this, nameof(OnLoadGameState));
		//GGPO.Singleton.Connect("event_disconnected_from_peer", this, nameof(OnEventDisconnectedFromPeer));
		//GGPO.Singleton.Connect("save_game_state", this, nameof(OnSaveGameState));
		//GGPO.Singleton.Connect("event_connected_to_peer", this, nameof(OnEventConnectedToPeer));
		//GGPO.Singleton.Connect("event_timesync", this, nameof(OnEventTimesync));
		//GGPO.Singleton.Connect("event_connection_interrupted", this, nameof(OnEventConnectionInterrupted));
	}

	public override void _Process(float delta)
	{
		if (Globals.mode == Globals.Mode.GGPO)
		{
			GGRS.Call("poll_remote_clients");
		}
	}

	public override void _PhysicsProcess(float _delta)
	{
		if (halfSpeed)
		{
			if (!displayFrame)
			{
				displayFrame = true;
				return;
			}
			else
			{
				displayFrame = false;
			}
		}


		camera.Call("adjust", P1.Position, P2.Position); // Camera is written in GDscript due to my own laziness
		if (Globals.mode == Globals.Mode.GGPO)
		{
			GGPOPhysicsProcess();
		}
		else if (Globals.mode == Globals.Mode.TRAINING) 
		{
			TrainingPhysicsProcess();
		}
		else if (Globals.mode == Globals.Mode.LOCAL)
		{
			LocalPhysicsProcess();
		}
		else if (Globals.mode == Globals.Mode.SYNCTEST)
		{
			SyncTestPhysicsProcess();
		}

		P1.TimeAdvance();
		P2.TimeAdvance();
		
	}

	private void ResetInputs()
	{
		inputs = 0; // reset the inputs
		p2inputs = 0;
	}

	private void SyncTestPhysicsProcess()
	{
		int frame = gsObj.Frame;
		int thisP2Inp = p2InputLoop[frame % p2InputLoop.Length];

		var combinedInputs = new int[2] { inputs, thisP2Inp };
		gsObj.SyncTestUpdate(new Godot.Collections.Array(combinedInputs));
		ResetInputs();
	}

	public void GGPOPhysicsProcess()
	{
		if (!roundFinished && (bool)GGRS.Call("is_running"))
		{
			if (waitFrames > 0)
			{
				waitFrames--;
				return;
			}

			GGRS.Call("advance_frame", localPlayerHandle, inputs);
			var events = (Godot.Collections.Array) GGRS.Call("get_events");
			foreach (var item in events)
			{
				var itemArr = (Godot.Collections.Array)item;
				if ((string)itemArr[0] == "WaitRecommendation")
				{
					waitFrames = (int)itemArr[1];
				}

			}

			var netStats = (Godot.Collections.Array)GGRS.Call("get_network_stats", 1);
			statsText.Text = $"Ping = {netStats[1]} \n ";
			
		}
		else if (roundFinished)
		{
			gsObj.Update(0, 0);

		}
		else
		{
			return;
		}
		ResetInputs();
		UpdateTime();
	}

	public void TrainingPhysicsProcess()
	{
		gsObj.Update(inputs, 0);
		ResetInputs();
		UpdateTime();
	}

	public void LocalPhysicsProcess()
	{
		gsObj.Update(inputs, p2inputs);
		ResetInputs();
		UpdateTime();
	}

	public void ggrs_advance_frame(Godot.Collections.Array<Godot.Collections.Array> combinedInputs)
	{
		int p1Inps = (int)combinedInputs[0][2];
		int p2Inps = (int)combinedInputs[1][2];

		gsObj.Update(p1Inps, p2Inps);
	}

	//GGPO callbacks
	public byte[] ggrs_save_game_state(int frame)
	{
		return gsObj.SaveGameState().DataArray;
	}

	public void ggrs_load_game_state(int frame, byte[] buffer, int checksum)
	{
		var buf = new StreamPeerBuffer();
		buf.DataArray = buffer;
		gsObj.LoadGameState(buf);
	}

	public void OnResetButtonDown()
	{
		gsObj.ResetGameState();
	}

	/// <summary>
	/// Called whenever the user presses a key, which gets added to the inputs int
	/// </summary>
	/// <param name="event"></param>
	public override void _Input(InputEvent @event)
	{
				if (@event.IsActionPressed("8"))
		{
			AddPress((int) Globals.Inputs.UP);
		}
		else if (@event.IsActionPressed("2"))
		{
			AddPress((int)Globals.Inputs.DOWN);
		}
		else if (@event.IsActionPressed("4"))
		{
			AddPress((int)Globals.Inputs.LEFT);
		}
		else if (@event.IsActionPressed("6"))
		{
			AddPress((int)Globals.Inputs.RIGHT);
		}
		else if (@event.IsActionPressed("p"))
		{
			AddPress((int)Globals.Inputs.PUNCH);
		}
		else if (@event.IsActionPressed("k"))
		{
			AddPress((int)Globals.Inputs.KICK);
		}
		else if (@event.IsActionPressed("s"))
		{
			AddPress((int)Globals.Inputs.SLASH);
		}
		else if (@event.IsActionReleased("8"))
		{
			AddRelease((int)Globals.Inputs.UP);
		}
		else if (@event.IsActionReleased("2"))
		{
			AddRelease((int)Globals.Inputs.DOWN);
		}
		else if (@event.IsActionReleased("4"))
		{
			AddRelease((int)Globals.Inputs.LEFT);
		}
		else if (@event.IsActionReleased("6"))
		{
			AddRelease((int)Globals.Inputs.RIGHT);
		}
		else if (@event.IsActionReleased("p"))
		{
			AddRelease((int)Globals.Inputs.PUNCH);
		}
		else if (@event.IsActionReleased("k"))
		{
			AddRelease((int)Globals.Inputs.KICK);
		}
		else if (@event.IsActionReleased("s"))
		{
			AddRelease((int)Globals.Inputs.SLASH);
		}

		if (Globals.mode != Globals.Mode.LOCAL)
		{
			return;
		}

		// P2 inputs in local mode handled below here

		if (@event.IsActionPressed("8b"))
		{
			AddP2Press((int)Globals.Inputs.UP);
		}
		else if (@event.IsActionPressed("2b"))
		{
			AddP2Press((int)Globals.Inputs.DOWN);
		}
		else if (@event.IsActionPressed("4b"))
		{
			AddP2Press((int)Globals.Inputs.LEFT);
		}
		else if (@event.IsActionPressed("6b"))
		{
			AddP2Press((int)Globals.Inputs.RIGHT);
		}
		else if (@event.IsActionPressed("pb"))
		{
			AddP2Press((int)Globals.Inputs.PUNCH);
		}
		else if (@event.IsActionPressed("kb"))
		{
			AddP2Press((int)Globals.Inputs.KICK);
		}
		else if (@event.IsActionPressed("sb"))
		{
			AddP2Press((int)Globals.Inputs.SLASH);
		}
		else if (@event.IsActionReleased("8b"))
		{
			AddP2Release((int)Globals.Inputs.UP);
		}
		else if (@event.IsActionReleased("2b"))
		{
			AddP2Release((int)Globals.Inputs.DOWN);
		}
		else if (@event.IsActionReleased("4b"))
		{
			AddP2Release((int)Globals.Inputs.LEFT);
		}
		else if (@event.IsActionReleased("6b"))
		{
			AddP2Release((int)Globals.Inputs.RIGHT);
		}
		else if (@event.IsActionReleased("pb"))
		{
			AddP2Release((int)Globals.Inputs.PUNCH);
		}
		else if (@event.IsActionReleased("kb"))
		{
			AddP2Release((int)Globals.Inputs.KICK);
		}
		else if (@event.IsActionReleased("sb"))
		{
			AddP2Release((int)Globals.Inputs.SLASH);
		}
	}
	private void AddPress(int key)
	{
		if (roundFinished || !roundStarted) // not the best place for this, but it works for now.  Eventually will want a message to send to each player
		{
			return;
		}
		int thisInput = key * 10;
		AddInput(thisInput);
	}
	private void AddRelease(int key)
	{
		int thisInput = key * 10 + 1;
		AddInput(thisInput);
	}

	private void AddP2Press(int key)
	{
		int thisInput = key * 10;
		AddP2Input(thisInput);
	}
	private void AddP2Release(int key)
	{
		int thisInput = key * 10 + 1;
		AddP2Input(thisInput);
	}


	/// <summary>
	/// 
	/// </summary>
	/// <param name="input"></param>
	/// <param name="p2">Sets the inputs for p2</param>
	private void AddInput(int input)
	{
		if (inputs == 0) //This is the first input of the frame
		{
			inputs = input;
			return;
		}

		string inputsCurr = inputs.ToString();
		
		if (inputsCurr.Length > 10) //Max 5 inputs per frame to prevent overflow
		{
			GD.Print("Too many inputs");
			return;
		}
		string newInput = input.ToString();
		inputs = int.Parse(inputsCurr + newInput);
	}

	private void AddP2Input(int input)
	{
		if (p2inputs == 0) //This is the first input of the frame
		{
			p2inputs = input;
			return;
		}

		string inputsCurr = p2inputs.ToString();

		if (inputsCurr.Length > 10) //Max 5 inputs per frame to prevent overflow
		{
			GD.Print("Too many inputs");
			return;
		}
		string newInput = input.ToString();
		p2inputs = int.Parse(inputsCurr + newInput);
	}

	// HUD

	private void WaitForConnectionDisplay()
	{
		P1.Visible = false;
		P2.Visible = false;
		centerText.Text = "WAITING FOR CONNECTION...";
		centerText.Visible = true;
	}

	private void Connected()
	{
		P1.Visible = true;
		P2.Visible = true;
		centerText.Visible = false;
	}

	private void PreRoundTime(int frame)
	{
		if (frame == 1)
		{
			centerText.Text = "3";
			P1.Visible = true;
			P2.Visible = true;
		}
		if (frame % countDownSpeed == 0)
		{
			centerText.Visible = true;
			centerText.Text = (3 - Mathf.FloorToInt(frame / countDownSpeed)).ToString();
			if (frame == countDownSpeed * 3)
			{
				roundStarted = true;
				centerText.Text = "FIGHT!";
			}
		}
	}

	private void MainGameTime(int frame)
	{
		int postIntroFrame = frame - countDownSpeed * 3;
		if (postIntroFrame / 60 < 100)
		{
			if (postIntroFrame == 60)
			{
				centerText.Visible = false; // "hide the 'FIGHT' center text"
			}

			if (postIntroFrame % 60 == 0)
			{
				timer.Text = (99 - Mathf.FloorToInt(postIntroFrame / 60)).ToString();
			}
			
		}

		else
		{
			
			centerText.Visible = true;
			centerText.Text = "TIME UP";
			EndRound();
			
		}
	}

	private void EndRound()
	{
		gameFinishFrame = gsObj.Frame;
		roundFinished = true;
	}

	private void PostGameTime(int frame)
	{
		int postGameFrame = frame - gameFinishFrame;
		if (postGameFrame > 200)
		{
			ReturnToLobby();
		}
	}

	/// <summary>
	/// This needs to be organized!!
	/// </summary>
	private void UpdateTime()
	{
		if (Globals.mode == Globals.Mode.TRAINING || Globals.mode == Globals.Mode.SYNCTEST)
		{
			return;
		}
		int frame = gsObj.Frame;

		if (!roundStarted)
		{
			PreRoundTime(frame);
		}
		else if (roundStarted && !roundFinished)
		{
			MainGameTime(frame);
		}
		else if (roundFinished)
		{
			gsObj.EndGame();
			PostGameTime(frame);
		}
		


		
	}
	public void OnPlayerComboChange(string name, int combo)
	{
		if (name == "P2")
		{
			if (combo > 1)
			{
				P1Combo.Call("combo", combo);
			}
			else
			{
				P1Combo.Call("off");
			}
		}

		else
		{
			if (combo > 1)
			{
				P2Combo.Call("combo", combo);
			}
			else
			{
				P2Combo.Call("off");
			}
		}
	}

	public void OnPlayerComboSet(string name, int combo)
	{
		if (name == "P2")
		{
			P1Combo.Call("combo_set", combo);
		}

		else
		{
			P2Combo.Call("combo_set", combo);
		}
	}
	public void OnPlayerHealthChange(string name, int health)
	{
		if (health < 1)
		{
			if (Globals.mode == Globals.Mode.TRAINING || Globals.mode == Globals.Mode.SYNCTEST) // eventually this should reset player health
			{
				return;
			}
			EndRound();
			centerText.Visible = true;
		}
		if (name == "P2")
		{
			P2Health.Value = health;
			centerText.Text = "P1 WINS";
		}

		else
		{
			P1Health.Value = health;
			centerText.Text = "P2 WINS";
		}
	}
	public void OnHadoukenEmitted(HadoukenPart h)
	{
		AddChild(h); // Add the hadouken as a child
		gsObj.NewHadouken(h); // let the gamestate object control it. this still needs to be cleaned up on deletion
		
	}

	public void OnHadoukenRemoved(HadoukenPart h)
	{
		
		gsObj.RemoveHadouken(h);
	}

	private void ReturnToLobby()
	{
		GD.Print("RETURN TO LOBBY FUNCTION");
		if (Globals.mode == Globals.Mode.GGPO)
		{

			//GD.Print("Closing GGPO session");
			//int closeResult = GGPO.CloseSession();
			//GD.Print($"GGPO session closed with code {closeResult}");
		}
		CloseMainscene();
	}

	private void CloseMainscene()
	{
		GD.Print("Emitting lobby return signal");
		EmitSignal(nameof(LobbyReturn));
		GD.Print("Emitted lobby return signal, queueing free");
		QueueFree();QueueFree();
	}
}