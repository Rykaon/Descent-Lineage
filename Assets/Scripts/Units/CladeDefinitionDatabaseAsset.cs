using UnityEngine;

[CreateAssetMenu(fileName = "CladeDefinitionDatabaseAsset", menuName = "Scriptable Objects/CladeDefinitionDatabaseAsset")]
public class CladeDefinitionDatabaseAsset : ScriptableObject
{
    [SerializeField] private CladeDefinitionAsset[] clades;

    public ICladeDefinitionDatabase Build()
    {
        return new CladeDefinitionDatabase(clades);
    }
}
