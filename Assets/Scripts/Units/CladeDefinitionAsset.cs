using UnityEngine;

[CreateAssetMenu(fileName = "CladeDefinitionAsset", menuName = "Scriptable Objects/CladeDefinitionAsset")]
public class CladeDefinitionAsset : ScriptableObject
{
    public string Id;
    public string DisplayName;
    public CladeTierDefinition[] Tiers;

    public CladeDefinition ToCore()
    {
        return new CladeDefinition
        {
            Id = Id,
            DisplayName = DisplayName,
            Tiers = Tiers
        };
    }
}
