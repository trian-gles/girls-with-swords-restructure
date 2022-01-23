using Godot;
using System;

public class Jump : State
{

	[Export]
	public int jumpForce = 700;
	public override void Enter()
	{
		base.Enter();
		owner.velocity.y = -1 * jumpForce;
		owner.grounded = false;
		owner.ScheduleEvent(EventScheduler.EventType.AUDIO, "Jump", Name);

		AddGatling(new[] { 'p', 'p' }, "JumpA");
		AddGatling(new[] { 'k', 'p' }, "JumpB");
		AddGatling(new[] { 's', 'p' }, "JumpC");
		AddGatling(new[] { '8', 'p' }, () => owner.canDoubleJump, "DoubleJump", () => { owner.canDoubleJump = false; });
	}


	public override void FrameAdvance()
	{
		base.FrameAdvance();
		if (owner.grounded && frameCount > 0) 
		{
			EmitSignal(nameof(StateFinished), "Idle");
		}
		ApplyGravity();
		
	}

	public override void PushMovement(float _xVel)
	{
	}

	public override void AnimationFinished()
	{
		EmitSignal(nameof(StateFinished), "Fall");
	}
}


