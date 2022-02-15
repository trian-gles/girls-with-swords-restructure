using Godot;
using System;
using System.Collections.Generic;

public class GLCrouch : Crouch
{
    public override void _Ready()
    {
        base._Ready();
        AddGatling(new List<char[]>() { new char[] { '6', 'p' }, new char[] { '2', 'p' }, new char[] { '6', 'p' }, new char[] { 'p', 'p' } }, "GLDP");
    }

    public override void Enter()
    {
        base.Enter();
        //GD.Print("Entering GL crouch");
    }

}
