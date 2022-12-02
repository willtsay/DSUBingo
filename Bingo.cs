using Godot;
using System;
using System.Dynamic;
using static Godot.HTTPRequest;
using System.Runtime.Remoting.Messaging;
using System.Text;
using Godot.Collections;
using System.Collections.Generic;
using System.Collections;
using System.Drawing.Text;
using System.Linq;

public class Bingo : Container
{
    
    HTTPRequest _httpRequest;
    GridContainer _gc;
    List<Button> _bingoButtons;
    List<Label> _bingoLabels;
    List<List<Button>> _winConditionChecker;
    Button _generateButton;
    OptionButton _optionButton;
    CheckBox _usingFillButton;

    // packed scenes
    PackedScene _ratJammy;

    // jammy rain

    float rainDelay = 0.3f;
    float timeUntilRain = 0;



    [Signal]
    public delegate void NotWin();

    bool _SimpleWin;
    bool _SecretWin;

    System.Collections.Generic.Dictionary<string, List<string>> _nameEntries;
    Random _rng;
    RandomNumberGenerator _fRng;
    string _mainExclude;
    private List<string> _freeSpaceEntries;

    public override void _Ready()
    {
        _rng = new Random();
        _fRng = new RandomNumberGenerator();
        _SimpleWin = false;
        _SecretWin = false;
        _nameEntries = new System.Collections.Generic.Dictionary<string, List<string>>();
        SetupHttpRequest();
        _gc = GetNode<GridContainer>("HBoxContainer/GridContainer");
        _usingFillButton = GetNode<CheckBox>("HBoxContainer/PanelContainer/VBoxContainer/UsingFill");
        _generateButton = GetNode<Button>("HBoxContainer/PanelContainer/VBoxContainer/Button");
        _generateButton.Connect("pressed", this, "GenerateBingo");

        _ratJammy = ResourceLoader.Load<PackedScene>("res://RatJammy.tscn");

        // generate dummy slots in the gridcontainer
        MakeEmptyBingoBoard();
        PopulateOptionButton();
    }

    private void SetupHttpRequest()
    {
        _httpRequest = new HTTPRequest();
        this.AddChild(_httpRequest);
        _httpRequest.Connect("request_completed", this, "BingoRequestCompleted");
        var e = _httpRequest.Request("https://raw.githubusercontent.com/willtsay/bingobango/main/bingoEntries.tsv");
        if (e != Error.Ok)
        {
            GD.Print("failed request"); // push?
        }
    }

    private void MakeEmptyBingoBoard()
    {
        var bingoButton = ResourceLoader.Load<PackedScene>("res://BingoButton.tscn");
        _bingoLabels = new List<Label>();
        _bingoButtons = new List<Button>();
        for (int i=0; i<25; i ++)
        {
            Button newButton = (Button)bingoButton.Instance();
            newButton.Connect("pressed", this, "CheckWinCondition");
            Label newLabel = newButton.GetChild<Label>(0);
            if (i == 12)
            {
                newLabel.Text = "FREE SPACE";
                newButton.Pressed = true;
            }
            _bingoLabels.Add(newLabel);
            _bingoButtons.Add(newButton);
            _gc.AddChild(newButton);
        }
        
        // make win condition checker;
        _winConditionChecker = new List<List<Button>>();

        // first 2 elements are reserved for the diagonals
        _winConditionChecker.Add(new List<Button>());
        _winConditionChecker.Add(new List<Button>());

        for (int i =0; i < 5; i++)
        {
            _winConditionChecker[0].Add(_bingoButtons[i*6]);
            _winConditionChecker[1].Add(_bingoButtons[20 - i*4]);
            List<int> horizontalRange = Enumerable.Range(0, 5).ToList();
            List<Button> rowList = new List<Button>();
            List<Button> colList = new List<Button>();
            horizontalRange.ForEach(x =>
            {
                rowList.Add(_bingoButtons[x + 5 * i]);
                colList.Add(_bingoButtons[x * 5 + i]);
            });
            _winConditionChecker.Add(rowList);
            _winConditionChecker.Add(colList);
        }
    }

    private void CheckWinCondition()
    {
        bool _SecretWinChecker = true;
        foreach (var btnList in _winConditionChecker)
        {
            bool isWin = true;
            foreach (var btn in btnList)
            {
                isWin = isWin && btn.Pressed;
                _SecretWinChecker = _SecretWinChecker && btn.Pressed;
            }
            if (isWin && _SimpleWin != true)
            {
                _SimpleWin = isWin;
            }
        }
        if (!_SimpleWin)
        {
            this.EmitSignal("NotWin");
        }
        _SecretWin = _SecretWinChecker;
    }

    public override void _Process(float delta)
    {
        if (_SimpleWin)
        {
            timeUntilRain -= delta;
            if (timeUntilRain <= 0) 
            {
                for (int i = 0; i < 10; i++)
                {
                    Timer ratTimer = new Timer();
                    ratTimer.WaitTime = _fRng.RandfRange(0.5f, 1.0f);
                    ratTimer.Connect("timeout", this, "spawnRatJammy", new Godot.Collections.Array(i, ratTimer));
                    ratTimer.OneShot = true;
                    this.AddChild(ratTimer);
                    ratTimer.Start();
                }
                timeUntilRain += rainDelay;
            }
        }
    }

