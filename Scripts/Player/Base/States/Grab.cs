using Godot;
using System;

public class Grab : State
{
	[Export]
	public int releaseFrame = 10;

	[Export]
	public Vector2 launch = new Vector2();

	[Export]
	public int dmg = 0;

	[Export]
	public int hitStun = 0;

	[Export]
	public int prorationLevel = 2;

	public bool released = false;

	public bool rightGrab = true;

    public override void _Ready()
    {
        base._Ready();
		AddCancel("Idle");
    }

    public override void Enter()
	{
		base.Enter();
		owner.velocity = new Vector2(0, 0);
		released = false;
		owner.otherPlayer.ChangeState("Grabbed");
		if (owner.CheckHeldKey('6'))
		{
			owner.TurnRight();
			rightGrab = true;
		}
		else
		{
			owner.TurnLeft();
			rightGrab = false;
		}

	}

	public override void HandleInput(char[] inputArr)
	{
		if (frameCount < releaseFrame)
		{
			return;
		}
		base.HandleInput(inputArr);
	}

	public override void FrameAdvance()
	{
		base.FrameAdvance();
		if (frameCount < releaseFrame)
		{
			Vector2 relGrabPosition = owner.grabPos.Position * 100;
			if (!rightGrab)
			{
				relGrabPosition.x *= -1;
			}

			Vector2 absGrabPosition = relGrabPosition + owner.internalPos;

			owner.otherPlayer.internalPos =  absGrabPosition;
		}
		
		else if ((frameCount == releaseFrame) && !released)
		{
			Vector2 actualLaunch = launch;
			if (!rightGrab)
			{
				actualLaunch.x *= -1;
			}

			var direction = BaseAttack.ATTACKDIR.EQUAL;

			if (owner.OtherPlayerOnRight())
			{
				direction = BaseAttack.ATTACKDIR.RIGHT;
			}
			else if (owner.OtherPlayerOnLeft())
			{
				direction = BaseAttack.ATTACKDIR.LEFT;
			}

			owner.otherPlayer.ReceiveHit(Vector2.Inf, direction, dmg, hitStun, hitStun, HEIGHT.MID, 0, launch, false, prorationLevel);
		}
	}
	public override void AnimationFinished()
	{
		EmitSignal(nameof(StateFinished), "Idle");
	}
}
