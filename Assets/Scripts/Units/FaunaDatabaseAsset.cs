using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "FaunaDefinitionDatabaseAsset", menuName = "Scriptable Objects/FaunaDefinitionDatabaseAsset")]
public class FaunaDefinitionDatabaseAsset : ScriptableObject
{
    [SerializeField] private FaunaDefinitionAsset[] faunaAssets;

    public IFaunaDefinitionDatabase Build()
    {
        var definitions = faunaAssets.Select(asset => asset.ToCore());
        return new FaunaDefinitionDatabase(definitions);
    }
}