    private void spawnRatJammy(int i, Timer ratTimer)
    {
        //pick a spot based on the RNG... i should be 1280/10 -> 128 pixels and then i guess shift it +/-  
        i -= 1;

        RatJammy newRatJammy = (RatJammy)_ratJammy.Instance();
        newRatJammy.Position += new Vector2(i * 128 + _rng.Next(-100, 100), 0);
        newRatJammy.Speed += _rng.Next(-50, 150);

        // as long as the direction is "down and to the right" 
        // a 45 degree angle is 1,1 so like i guess i can just do a random + random and then normalize the direction
        var dir = new Vector2(_fRng.Randf(), _fRng.Randf());
        newRatJammy.direction = dir.Normalized();
        this.Connect("NotWin", newRatJammy, "despawn");
        AnimatedSprite sprite = (AnimatedSprite)newRatJammy.GetChild(0);
        if (_SecretWin)
        {
            sprite.Play("ToxicSpin");
        } else
        {
            sprite.Play("default");
        }


        this.AddChild(newRatJammy);
    }


    private void PopulateOptionButton()
    {
        var ppls = Enum.GetValues(typeof (Ppl));
        _optionButton = GetNode<OptionButton>("HBoxContainer/PanelContainer/VBoxContainer/OptionButton");
        for (int i = 0; i < ppls.Length; i++)
        {
            _optionButton.AddItem(ppls.GetValue(i).ToString());
        }
    }

    private void BingoRequestCompleted(int result, int response_code, string[] headers, byte[] body)
    {
        var a = Encoding.UTF8.GetString(body);
        // populate the dictionary using this TSV.
        // 
        string[] splitArray = a.Split("\n");
        for (int i = 0; i < splitArray.Length; i++)
        {
            string[] nameEntries = splitArray[i].Split("\t");
            string name = nameEntries[0];
            List<string> entries = nameEntries.Skip(1).Where( ent => !string.IsNullOrWhiteSpace(ent)).ToList();
            _nameEntries.Add(name, entries);
        }
        GD.Print(_nameEntries.ContainsKey("free space"));
        _nameEntries.TryGetValue("free space", out _freeSpaceEntries);
        _nameEntries.Remove("free space");
    }

    private void GenerateBingo()
    {
        List<KeyValuePair<string, List<string>>> allowedEntries = null;

        // remove jinju specific OR kera specific from the pool.
        if (_usingFillButton.Pressed)
        {
            // special to keep the wloda/fluffles/jinju category available. 
            var illegalKeys = _nameEntries.Where(kvp => kvp.Key.Contains("jinju") && kvp.Key.Split(",").Length < 3).Select(kvp => kvp.Key).ToList();
            allowedEntries = _nameEntries.Where(kvp => !illegalKeys.Contains(kvp.Key)).ToList();
        }
        else
        {
            var illegalKeys = _nameEntries.Where(kvp => kvp.Key.Contains("kera") && kvp.Key.Split(",").Length < 3).Select(kvp => kvp.Key).ToList();
            allowedEntries = _nameEntries.Where(kvp => !illegalKeys.Contains(kvp.Key)).ToList();
        }

        // remove selected player from pool
        _mainExclude = _optionButton.GetItemText(_optionButton.Selected);
        if (_optionButton.Selected != -1)
        {
            // remove any kvp where the key contains the player's name. 
            var culledEntries = allowedEntries.Where(kvp => !kvp.Key.Contains(_mainExclude)).ToList();
            //var allowedEntries = _nameEntries.Where(kvp => !_excludes.Contains(kvp.Key)).ToList();
            var listEntries = new List<string>();
            culledEntries.ForEach(kvp => listEntries.AddRange(kvp.Value));

            for (int i = 0; i < listEntries.Count - 1; i++)
            {
                int j = _rng.Next(i, listEntries.Count);
                string temp = listEntries[i];
                listEntries[i] = listEntries[j];
                listEntries[j] = temp;
            }

            int entryIdx = 0;
            for (int i = 0; i < _bingoLabels.Count; i++)
            {
                if (i != 12)
                {
                    _bingoLabels[i].Text = listEntries[entryIdx];
                    Button parentButton = (Button) _bingoLabels[i].GetParent();
                    parentButton.Pressed = false;
                    entryIdx++;
                } else
                {
                    _bingoLabels[i].Text = _freeSpaceEntries[_rng.Next(0,_freeSpaceEntries.Count)]; 
                }
            }

        }

    }

    // make sure this matches the csv u plop up.. test commas i guess. 
    enum Ppl
    {
        mimi,
        mythos,
        will,
        jay,
        jinju,
        sheep,
        fluffles,
        wloda,
        kera
    }

}


