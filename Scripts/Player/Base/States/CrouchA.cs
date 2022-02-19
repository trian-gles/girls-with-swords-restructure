using Godot;
using System;
using System.Collections.Generic;

public class CrouchA : GroundAttack
{
	public override void _Ready()
	{
		base._Ready();
		AddJumpCancel();
		AddGatling(new char[] { '8', 'p' }, "Jump");
		AddGatling(new char[] { 'p', 'p' }, () => owner.CheckHeldKey('2'), "CrouchA");
		AddGatling(new char[] { 'k', 'p' }, () => owner.CheckHeldKey('2'), "CrouchB");
		AddGatling(new char[] { 's', 'p' }, () => owner.CheckHeldKey('2'), "CrouchC");
		AddGatling(new char[] { 'p', 'p' }, "Jab");
		AddGatling(new char[] { 'k', 'p' }, "Kick");
		AddGatling(new char[] { 's', 'p' }, "Slash");
		
		
	}
}

