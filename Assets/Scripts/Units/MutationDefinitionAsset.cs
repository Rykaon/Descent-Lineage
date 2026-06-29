using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MutationDefinitionAsset", menuName = "Scriptable Objects/MutationDefinitionAsset")]
public sealed class MutationDefinitionAsset : ScriptableObject
{
    public string Id;
    public string DisplayName;

    public List<BiomeType> EligibleBiomes = new();

    public List<string> GrantedKeywordIds = new();

    public string Description;

    public MutationDefinition ToCore()
    {
        return new MutationDefinition
        {
            Id = Id,
            DisplayName = DisplayName,
            EligibleBiomes = EligibleBiomes,
            GrantedKeywordIds = GrantedKeywordIds,
            Description = Description,
        };
    }
}