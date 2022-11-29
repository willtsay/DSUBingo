using Godot;
using System;

public class TestGoogleCsv : Control
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";
    
    static string CSVUrl = "https://docs.google.com/spreadsheets/d/15MBfpaL4ybL5gVzow5KPxU7X9r8lJgRnHOzWVcRF7Rk/export?format=csv&id=15MBfpaL4ybL5gVzow5KPxU7X9r8lJgRnHOzWVcRF7Rk&gid=0";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        HTTPRequest httpRequest = new HTTPRequest();
        this.AddChild(httpRequest);
        httpRequest.Connect("request_completed", this, "GIMMECSV");
        var e = httpRequest.Request(CSVUrl);
        if (e != Error.Ok)
        {
            GD.Print("failed request"); // push?
        }

    }

    private void GIMMECSV(int result, int response_code, string[] headers, byte[] body)
    {
        GD.Print(result);
        GD.Print(response_code);
        GD.Print(body);
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
