using System.Collections.Generic;
using UnityEngine;

public class DamagePopUpPool : MonoBehaviour
{
    [SerializeField] private DamagePopUpView prefab;
    [SerializeField] private Transform root;
    [SerializeField] private int initialSize = 10;

    private readonly Queue<DamagePopUpView> available = new();

    private void Awake()
    {
        for (int i = 0; i < initialSize; i++)
            available.Enqueue(CreateInstance());
    }

    public DamagePopUpView Get(Vector3 position)
    {
        DamagePopUpView popUp = available.Count > 0 ? available.Dequeue() : CreateInstance();

        popUp.transform.position = position;
        popUp.transform.position += new Vector3(Random.Range(-0.01f, 0.01f), 0f, Random.Range(-0.01f, 0.01f));
        popUp.gameObject.SetActive(true);
        popUp.Initialize(this);

        return popUp;
    }

    public void Release(DamagePopUpView popUp)
    {
        popUp.gameObject.SetActive(false);
        popUp.transform.SetParent(root);
        available.Enqueue(popUp);
    }

    private DamagePopUpView CreateInstance()
    {
        DamagePopUpView popUp = Instantiate(prefab, root);
        popUp.gameObject.SetActive(false);
        return popUp;
    }
}