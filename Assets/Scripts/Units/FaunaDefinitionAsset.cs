using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FaunaDefinitionAsset", menuName = "Scriptable Objects/FaunaDefinitionAsset")]
public sealed class FaunaDefinitionAsset : ScriptableObject
{
    public string Id;
    public string DisplayName;
    public int FossilValue;
    public List<BiomeType> EligibleBiomes = new();
    public List<MutationDefinitionAsset> EligibleMutations = new();
    public int Cost;

    public int MaxPoolCount;

    public FaunaDefinition ToCore()
    {
        List<MutationDefinition> mutations = new();

        foreach (var mutation in EligibleMutations)
        {
            mutations.Add(mutation.ToCore());
        }

        return new FaunaDefinition
        {
            Id = Id,
            DisplayName = DisplayName,
            EligibleBiomes = EligibleBiomes,
            FossilValue = FossilValue,
            Cost = Cost,
            EligibleMutations = mutations,
            MaxPoolCount = MaxPoolCount
        };
    }
}