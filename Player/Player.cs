using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Player : Node2D
{
	private State currentState;
	public Player otherPlayer; //I know I shouldn't do this, but it makes my life so much easier...

	[Signal]
	public delegate void HealthChanged(string name, int health);
	[Signal]
	public delegate void ComboChanged(string name, int combo);
	[Signal]
	public delegate void HitConfirm();

	[Export]
	public int speed = 4;

	[Export]
	public int dashSpeed = 7;

	[Export]
	public int jumpForce = 7;

	[Export]
	public int gravityDenom = 5; //GGPO can only store ints due to issues with float determinism so I have to do some wacky stuff

	[Export]
	public bool dummy = false; //you can use this for testing with a dummy

	private InputHandler inputHandler;

	// All of these will be stored in gamestate
	private int health = 100;
	public Vector2 velocity = new Vector2(0, 0);
	public int gravityPos = 0;
	public bool facingRight = true;
	public bool grounded;
	private int combo = 0;

	/// <summary>
	/// Contains all vital data for saving gamestate
	/// </summary>
	[Serializable]
	public struct PlayerState
	{
		public List<char[]> inBuf2 { get; set; }
		public int inBuf2Timer { get; set; }
		public List<char> heldKeys { get; set; }
		public List<char[]> unhandledInputs { get; set; }
		public string currentState { get; set; }
		public bool hitConnect { get; set; }
		public int frameCount { get; set; }
		public int stunRemaining { get; set; }

		public bool flipH { get; set; }
		public int health { get; set; }
		public int[] position { get; set; }
		public int gravityPos { get; set; }
		public int[] velocity { get; set; }
		public bool facingRight { get; set; }
		public bool touchingWall { get; set; }
		public bool grounded { get; set; }
		public int combo { get; set; }


	}
	

	private Color hitColor = new Color(255, 0, 0, 0.5f);
	private Color hurtColor = new Color(0, 255, 0, 0.5f);
	private Color colColor = new Color(0, 0, 255, 0.5f);

	private Area2D hitBoxes;
	public Area2D hurtBoxes;
	private CollisionShape2D colBox;
	public AnimationPlayer animationPlayer;
	private Sprite sprite;
	

	public override void _Ready()
	{
		hitBoxes = GetNode<Area2D>("HitBoxes");
		hurtBoxes = GetNode<Area2D>("HurtBoxes");
		colBox = GetNode<CollisionShape2D>("CollisionBox");
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		sprite = GetNode<Sprite>("Sprite");
		animationPlayer.Connect("AnimationFinished", this, nameof(AnimationFinished));
		foreach (CollisionShape2D box in hitBoxes.GetChildren()) 
		{
			box.Shape = new RectangleShape2D();
		}
		foreach (CollisionShape2D box in hurtBoxes.GetChildren())
		{
			box.Shape = new RectangleShape2D();
		}

		inputHandler = new InputHandler();
		Godot.Collections.Array allStates = GetNode<Node>("StateTree").GetChildren();
		foreach (Node state in allStates) 
		{
			state.Connect("StateFinished", this, nameof(ChangeState));
		}
		currentState = GetNode<State>("StateTree/Idle");
		ChangeState("Idle");
	}

	public PlayerState GetState()
	{
		var pState = new PlayerState();
		pState.inBuf2 = inputHandler.inBuf2;
		pState.inBuf2Timer = inputHandler.inBuf2Timer;
		pState.heldKeys = inputHandler.heldKeys;
		pState.unhandledInputs = inputHandler.unhandledInputs;
		pState.currentState = currentState.Name;
		pState.frameCount = currentState.frameCount;
		pState.hitConnect = currentState.hitConnect;
		pState.stunRemaining = currentState.stunRemaining;
		pState.flipH = sprite.FlipH;

		pState.health = health;
		pState.position = new int[] { (int)Position.x, (int)Position.y };


		pState.velocity = new int[] { (int)velocity.x, (int)velocity.y };
		pState.gravityPos = gravityPos;
		pState.facingRight = facingRight;
		pState.grounded = grounded;
		pState.combo = combo;
		return pState;
	}

	public void SetState(PlayerState pState)
	{
		inputHandler.inBuf2 = pState.inBuf2;
		inputHandler.inBuf2Timer = pState.inBuf2Timer;
		inputHandler.heldKeys = pState.heldKeys;
		inputHandler.unhandledInputs = pState.unhandledInputs;
		currentState = GetNode<State>("StateTree/" + pState.currentState);
		currentState.hitConnect = pState.hitConnect;
		currentState.frameCount = pState.frameCount;
		animationPlayer.SetAnimationAndFrame(pState.currentState, pState.frameCount);
		currentState.stunRemaining = pState.stunRemaining;
		sprite.FlipH = pState.flipH;

		health = pState.health;
		Position = new Vector2(pState.position[0], pState.position[1]);
		GD.Print($"setting {Name} position to {Position}");
		gravityPos = pState.gravityPos;
		velocity = new Vector2(pState.velocity[0], pState.velocity[1]);
		facingRight = pState.facingRight;
		grounded = pState.grounded;
		combo = pState.combo;
		EmitSignal(nameof(ComboChanged), Name, combo);

	}

	/// <summary>
	/// Right now this is an unneccessary step in input handling, but it works so I'll leave it for now
	/// </summary>
	/// <param name="inputs"></param>
	public void SetUnhandledInputs(List<char[]> inputs)
	{
		inputHandler.SetUnhandledInputs(new List<char[]>(inputs));
	}

	/// <summary>
	/// Deals with unhandled inputs, the input buffer, and a hitstop buffer.  Subject to constant change
	/// </summary>
	private class InputHandler 
	{
		public List<char[]> inBuf2 = new List<char[]>();
		public int inBuf2TimerMax = 8;
		public int inBuf2Timer = 8;
		public List<char> heldKeys = new List<char>();
		public List<char[]> unhandledInputs = new List<char[]>();

		public void Buf2AddInputs(List<char[]> newInputs) 
		{ 
			if (!newInputs.Any())
            {
				if (inBuf2Timer > 0)
                {
					inBuf2Timer--;
				}
				else
                {
					inBuf2 = new List<char[]>();
				}
            }
			else
            { // would it be faster with concat
				foreach (char[] newInput in newInputs)
                {
					inBuf2Timer = inBuf2TimerMax;
					inBuf2.Add(newInput);
                }
            }
		}
		public void SetUnhandledInputs(List<char[]> thisFrameInputs) 
		{
			unhandledInputs.AddRange(thisFrameInputs);
		}
		public void FrameAdvance(int hitStop, State currentState) 
		{
			if (hitStop > 0) // delay the handling of inputs until after hitstop ends
			{
				return;
			}
			List<char[]> curBufStep = new List<char[]>();
			foreach (char[] inputArr in unhandledInputs)
			{
				
				// Hold or release keys
				if (inputArr[1] == 'p')
				{
					heldKeys.Add(inputArr[0]);
					
				}
				else if (inputArr[1] == 'r')
				{
					bool removeResult = heldKeys.Remove(inputArr[0]);
					if (inputArr[0] == '4' || inputArr[0] == '6')
					{
						GD.Print($"Result of removing {inputArr[0]} from heldKeys = {removeResult}");
					}
					
				}
				
				
				curBufStep.Add(inputArr);
			}

			Buf2AddInputs(curBufStep); // new better input buffer
			
			foreach (char[] inputArr in unhandledInputs)
			{
				currentState.HandleInput(inputArr);
			}
			unhandledInputs.Clear();
		}

		public List<char[]> GetBuffer() 
		{
			return inBuf2;
		}
	}//input buffer needs to be tested with GGPO!!!

	/// <summary>
	/// Call the Enter() and Exit() methods of the current state and go to a new one
	/// </summary>
	/// <param name="nextStateName"></param>
	public void ChangeState(string nextStateName) 
	{
		currentState.Exit();
		animationPlayer.NewAnimation(nextStateName);
		currentState = GetNode<State>("StateTree/" + nextStateName);
		
		currentState.Enter();
		CheckTurnAround();

	}

	public void AnimationFinished(string animName) 
	{
		if (currentState.loop) 
		{
			animationPlayer.Restart();
		}
		else
		{
			currentState.AnimationFinished();
		}
	}

	public bool CheckHeldKey(char key) 
	{
		return (inputHandler.heldKeys.Contains(key));
	}

	/// <summary>
	/// Checks if the key is in the input buffer
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public bool CheckBuffer(char[] key)
	{
		
		return Globals.ArrayInList(inputHandler.GetBuffer(), key);
	}

	/// <summary>
	/// Checks if the sequence of inputs in elements can be found in order in the buffer
	/// </summary>
	/// <param name="elements"></param>
	/// <returns></returns>
	public bool CheckBufferComplex(List<char[]> elements)
	{
		foreach (char[] input in inputHandler.GetBuffer()) { GD.Print(input[0]); }
		return Globals.ArrOfArraysComplexInList(inputHandler.GetBuffer(), elements);
	}

	public void FrameAdvanceInputs(int hitStop)
	{
		inputHandler.FrameAdvance(hitStop, currentState);
	}
	public void FrameAdvance() 
	{
		
		Update();
		
		animationPlayer.FrameAdvance();

		if (CheckHurtRect())
		{
			currentState.InHurtbox();
		}
		currentState.FrameAdvance();

		MoveSlideDeterministicOne();
	}

	/// <summary>
	/// First half of the integer based, deterministic collision detection system.
	/// </summary>
	private void MoveSlideDeterministicOne()
	{
		int xChange = (int)Math.Floor(velocity.x / 2);
		int yChange = (int)Math.Floor(velocity.y / 2);
		Position += new Vector2(xChange, yChange);
		CorrectPositionBounds();
	}

	/// <summary>
	/// Finishes the movement system
	/// </summary>
	public void MoveSlideDeterministicTwo()
	{
		int xChange = (int)Math.Ceiling(velocity.x / 2);
		int yChange = (int)Math.Ceiling(velocity.y / 2);
		Position += new Vector2(xChange, yChange);
		CorrectPositionBounds();
	}

	/// <summary>
	/// Stay inside the bounds of the stage
	/// </summary>
	private void CorrectPositionBounds()
	{
		if (Position.y > 220)
		{
			Position = new Vector2(Position.x, 220);
			grounded = true;
		}

		if (Position.x > 475)
		{
			Position = new Vector2(475, Position.y);
		}
		else if (Position.x < 5)
		{
			Position = new Vector2(5, Position.y);
		}
	}

	public bool CheckTouchingWall()
	{
		if (Position.x > 474 || Position.x < 6)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public void SlideAway() 
	{
		var mod = 1;

		if (Position.x < otherPlayer.Position.x) 
		{
			mod = -1;
		}
		GlobalPosition = new Vector2(GlobalPosition.x + 4 * mod, GlobalPosition.y);
	}

	public void PushMovement(float xVel) 
	{
		currentState.PushMovement(xVel);
	}

	public bool OtherPlayerOnRight()
	{
		if (Position.x > otherPlayer.Position.x)
		{
			return false;
		}
		else
		{
			return true;
		}
	}

	/// <summary>
	/// Called to check if the player should change directions.  Always called when changing states.  Some states call this in their FrameAdvance() methods.
	/// </summary>
	public void CheckTurnAround() 
	{
		if (otherPlayer == null) 
		{
			return;
		}
		if (!OtherPlayerOnRight() && facingRight)
		{
			TurnLeft();
		}
		else if (OtherPlayerOnRight() && !facingRight) 
		{
			TurnRight();
		}
	}

	public void TurnRight()
	{
		facingRight = true;

		GetNode<Sprite>("Sprite").FlipH = false;

		hurtBoxes.Scale = new Vector2(1, 1);
		hitBoxes.Scale = new Vector2(1, 1);
	}

	public void TurnLeft()
	{
		facingRight = false;

		GetNode<Sprite>("Sprite").FlipH = true;

		hurtBoxes.Scale = new Vector2(-1, 1);
		hitBoxes.Scale = new Vector2(-1, 1);
	}

	public void ReceiveHit(bool rightAttack, int dmg, int stun, State.HEIGHT height, Vector2 push, bool knockdown) 
	{
		currentState.ReceiveHit(rightAttack, height, push, knockdown);
		currentState.receiveStun(stun);
		currentState.receiveDamage(dmg);
		EmitSignal(nameof(HitConfirm));
	}

	public void OnHitConnected(Vector2 hitPush) 
	{
		if (otherPlayer.CheckTouchingWall())
		{
			if (OtherPlayerOnRight())
			{
				velocity.x = -hitPush.x;
			}
			else
			{
				velocity.x = hitPush.x;
			}
		}
	}

	public void ResetCombo()
	{
		combo = 0;
		EmitSignal(nameof(ComboChanged), Name, combo);
	}

	public void ComboUp()
	{
		combo++;
		EmitSignal(nameof(ComboChanged), Name, combo);
	}

	public void DeductHealth(int dmg)
	{
		health -= dmg;
		EmitSignal(nameof(HealthChanged), Name, health);
	}

	public bool CheckHurtRect()
	{
		List<Rect2> myRects = GetRects(hurtBoxes, true);
		List<Rect2> otherRects = otherPlayer.GetRects(otherPlayer.hitBoxes, true);
		foreach (Rect2 hurtRect in myRects)
		{
			foreach (Rect2 hitRect in otherRects)
			{
				if (hurtRect.Intersects(hitRect))
				{
					return true;
				}
			}
		}
		return false;
	}

	public List<Rect2> GetRects(Area2D area, bool globalPosition = false) 
	{
		List<Rect2> allRects = new List<Rect2>();
		foreach (CollisionShape2D colShape in area.GetChildren()) 
		{
			if (!colShape.Disabled)
			{
				allRects.Add(GetRect(colShape, globalPosition));
			}
			
		}
		return allRects;
	}

	public Rect2 GetRect(CollisionShape2D colShape, bool globalPosition = false) 
	{
		RectangleShape2D shape = (RectangleShape2D)colShape.Shape;
		Vector2 extents = shape.Extents * 2;
		Vector2 position;
		if (facingRight)
		{
			position = colShape.Position - extents / 2;
		}
		else
		{
			position = new Vector2(-colShape.Position.x - extents.x / 2, colShape.Position.y - extents.y / 2);
		}
		if (globalPosition)
		{
			position += Position;
		}
		return new Rect2(position, extents);
	}

	public Rect2 GetCollisionRect()
	{
		return GetRect(colBox);
	}

	public void DebugDisplay()
	{
		GetNode<Label>("DebugPos").Text = Position.ToString();
	}

	/// <summary>
	/// You can use this if you want to draw all the boxes
	/// </summary>
	public override void _Draw()
	{
		if (Globals.mode == Globals.Mode.TRAINING)
		{
			List<Rect2> hitRects = GetRects(hitBoxes);
			List<Rect2> hurtRects = GetRects(hurtBoxes);
			Rect2 colRect = GetRect(colBox);


			foreach (Rect2 rect in hitRects)
			{
				DrawRect(rect, hitColor);
			}

			foreach (Rect2 rect in hurtRects)
			{
				DrawRect(rect, hurtColor);
			}

			DrawRect(colRect, colColor);
		}
		
	}
}
