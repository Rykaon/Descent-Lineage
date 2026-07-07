using System.Collections.Generic;
using UnityEngine;

public class CladePanelView : MonoBehaviour
{
    [SerializeField] private ClientGameContext context;
    [SerializeField] private Transform container;
    [SerializeField] private CladeEntryView entryPrefab;

    private readonly List<CladeEntryView> entries = new();

    public void Refresh(ClientCladeMirror mirror)
    {
        while (entries.Count < mirror.Entries.Count)
        {
            entries.Add(Instantiate(entryPrefab, container));
        }

        for (int i = 0; i < entries.Count; i++)
        {
            bool active = i < mirror.Entries.Count;
            entries[i].gameObject.SetActive(active);

            if (!active)
                continue;

            entries[i].Refresh(mirror.Entries[i], context.CladeDatabase);
        }
    }
}
