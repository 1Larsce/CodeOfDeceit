using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConditionsDB
{
    public static void Init()
    {
        foreach (var kvp in Conditions)
        {
            var conditionId = kvp.Key;
            var condition = kvp.Value;

            condition.Id = conditionId;
        }
    }
    public static Dictionary<ConditionID, Condition> Conditions { get; set; } = new Dictionary<ConditionID, Condition>()
    {
        {
            ConditionID.psn,
            new Condition()
            {
                Name = "Poison",
                StartMessage = "has been poisoned.",
                OnAfterTurn = (Cipher cipher) =>
                {
                    cipher.UpdateHP(cipher.MaxHp / 8);
                    cipher.StatusChanges.Enqueue($"{cipher.Base.Name} hurt itself due to poison.");
                }
            }
        },
        {
            ConditionID.brn,
            new Condition()
            {
                Name = "Burn",
                StartMessage = "has been burned. Ouch.",
                OnAfterTurn = (Cipher cipher) =>
                {
                    cipher.UpdateHP(cipher.MaxHp / 16);
                    cipher.StatusChanges.Enqueue($"{cipher.Base.Name} hurt itself due to burn.");
                }
            }
        },
        {
            ConditionID.par,
            new Condition()
            {
                Name = "Paralyze",
                StartMessage = "has been paralyzed. Oof.",
                OnBeforeMove = (Cipher cipher) =>
                {
                    if (Random.Range(1, 5) == 1)
                    {
                        cipher.StatusChanges.Enqueue($"{cipher.Base.Name}'s paralyzed and can't move.");
                        return false;
                    }

                    return true;
                }
            }
        },
        {
            ConditionID.frz,
            new Condition()
            {
                Name = "Freeze",
                StartMessage = "has been frozen. So cold.",
                OnBeforeMove = (Cipher cipher) =>
                {
                    if (Random.Range(1, 5) == 1)
                    {
                        cipher.CureStatus();
                        cipher.StatusChanges.Enqueue($"{cipher.Base.Name}'s is not frozen anymore.");
                        return true;
                    }

                    return false;
                }
            }
        },
        {
            ConditionID.slp,
            new Condition()
            {
                Name = "Sleep",
                StartMessage = "has fallen asleep. Zzz.",
                OnStart = (Cipher cipher) =>
                {
                    // Sleep for 1-3 turns
                    cipher.StatusTime = Random.Range(1, 4);
                    Debug.Log($"Will be asleep for {cipher.StatusTime} moves.");
                },
                OnBeforeMove = (Cipher cipher) =>
                {
                    if (cipher.StatusTime <= 0)
                    {
                        cipher.CureStatus();
                        cipher.StatusChanges.Enqueue($"{cipher.Base.Name} woke up!");
                        return true;
                    }

                    cipher.StatusTime--;
                    cipher.StatusChanges.Enqueue($"{cipher.Base.Name} is sleeping");
                    return false;
                }
            }
        },

        // Volatile Status Conditions
        {
            ConditionID.confusion,
            new Condition()
            {
                Name = "Confusion",
                StartMessage = "has been confused",
                OnStart = (Cipher cipher) =>
                {
                    //Confused for 1 - 4 turns
                    cipher.VolatileStatusTime = Random.Range(1, 5);
                    Debug.Log($"Will be confused for {cipher.VolatileStatusTime} moves.");
                },
                OnBeforeMove = (Cipher cipher) =>
                {
                    if (cipher.VolatileStatusTime <= 0)
                    {
                        cipher.CureVolatileStatus();
                        cipher.StatusChanges.Enqueue($"{cipher.Base.Name} kicked out of confusion!");
                        return true;
                    }
                    cipher.VolatileStatusTime--;

                    // 50% chance to do a move
                    if (Random.Range(1, 3) == 1)
                        return true;

                    // Hurt by confusion
                    cipher.StatusChanges.Enqueue($"{cipher.Base.Name} is confused.");
                    cipher.UpdateHP(cipher.MaxHp / 8);
                    cipher.StatusChanges.Enqueue($"It hurt itself due to confusion. Lol.");
                    return false;
                }
            }
        }
    };

    public static float GetStatusBonus(Condition condition)
    {
        if (condition == null)
            return 1f;
        else if (condition.Id == ConditionID.slp || condition.Id == ConditionID.frz)
            return 2f;
        else if (condition.Id == ConditionID.par || condition.Id == ConditionID.psn || condition.Id == ConditionID.brn)
            return 1.5f;

        return 1f;
    }
}

public enum ConditionID
{
    none, psn, brn, slp, par, frz,
    confusion
}
