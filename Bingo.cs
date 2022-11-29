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
    Button _generateButton;
    OptionButton _optionButton;
    CheckBox _usingFillButton;
    List<String> _excludes;
    System.Collections.Generic.Dictionary<string, List<string>> _nameEntries;
    Random _rng;

    public override void _Ready()
    {
        _rng = new Random();
        _nameEntries = new System.Collections.Generic.Dictionary<string, List<string>>();
        SetupHttpRequest();
        _gc = GetNode<GridContainer>("GridContainer");
        _usingFillButton = GetNode<CheckBox>("PanelContainer/VBoxContainer/UsingFill");
        _generateButton = GetNode<Button>("PanelContainer/VBoxContainer/Button");
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
        var e = _httpRequest.Request("https://raw.githubusercontent.com/willtsay/bingobango/main/bingoEntries2.csv");
        if (e != Error.Ok)
        {
            GD.Print("failed request"); // push?
        }
    }

    private void MakeEmptyBingoBoard()
    {
        var bingoButton = ResourceLoader.Load<PackedScene>("res://BingoButton.tscn");
        _bingoButtons = new List<Button>();
        for (int i=0; i<25; i ++)
        {
            Button newButton = (Button) bingoButton.Instance();
            if (i == 12)
            {
                newButton.Text = "FREE SPACE";
                newButton.Pressed = true;
            }
            _bingoButtons.Add(newButton);
            _gc.AddChild(newButton);
        }
    }

    private void PopulateOptionButton()
    {
        var ppls = Enum.GetValues(typeof (Ppl));
        _optionButton = GetNode<OptionButton>("PanelContainer/VBoxContainer/OptionButton");
        for (int i = 0; i < ppls.Length; i++)
        {
            _optionButton.AddItem(ppls.GetValue(i).ToString());
        }
    }

    private void BingoRequestCompleted(int result, int response_code, string[] headers, byte[] body)
    {
        var a = Encoding.UTF8.GetString(body);
        // populate the dictionary using this csv. 
        string[] splitArray = a.Split("\n");
        for (int i = 0; i < splitArray.Length; i++)
        {
            string[] nameEntries = splitArray[i].Split(",");
            string name = nameEntries[0];
            List<string> entries = nameEntries.Skip(1).Where( ent => !string.IsNullOrWhiteSpace(ent)).ToList();
            _nameEntries.Add(name, entries);
        }
    }

    private void GenerateBingo()
    {
        if (_usingFillButton.Pressed)
        {
            _excludes.Add(Ppl.jinju.ToString());
            _excludes.Remove(Ppl.kera.ToString());
        }
        else
        {
            _excludes.Add(Ppl.kera.ToString());
            _excludes.Remove(Ppl.jinju.ToString());

        }


        if (_optionButton.Selected != -1)
        {
            _excludes.Add(_optionButton.GetItemText(_optionButton.Selected));
            // how to evenly distribute the bingo properly to make sure you get an even spread
            // option: don't make it particularly even just have it fully random?
            // in the form give the following option - player or "any" make them do the wark.  
            // pick 24 options from the remaining (selection sampling) 

            var allowedEntries = _nameEntries.Where(kvp => !_excludes.Contains(kvp.Key)).ToList();
            var listEntries = new List<string>();
            allowedEntries.ForEach(kvp => listEntries.AddRange(kvp.Value));

            //while (pickedEntries.Count < 24 && listEntriesIdx < listEntries.Count)
            //{
            //    float target = (24 - pickedEntries.Count) / ((float) (listEntries.Count-listEntriesIdx));
            //    GD.Print(target);
            //    if (target >= _randomNumberGenerator.Randf())
            //    {
            //        pickedEntries.Add(listEntries[listEntriesIdx]);
            //    }
            //    listEntriesIdx++;
            //}
            for (int i = 0; i < listEntries.Count - 1; i++)
            {
                int j = _rng.Next(i, listEntries.Count);
                string temp = listEntries[i];
                listEntries[i] = listEntries[j];
                listEntries[j] = temp;
            }

            int entryIdx = 0;
            for (int i = 0; i < _bingoButtons.Count; i++)
            {
                if (i != 12)
                {
                    _bingoButtons[i].Text = (string)listEntries[entryIdx];
                    _bingoButtons[i].Pressed = false;
                    entryIdx++;
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


