using Godot;
using System;
using System.Collections.Generic;

class BaseManager : Node2D
{
    [Export]
    public PackedScene charSelectScene;

    [Export]
    public PackedScene mainGameScene;

    protected BaseGame currGame;

    public override void _Ready()
    {
        StartCharSelect();
    }

    protected virtual void StartCharSelect()
    {
        currGame = charSelectScene.Instance() as BaseGame;
        AddChild(currGame);
        currGame.Connect("Finished", this, nameof(OnCharSelectFinished));
    }

    public virtual void OnCharSelectFinished()
    {
        StartGame();
    }

    protected virtual void StartGame()
    {
        currGame.QueueFree();
        currGame = mainGameScene.Instance() as BaseGame;
        AddChild(currGame);
        currGame.Connect("Finished", this, nameof(OnGameFinished));
    }

    public virtual void OnGameFinished()
    {
        QueueFree();
    }

    public virtual List<char[]> GetLocalInputs(string end)
    {
        return new List<char[]>();
    }
}