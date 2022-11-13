using Godot;
using System;

public class BaseCard : Resource
{
    [Export]
    public int Id;
    [Export]
    public string Name;
    [Export]
    public int Cost;
    [Export]
    public Texture Art;

    private Battle _battle;
    private PlayerTypes _owner;
    public virtual void OnPlay() { }
    public virtual string GetDescription() => "No description available";

    public void Initialise(Battle battle, PlayerTypes owner)
    {
        _battle = battle;
        _owner = owner;
    }

    public void Play()
    {
        OnPlay();
    }

    public void ModifyCredibility(int value, bool self = true)
    {
        PlayerTypes target = self ? _owner : Lawyer.GetOther(_owner);
        _battle.ModifyCredibility(_owner, value, target);
    }
}
