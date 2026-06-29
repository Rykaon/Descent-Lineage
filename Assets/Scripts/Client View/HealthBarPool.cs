using System.Collections.Generic;
using UnityEngine;

public class HealthBarPool : MonoBehaviour
{
    [SerializeField] private HealthBarView prefab;
    [SerializeField] private Transform root;
    [SerializeField] private int initialSize = 60;

    private readonly Queue<HealthBarView> available = new();

    private void Awake()
    {
        for (int i = 0; i < initialSize; i++)
        {
            available.Enqueue(CreateInstance());
        }
    }

    public HealthBarView Get(Vector3 position)
    {
        HealthBarView healthBar = available.Count > 0 ? available.Dequeue() : CreateInstance();

        healthBar.transform.position = position;
        healthBar.gameObject.SetActive(true);
        healthBar.Initialize(this);

        return healthBar;
    }

    public void Release(HealthBarView healthBar)
    {
        healthBar.gameObject.SetActive(false);
        healthBar.transform.SetParent(root);
        available.Enqueue(healthBar);
    }

    private HealthBarView CreateInstance()
    {
        HealthBarView healthBar = Instantiate(prefab, root);
        healthBar.gameObject.SetActive(false);
        return healthBar;
    }
}
