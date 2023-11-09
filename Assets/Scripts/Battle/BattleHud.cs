using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleHud : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] Text statusText;
    [SerializeField] HPBar hpBar;
    [SerializeField] GameObject expBar;

    [SerializeField] Color psnColor;
    [SerializeField] Color brnColor;
    [SerializeField] Color slpColor;
    [SerializeField] Color parColor;
    [SerializeField] Color frzColor;

    Cipher _cipher;
    Dictionary<ConditionID, Color> statusColors;

    public void SetData(Cipher cipher)
    {
        _cipher = cipher;

        nameText.text = cipher.Base.Name;
        SetLevel();
        hpBar.SetHP((float) cipher.HP / cipher.MaxHp);
        SetExp();

        statusColors = new Dictionary<ConditionID, Color>()
        {
            {ConditionID.psn, psnColor },
            {ConditionID.brn, brnColor },
            {ConditionID.slp, slpColor },
            {ConditionID.par, parColor },
            {ConditionID.frz, frzColor },
        };

        SetStatusText();
        _cipher.OnStatusChanged += SetStatusText;
    }

    void SetStatusText()
    {
        if (_cipher.Status == null)
        {
            statusText.text = "";
        } 
        else
        {
            statusText.text = _cipher.Status.Id.ToString().ToUpper();
            statusText.color = statusColors[_cipher.Status.Id];
        }
    }

    public void SetLevel()
    {
        levelText.text = "Lvl " + _cipher.Level;
    }

    public void SetExp()
    {
        if (expBar == null) return;

        float normalizedExp = GetNormalizedExp();
        expBar.transform.localScale = new Vector3(normalizedExp, 1, 1);
    }

    public IEnumerator SetExpSmooth(bool reset=false)
    {
        if (expBar == null) yield break;

        if (reset)
            expBar.transform.localScale = new Vector3(0, 1, 1);

        float normalizedExp = GetNormalizedExp();
        yield return expBar.transform.DOScaleX(normalizedExp, 1.5f).WaitForCompletion();
    }

    float GetNormalizedExp()
    {
        int currLevelExp = _cipher.Base.GetExpForLevel(_cipher.Level);
        int nextLevelExp = _cipher.Base.GetExpForLevel(_cipher.Level + 1);

        float normalizedExp = (float) (_cipher.Exp - currLevelExp) / (nextLevelExp - currLevelExp);
        return Mathf.Clamp01(normalizedExp);
    }

    public IEnumerator UpdateHP()
    {
        if (_cipher.HpChanged)
        {
            yield return hpBar.SetHPSmooth((float)_cipher.HP / _cipher.MaxHp);
            _cipher.HpChanged = false;
        }
    }
}
