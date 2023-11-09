using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapArea : MonoBehaviour
{
    [SerializeField] List<Cipher> wildCiphers;

    public Cipher GetRandomWildCipher()
    {
        var wildCipher = wildCiphers[Random.Range(0, wildCiphers.Count)];
        wildCipher.Init();
        return wildCipher;
    }
}
