﻿using Godot;
using System;
using System.Collections.Generic;

public class Idle : State
{
    public override void _Ready()
    {
        base._Ready();
        loop = true;
    }
    public override void Enter()
    {
        base.Enter();
        owner.velocity.x = 0;
        owner.velocity.y = 0;
        if (owner.CheckHeldKey('2'))
        {
            EmitSignal(nameof(StateFinished), "Crouch");
            return;
        }

        if (owner.CheckHeldKey('6'))
        {
            owner.velocity.x = owner.speed;
            
            EmitSignal(nameof(StateFinished), "Walk");
            return;
        }

        else if (owner.CheckHeldKey('4'))
        {
            owner.velocity.x = -owner.speed;
            EmitSignal(nameof(StateFinished), "Walk");
            return;
        }

        else if (owner.CheckHeldKey('8'))
        {
            EmitSignal(nameof(StateFinished), "Jump");
            return;
        }
    } 
    public override void HandleInput(char[] inputArr)
    {
        if (Globals.CheckKeyPress(inputArr, 'p'))
        {
            EmitSignal(nameof(StateFinished), "Jab");

            if (owner.facingRight && owner.CheckBufferComplex(new List<char[]>() { new char[] { '2', 'p' }, new char[] { '6', 'p' }, new char[] { '2', 'r' } }))
            {
                EmitSignal(nameof(StateFinished), "Hadouken");
            }
            else if ((!owner.facingRight) && owner.CheckBufferComplex(new List<char[]>() { new char[] { '2', 'p' }, new char[] { '4', 'p' }, new char[] { '2', 'r' } }))
            {
                EmitSignal(nameof(StateFinished), "Hadouken");
            }
        }
        else if (Globals.CheckKeyPress(inputArr, '2'))
        {
            EmitSignal(nameof(StateFinished), "Crouch");
            return;
        }
        else if (Globals.CheckKeyPress(inputArr, '6'))
        {
            if (owner.CheckBufferComplex(new List<char[]>() { new char[] { '6', 'p' }, new char[] { '6', 'p' } }))
            {
                owner.velocity.x = owner.speed * 2;
                if (!owner.facingRight)
                {
                    EmitSignal(nameof(StateFinished), "Backdash");
                }
                else 
                {
                    owner.velocity.x = owner.dashSpeed;
                    EmitSignal(nameof(StateFinished), "Run");
                }
                
            }
            else
            {
                owner.velocity.x = owner.speed;
                GD.Print($"Setting speed to {owner.speed}");
                EmitSignal(nameof(StateFinished), "Walk");
            }

        }

        else if (Globals.CheckKeyPress(inputArr, '4'))
        {
            if (owner.CheckBufferComplex(new List<char[]>() { new char[] { '4', 'p' }, new char[] { '4', 'p' } }))
            {
                owner.velocity.x = owner.speed * -2;
                if (owner.facingRight)
                {
                    EmitSignal(nameof(StateFinished), "Backdash");
                }
                else 
                {
                    owner.velocity.x = -owner.dashSpeed;
                    EmitSignal(nameof(StateFinished), "Run");
                }
            }
            else
            {
                owner.velocity.x = -owner.speed;
                EmitSignal(nameof(StateFinished), "Walk");
            }
            
        }

        else if (Globals.CheckKeyPress(inputArr, '8'))
        {
            EmitSignal(nameof(StateFinished), "Jump");
        }

        else if (Globals.CheckKeyPress(inputArr, 'k'))
        {
            EmitSignal(nameof(StateFinished), "Kick");
        }

        else if (Globals.CheckKeyPress(inputArr, 's'))
        {
            EmitSignal(nameof(StateFinished), "Slash");
        }
    }

    public override void FrameAdvance()
    {
        base.FrameAdvance();
        owner.velocity.x = 0;
        owner.CheckTurnAround();
        if (owner.CheckHeldKey('8'))
        {
            EmitSignal(nameof(StateFinished), "Jump");
        }
    }
}

