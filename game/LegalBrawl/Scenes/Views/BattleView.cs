using Godot;
using System;

public class BattleView : View
{
    [Signal] public delegate void NextCard();
    [Signal] public delegate void FinishBattle();

    private Lawyer _player;
    private Lawyer _opponent;
    private Button _nextButton;
    private AnimationPlayer _animator;
    private Label _winnerLabel;
    public override void _Ready()
    {
        base._Ready();

        _player = FindNode("Player") as Lawyer;
        _player.Represents(PlayerTypes.Player);

        _opponent = FindNode("Opponent") as Lawyer;
        _opponent.Represents(PlayerTypes.Opponent);

        _nextButton = FindNode("NextButton") as Button;
        _nextButton.Connect("pressed", this, "OnNextPressed");

        _animator = GetNode<AnimationPlayer>("AnimationPlayer");
        _winnerLabel = FindNode("WinnerLabel") as Label;
    }

    public override void Setup()
    {
        _winnerLabel.Text = "";
    }

    public void OnNextPressed()
    {
        EmitSignal("NextCard");
    }

    public void OnPlayCard(int cardId, PlayerTypes character, Battle battle)
    {
        GetLawyer(character).Play(cardId, battle);
    }

    public void OnCredibilityChange(PlayerTypes character, int from, int to)
    {
        GetLawyer(character).UpdateCredibility(from, to);
    }

    public void OnLastCard()
    {
        _nextButton.Hide();
        _animator.Play("Wait");
    }

    public void OnDeclareWinner(PlayerTypes winner)
    {
        _winnerLabel.Text = $"{winner} wins!";
    }

    public void CallFinishBattle()
    {
        EmitSignal("FinishBattle");
    }

    public Lawyer GetLawyer(PlayerTypes character)
    {
        if (character == PlayerTypes.Player)
            return _player;
        return _opponent;
    }
}
