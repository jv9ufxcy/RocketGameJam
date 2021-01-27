﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Core Data", menuName = "Character Action/Core Data", order = 1)]
[System.Serializable]
public class CoreData : ScriptableObject
{
    //Character States
    public List<CharacterState> characterStates;

    public List<CharacterScript> characterScripts;

    //public List<CommandState> commandStates;

    public List<MoveList> moveLists;


    //public List<InputCommand> commands;

    public List<GameObject> globalPrefabs;
    //Save Files

    //Raw Inputs
    public List<RawInput> rawInputs, ps4Inputs, xboxInputs, keyboardInputs;
    public List<MotionCommand> motionCommands;
    public int currentMovelistIndex;

    public string[] GetScriptNames()
    {
        string[] _names = new string[characterScripts.Count];
        for (int i = 0; i < _names.Length; i++)
        {
            _names[i] = characterScripts[i].name;
        }
        return _names;
    }

    public string[] GetStateNames()
    {
        string[] _names = new string[characterStates.Count];
        for (int i = 0; i < _names.Length; i++)
        {
            _names[i] = characterStates[i].stateName;
        }
        return _names;
    }

    public string[] GetPrefabNames()
    {
        string[] _names = new string[globalPrefabs.Count];
        for (int i = 0; i < _names.Length; i++)
        {
            _names[i] = globalPrefabs[i].name;
        }
        return _names;
    }

    public string[] GetRawInputNames()
    {
        string[] _names = new string[rawInputs.Count];
        for (int i = 0; i < _names.Length; i++)
        {
            _names[i] = rawInputs[i].name;
        }
        return _names;
    }
    public string[] GetPs4InputNames()
    {
        string[] _names = new string[rawInputs.Count];
        for (int i = 0; i < _names.Length; i++)
        {
            _names[i] = ps4Inputs[i].name;
        }
        return _names;
    }
    public string[] GetXboxInputNames()
    {
        string[] _names = new string[rawInputs.Count];
        for (int i = 0; i < _names.Length; i++)
        {
            _names[i] = xboxInputs[i].name;
        }
        return _names;
    }
    public string[] GetKeyboardInputNames()
    {
        string[] _names = new string[rawInputs.Count];
        for (int i = 0; i < _names.Length; i++)
        {
            _names[i] = keyboardInputs[i].name;
        }
        return _names;
    }

    public string[] GetMotionCommandNames()
    {
        string[] _names = new string[motionCommands.Count];
        for (int i = 0; i < _names.Length; i++)
        {
            _names[i] = motionCommands[i].name;
        }
        return _names;
    }

    public string[] GetFollowUpNames(int _commandState, bool _deleteField)
    {
        int nameCount = moveLists[currentMovelistIndex].commandStates[_commandState].commandSteps.Count;
        if (_deleteField) { nameCount += 2; }
        string[] _names = new string[nameCount];
        for (int i = 0; i < _names.Length; i++)
        {
            if (i < _names.Length - 2)
            {
                _names[i] = moveLists[currentMovelistIndex].commandStates[_commandState].commandSteps[i].idIndex.ToString();
            }
            else if (i < _names.Length - 1)
            {
                _names[i] = "+ ADD";
            }
            else
            {
                _names[i] = "- DELETE";
            }
        }

        return _names;
    }


    public string[] GetCommandStateNames()
    {
        string[] _names = new string[moveLists[currentMovelistIndex].commandStates.Count];
        for (int i = 0; i < _names.Length; i++)
        {
            _names[i] = moveLists[currentMovelistIndex].commandStates[i].stateName.ToString();
        }
        return _names;
    }

    public string[] GetMovelistNames()
    {
        string[] _names = new string[moveLists.Count];
        for (int i = 0; i < _names.Length; i++)
        {
            _names[i] = moveLists[i].name.ToString();
        }
        return _names;
    }
}
