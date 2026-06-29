using System.Collections.Generic;
using UnityEngine;

public interface IUnitDefinitionDatabase
{
    UnitDefinition GetUnit(string id);
    bool TryGetUnit(string id, out UnitDefinition unit);
    IEnumerable<UnitDefinition> GetAllUnits();
}

public interface IMutationDefinitionDatabase
{
    MutationDefinition GetMutation(string id);
    bool TryGetMutation(string id, out MutationDefinition mutation);
    IEnumerable<MutationDefinition> GetAllMutations();
}

public interface IFaunaDefinitionDatabase
{
    FaunaDefinition GetFauna(string id);
    bool TryGetFauna(string id, out FaunaDefinition mutation);
    IEnumerable<FaunaDefinition> GetAllFaunas();
}