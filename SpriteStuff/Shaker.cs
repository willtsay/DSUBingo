using Godot;
using System;
using System.Net.Mail;

public class Shaker : KinematicBody2D
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";
    private float _scaleSpeed = 0.008f;
    private float time = 0;
    AnimatedSprite _sprite;
    private bool _spawning;
    private Vector2 _velocity;
    private float _bounceSpeed;
    // Called when the node ente rs the scene tree for the first time.
    public override void _Ready()
    {

        _sprite = this.GetNode<AnimatedSprite>("Sprite");
        var rect = GetViewportRect();
        var width = rect.Size.x;
        var height = rect.Size.y;
        this.Position = new Vector2(width/2, height/2);
        this.Scale = new Vector2(.1f, .1f);
        _spawning = true;
        _velocity = new Vector2(-120, -120);
    }

    public void UseRainbow()
    {
        (_sprite.Material as ShaderMaterial).SetShaderParam("rainbow_active", true);
    }

    public void SendToJail()
    {
        _spawning = false;
        this.Position = new Vector2(1032, 522);
        this.Scale = new Vector2(1, 1) ;
        this.ZIndex = -1;
        _sprite.SpeedScale = 0.5f;
    }


    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (_spawning) {
            time += delta * _scaleSpeed;
            this.Scale = this.Scale.LinearInterpolate(new Vector2(4, 4), time);
            _sprite.SpeedScale += .3f * delta;

            if (this.Scale.x >= 2)
            {
                this.Modulate = new Color(this.Modulate.r, this.Modulate.g, this.Modulate.b, this.Modulate.a - .5f * delta);

            }
            if (this.Modulate.a <= 0)
            {
                SendToJail();
            }
        }



        
    }

    public override void _PhysicsProcess(float delta)
    {
        if (!_spawning)
        {
            if (this.Modulate.a <= 1)
            {
                this.Modulate = new Color(this.Modulate, this.Modulate.a + .05f);
            }

            var collision = this.MoveAndCollide(_velocity * delta);
            if (collision != null)
            {
                _velocity = _velocity.Bounce(collision.Normal);
            }

        }
    }



}
