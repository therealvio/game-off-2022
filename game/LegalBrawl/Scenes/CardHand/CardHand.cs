using Godot;
using System;
using System.Collections.Generic;

public class CardHand : Control
{
    [Export]
    public float CardWidth;
    [Export]
    public float ShiftRatio;
    [Export]
    public float CardRotation;
    [Export]
    public float LiftRatio;
    CardList _cards;
    Control _cardAnchor;
    bool keyPress = false;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _cardAnchor = GetNode<Control>("Anchor");
        _cards = new CardList();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        bool assignPositions = false;

        if (Input.IsPhysicalKeyPressed((int)Godot.KeyList.A))
        {
            if (!keyPress)
            {
                keyPress = true;
                Card card = SceneManager.Create<Card>(SceneManager.Scenes.Card, this);
                _cards.Add(card);
                assignPositions = true;
            }
        }
        else if (Input.IsPhysicalKeyPressed((int)Godot.KeyList.D))
        {
            if (!keyPress)
            {
                keyPress = true;
                _cards.Get(_cards.Count - 1).Free();
                _cards.RemoveAt(_cards.Count - 1);
                assignPositions = true;
            }
        }
        else
            keyPress = false;

        if (_cards.IsHeld && _cards.IsHovered)
        {
            if (_cards.LastHeld != _cards.LastHovered && !_cards.LastHovered.IsMoving)
            {
                _cards.MoveHeldToHover();
            }
        }

        // if (_cards.IsHeld)
        //     assignPositions = true;


        // if (assignPositions)
        AssignCardPositions();

    }

    public CardPosition[] CalculateCardPositions()
    {
        float cardShift = CardWidth * ShiftRatio * (_cards.IsHeld ? 2 : 1);
        float cardLift = CardWidth * LiftRatio;
        int cardCount = _cards.Count;
        CardPosition[] cardPositions = new CardPosition[cardCount];

        float maxWidth = ((cardShift) * cardCount) - cardShift;
        float shiftStep = 0;
        float rotStep = CardRotation * ((cardCount - 1) / 2f);
        Vector2 offset = _cardAnchor.RectGlobalPosition;

        for (int i = 0; i < cardPositions.Length; i++)
        {
            float lift = Mathf.Pow((i - ((cardCount - 1) / 2f)), 2) * cardLift;
            Vector2 point = new Vector2(shiftStep - (maxWidth / 2), lift);
            cardPositions[i] = new CardPosition(point + offset, rotStep);
            shiftStep += cardShift; //step = (cardGap * cardCount) - cardGap * (cardCount / 2)
            rotStep -= CardRotation;
            //GD.Print(point);
        }

        return cardPositions;
    }

    public void AssignCardPositions()
    {
        var positions = CalculateCardPositions();
        for (int i = 0; i < _cards.Count; i++)
        {
            _cards.Get(i).SetFixedPosition(positions[i]);
        }
    }
}