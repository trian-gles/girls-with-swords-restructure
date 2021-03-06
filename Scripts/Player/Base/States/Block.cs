using Godot;
using System;

public class Block : HitState
{
	public override void _Ready()
	{
		base._Ready();
		loop = true;
	}

	public override void Enter()
	{
		base.Enter();
		owner.ScheduleEvent(EventScheduler.EventType.AUDIO);
	}
	public override void FrameAdvance()
	{
		base.FrameAdvance();
		stunRemaining--;
		if (stunRemaining == 0)
		{
			if (owner.grounded)
			{
				EmitSignal(nameof(StateFinished), "Idle");
			}
			else
			{
				EmitSignal(nameof(StateFinished), "Fall");
			}
			
		}
		if (!owner.grounded)
		{
			ApplyGravity();
		}
	}


	public override void receiveStun(int hitStun, int blockStun)
	{
		stunRemaining = blockStun;
	}

	/// <summary>
	/// Not multiplied by proration because this is chip damage.  Also no proration is applied
	/// </summary>
	/// <param name="dmg"></param>
	public override void receiveDamage(int dmg, int prorationLevel)
	{
		owner.DeductHealth(dmg);
	}
}

