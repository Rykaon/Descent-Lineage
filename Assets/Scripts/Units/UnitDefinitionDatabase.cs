using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitDefinitionDatabase : IUnitDefinitionDatabase
{
    public readonly Dictionary<string, UnitDefinition> units;

    public UnitDefinitionDatabase(IEnumerable<UnitDefinition> definitions)
    {
        units = definitions.ToDictionary(unit => unit.Id);
    }

    public UnitDefinition GetUnit(string id)
    {
        return units[id];
    }

    public bool TryGetUnit(string id, out UnitDefinition unit)
    {
        return units.TryGetValue(id, out unit);
    }

    public IEnumerable<UnitDefinition> GetAllUnits()
    {
        return units.Values;
    }
}

public class MutationDefinitionDatabase : IMutationDefinitionDatabase
{
    public readonly Dictionary<string, MutationDefinition> mutations;

    public MutationDefinitionDatabase(IEnumerable<MutationDefinition> definitions)
    {
        mutations = definitions.ToDictionary(mutation => mutation.Id);
    }

    public MutationDefinition GetMutation(string id)
    {
        return mutations[id];
    }

    public bool TryGetMutation(string id, out MutationDefinition mutation)
    {
        return mutations.TryGetValue(id, out mutation);
    }

    public IEnumerable<MutationDefinition> GetAllMutations()
    {
        return mutations.Values;
    }
}

public class FaunaDefinitionDatabase : IFaunaDefinitionDatabase
{
    public readonly Dictionary<string, FaunaDefinition> faunas;

    public FaunaDefinitionDatabase(IEnumerable<FaunaDefinition> definitions)
    {
        faunas = definitions.ToDictionary(fauna => fauna.Id);
    }

    public FaunaDefinition GetFauna(string id)
    {
        return faunas[id];
    }

    public bool TryGetFauna(string id, out FaunaDefinition fauna)
    {
        return faunas.TryGetValue(id, out fauna);
    }

    public IEnumerable<FaunaDefinition> GetAllFaunas()
    {
        return faunas.Values;
    }
}

public class CladeDefinitionDatabase : ICladeDefinitionDatabase
{
    private readonly Dictionary<string, CladeDefinition> byId = new();

    public CladeDefinitionDatabase(CladeDefinitionAsset[] assets)
    {
        foreach (var asset in assets)
        {
            var definition = asset.ToCore();
            byId[definition.Id] = definition;
        }
    }

    public CladeDefinition GetClade(string id)
    {
        byId.TryGetValue(id, out var definition);
        return definition;
    }

    public CladeDefinition[] GetAllClades()
    {
        var result = new CladeDefinition[byId.Count];
        byId.Values.CopyTo(result, 0);
        return result;
    }
}