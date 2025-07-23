using Godot;
using System;

namespace projectblacklodge;
public partial class Player : CharacterBody3D
{
    [Export]
    public float Speed { get; set; } = 5.0f;
    [Export]
    public float MouseSensitivity { get; set; } = 0.002f;
    [Export]
    public Camera3D Camera;

    private float Gravity { get; set; } = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

    public override void _Ready()
    {

        // Hide the mouse cursor and lock it to the game window.
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            RotateY(-mouseMotion.Relative.X * MouseSensitivity);
            Camera.RotateX(-mouseMotion.Relative.Y * MouseSensitivity);

            // Clamp the camera's vertical rotation to prevent it from flipping over.
            var cameraRotation = Camera.RotationDegrees;
            cameraRotation.X = Mathf.Clamp(cameraRotation.X, -90f, 90f);
            Camera.RotationDegrees = cameraRotation;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
        var direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        var velocity = Velocity;

        if(!IsOnFloor())
            velocity.Y -= Gravity * (float)delta;

        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        }
        else {
            velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed);
        }

        Velocity = velocity;

        MoveAndSlide();
    }
}
