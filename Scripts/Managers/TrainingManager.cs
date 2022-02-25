using Godot;
using System;
using System.Collections.Generic;

class TrainingManager : BaseManager
{
    public override void _PhysicsProcess(float delta)
    {
        List<char[]> p1Inputs = GetLocalInputs("");
        List<char[]> p2Inputs = GetLocalInputs("b");
        currGame.AdvanceFrame(p1Inputs, new List<char[]>());
    }
}