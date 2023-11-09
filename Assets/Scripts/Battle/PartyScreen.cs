using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyScreen : MonoBehaviour
{
    [SerializeField] Text messageText; 

    PartyMemberUI[] memberSlots;
    List<Cipher> ciphers;

    public void Init()
    {
        memberSlots = GetComponentsInChildren<PartyMemberUI>(true);
    }

    public void SetPartyData(List<Cipher> ciphers)
    {
        this.ciphers = ciphers;

        for (int i = 0; i < memberSlots.Length; i++)
        {
            if (i < ciphers.Count)
            {
                memberSlots[i].gameObject.SetActive(true);
                memberSlots[i].SetData(ciphers[i]);
            }
            else
                memberSlots[i].gameObject.SetActive(false);
        }

        messageText.text = "Choose a Cipher";
    }

    public void UpdateMemberSelection(int selectedMember)
    {
        for (int i = 0; i < ciphers.Count; i++)
        {
            if (i == selectedMember)
                memberSlots[i].SetSelected(true);
            else
                memberSlots[i].SetSelected(false);
        }
    }

    public void SetMessageText(string message)
    {
        messageText.text = message;
    }
}
