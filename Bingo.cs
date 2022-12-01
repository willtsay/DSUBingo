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
    List<Label> _bingoLabels;
    Button _generateButton;
    OptionButton _optionButton;
    CheckBox _usingFillButton;
    List<String> _excludes;
    System.Collections.Generic.Dictionary<string, List<string>> _nameEntriesJinju;
    System.Collections.Generic.Dictionary<string, List<string>> _nameEntriesKera;
    System.Collections.Generic.Dictionary<string, List<string>> _nameEntries;
    Random _rng;
    string _mainExclude;
    private List<string> _freeSpaceEntries;

    public override void _Ready()
    {
        _rng = new Random();
        _nameEntries = new System.Collections.Generic.Dictionary<string, List<string>>();
        SetupHttpRequest();
        _gc = GetNode<GridContainer>("HBoxContainer/GridContainer");
        _usingFillButton = GetNode<CheckBox>("HBoxContainer/PanelContainer/VBoxContainer/UsingFill");
        _generateButton = GetNode<Button>("HBoxContainer/PanelContainer/VBoxContainer/Button");
        _generateButton.Connect("pressed", this, "GenerateBingo");

        _excludes = new List<String>();
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
        for (int i=0; i<25; i ++)
        {
            Button newButton = (Button) bingoButton.Instance();
            Label newLabel = newButton.GetChild<Label>(0);
            if (i == 12)
            {
                newLabel.Text = "FREE SPACE";
                newButton.Pressed = true;
            }
            _bingoLabels.Add(newLabel);
            _gc.AddChild(newButton);
        }
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


