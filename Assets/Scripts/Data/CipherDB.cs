using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CipherDB
{
    static Dictionary<string, CipherBase> ciphers;

    public static void Init()
    {
        ciphers = new Dictionary<string, CipherBase>();

        var cipherArray = Resources.LoadAll<CipherBase>("");
        foreach (var cipher in cipherArray)
        {
            if (ciphers.ContainsKey(cipher.Name))
            {
                Debug.LogError($"There are two ciphers with the same {cipher.Name}.");
                continue;
            }
            
            ciphers[cipher.Name] = cipher;
        }
    }

    public static CipherBase GetCipherByName(string name)
    {
        if (!ciphers.ContainsKey(name))
        {
            Debug.LogError($"Cipher with name {name} is not found in the database.");
            return null;
        }

        return ciphers[name];
    }
}
