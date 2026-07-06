using UnityEngine;

public class BoardBiomeView : MonoBehaviour
{
    public BiomeType biomeType;
    private Renderer biomeRenderer;

    [Header("Materials")]
    [SerializeField] private Material defaultBoardMaterial;
    [SerializeField] private Material forestMaterial;
    [SerializeField] private Material swampMaterial;
    [SerializeField] private Material savannaMaterial;
    [SerializeField] private Material coastMaterial;
    [SerializeField] private Material mountainMaterial;

    [Header("Prefabs")]
    [SerializeField] public GameObject ressourcePrefab;

    private Material GetMaterial()
    {
        return biomeType switch
        {
            BiomeType.Forest => forestMaterial,
            BiomeType.Swamp => swampMaterial,
            BiomeType.Savanna => savannaMaterial,
            BiomeType.Coast => coastMaterial,
            BiomeType.Mountain => mountainMaterial,
            BiomeType.None => defaultBoardMaterial,
            _ => defaultBoardMaterial
        };
    }

    private void Start()
    {
        biomeRenderer = transform.GetChild(0).GetComponent<Renderer>();
        biomeRenderer.material = GetMaterial();
    }

    public BoardBiomeRessourceView CreateRessourceBiome()
    {
        GameObject go = Instantiate(ressourcePrefab, transform.position, Quaternion.identity);
        go.GetComponent<Renderer>().material = biomeRenderer.material;
        go.GetComponent<BoardBiomeRessourceView>().biomeType = biomeType;

        return go.GetComponent<BoardBiomeRessourceView>();
    }
}
