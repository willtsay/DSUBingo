using Godot;
using System;

public class RatJammy : KinematicBody2D
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    public float Speed = 500;

    private float _minScale = 0.5f;
    private float _maxScale = 2.0f;
    private float _scaleStep = 0.2f;
    private bool _isGrowing = true;
    public Vector2 direction = new Vector2(1,1);

    // the underlying sprite is 96x96

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        direction = direction.Normalized();
    }

    private void despawn()
    {
        QueueFree();
    }

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        this.MoveAndSlide(Speed * direction);

        // as long as the rat jammies are always falling "down"this queuefree should be fine
        if (this.Position.y > 2000)
        {
            QueueFree();
        }



    }
}
