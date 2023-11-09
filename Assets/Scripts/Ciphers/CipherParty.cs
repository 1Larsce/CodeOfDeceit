using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CipherParty : MonoBehaviour
{
    [SerializeField] List<Cipher> ciphers;

    public List<Cipher> Ciphers
    {
        get
        {
            return ciphers;
        }
        set
        {
            ciphers = value;
        }
    }

    private void Start()
    {
        foreach (var cipher in ciphers)
        {
            cipher.Init();
        }
    }

    public Cipher GetHealthyCipher()
    {
        return ciphers.Where(x => x.HP > 0).FirstOrDefault();
    }

    public void AddCipher(Cipher newCipher)
    {
        if (ciphers.Count < 6)
        {
            ciphers.Add(newCipher);
        }
        else
        {
            // TODO: Add to the PC once that's implemented
        }
    }
}
