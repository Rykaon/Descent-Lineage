using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "UnitDefinitionDatabaseAsset", menuName = "Scriptable Objects/UnitDefinitionDatabaseAsset")]
public class UnitDefinitionDatabaseAsset : ScriptableObject
{
    [SerializeField] private UnitDefinitionAsset[] unitAssets;

    public IUnitDefinitionDatabase Build()
    {
        var definitions = unitAssets.Select(asset => asset.ToCore());
        return new UnitDefinitionDatabase(definitions);
    }
}