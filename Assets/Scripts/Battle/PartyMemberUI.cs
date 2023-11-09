using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyMemberUI : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] HPBar hpBar;

    [SerializeField] Color highlightedColor;

    Cipher _cipher;

    public void SetData(Cipher cipher)
    {
        _cipher = cipher;

        nameText.text = cipher.Base.Name;
        levelText.text = "Lvl " + cipher.Level;
        hpBar.SetHP((float)cipher.HP / cipher.MaxHp);
    }

    public void SetSelected(bool selected)
    {
        if (selected)
            nameText.color = highlightedColor;
        else
            nameText.color = Color.black;
    }
}
