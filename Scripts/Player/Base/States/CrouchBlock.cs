using Godot;
using System;

public class CrouchBlock : State
{
    public override void _Ready()
    {
        base._Ready();
        loop = true;
    }
    public override void FrameAdvance()
    {
        base.FrameAdvance();
        stunRemaining--;
        if (stunRemaining == 0)
        {
            if (owner.grounded)
            {
                EmitSignal(nameof(StateFinished), "Crouch");
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

    public override void receiveDamage(int dmg, int prorationLevel)
    {
        owner.DeductHealth(dmg);
    }
}
