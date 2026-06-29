using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "MutationDefinitionDatabaseAsset", menuName = "Scriptable Objects/MutationDefinitionDatabaseAsset")]
public class MutationDefinitionDatabaseAsset : ScriptableObject
{
    [SerializeField] private MutationDefinitionAsset[] mutationAssets;

    public IMutationDefinitionDatabase Build()
    {
        var definitions = mutationAssets.Select(asset => asset.ToCore());
        return new MutationDefinitionDatabase(definitions);
    }
}