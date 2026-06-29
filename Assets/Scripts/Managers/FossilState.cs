using System.Collections.Generic;
using UnityEngine;

public class FossilState
{
    public int FossilValue;
    public int Level;
    public Dictionary<string, int> Mutations;

    public void Initialize()
    {
        FossilValue = 0;
        Level = 1;
        Mutations = new Dictionary<string, int>();

        Mutations["Essaim"] = 1;
        Mutations["Mucus"] = 1;
        Mutations["Mue"] = 1;
    }

    public void AddFossilValue(int value)
    {
        FossilValue += value;
    }
}