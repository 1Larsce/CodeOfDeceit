using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum BattleState { Start, ActionSelection, MoveSelection, RunningTurn, Busy, PartyScreen, AboutToUse, MoveToForget, BattleOver}
public enum BattleAction { Move, SwitchCipher, UseItem, Run }

public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] BattleDialogBox dialogBox;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] Image playerImage;
    [SerializeField] Image trainerImage;
    [SerializeField] GameObject cipherballSprite;
    [SerializeField] MoveSelectionUI moveSelectionUI;

    public event Action<bool> OnBattleOver;

    BattleState state;
    BattleState? prevState;
    int currentAction;
    int currentMove;
    int currentMember;
    bool aboutToUseChoice = true;

    CipherParty playerParty;
    CipherParty trainerParty;
    Cipher wildCipher;

    bool isTrainerBattle = false;
    PlayerController player;
    TrainerController trainer;

    int escapeAttempts;
    MoveBase moveToLearn;

    public void StartBattle(CipherParty playerParty, Cipher wildCipher)
    {
        this.playerParty = playerParty;
        this.wildCipher = wildCipher;
        player = playerParty.GetComponent<PlayerController>();
        isTrainerBattle = false;

        StartCoroutine(SetupBattle());
    }

    public void StartTrainerBattle(CipherParty playerParty, CipherParty trainerParty)
    {
        this.playerParty = playerParty;
        this.trainerParty = trainerParty;

        isTrainerBattle = true;
        player = playerParty.GetComponent<PlayerController>();
        trainer = trainerParty.GetComponent<TrainerController>();

        StartCoroutine(SetupBattle());
    }

    public IEnumerator SetupBattle()
    {
        playerUnit.Clear();
        enemyUnit.Clear();

        if (!isTrainerBattle)
        {
            // Wild Pokemon Battle 
            playerUnit.Setup(playerParty.GetHealthyCipher());
            enemyUnit.Setup(wildCipher);

            dialogBox.SetMoveNames(playerUnit.Cipher.Moves);
            yield return dialogBox.TypeDialog($"A wild {enemyUnit.Cipher.Base.Name} appeared.");
        }
        else
        {
            // Trainer Battle

            // Show Trainer and player sprites
            playerUnit.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(false);

            playerImage.gameObject.SetActive(true);
            trainerImage.gameObject.SetActive(true);
            playerImage.sprite = player.Sprite;
            trainerImage.sprite = trainer.Sprite;

            yield return dialogBox.TypeDialog($"{trainer.Name} wants to battle.");

            // Send out first cipher of the trainer
            trainerImage.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(true);
            var enemyCipher = trainerParty.GetHealthyCipher();
            enemyUnit.Setup(enemyCipher);
            yield return dialogBox.TypeDialog($"{trainer.Name} send out {enemyCipher.Base.Name}");

            // Send out first cipher of the player
            playerImage.gameObject.SetActive(false);
            playerUnit.gameObject.SetActive(true);
            var playerCipher = playerParty.GetHealthyCipher();
            playerUnit.Setup(playerCipher);
            yield return dialogBox.TypeDialog($"Go {playerCipher.Base.Name}!");
            dialogBox.SetMoveNames(playerUnit.Cipher.Moves);
        }

        escapeAttempts = 0; 
        partyScreen.Init();
        ActionSelection();
    }

    void BattleOver(bool won)
    {
        state = BattleState.BattleOver;
        playerParty.Ciphers.ForEach(c => c.OnBattleOver());
        OnBattleOver(won);
    }


    void ActionSelection()
    {
        state = BattleState.ActionSelection;
        dialogBox.SetDialog("Choose an action");
        dialogBox.EnableActionSelector(true);
    }

    void OpenPartyScreen()
    {
        state = BattleState.PartyScreen;
        partyScreen.SetPartyData(playerParty.Ciphers);
        partyScreen.gameObject.SetActive(true);
    }

    void MoveSelection()
    {
        state = BattleState.MoveSelection;
        dialogBox.EnableActionSelector(false);
        dialogBox.EnableDialogText(false);
        dialogBox.EnableMoveSelector(true);
    }

    IEnumerator AboutToUse(Cipher newCipher)
    {
        state = BattleState.Busy;
        yield return dialogBox.TypeDialog($"{trainer.Name} is about to use {newCipher.Base.Name}. Do you want to change ciphers?");

        state = BattleState.AboutToUse;
        dialogBox.EnableChoiceBox(true);
    }

    IEnumerator ChooseMoveToForget(Cipher cipher, MoveBase newMove)
    {
        state = BattleState.Busy;
        yield return dialogBox.TypeDialog($"Choose a cipher move that you want to forget");
        moveSelectionUI.gameObject.SetActive(true);
        moveSelectionUI.SetMoveData(cipher.Moves.Select(x => x.Base).ToList(), newMove);
        moveToLearn = newMove;

        state = BattleState.MoveToForget;
    }

    IEnumerator RunTurns(BattleAction playerAction)
    {
        state = BattleState.RunningTurn;

        if (playerAction == BattleAction.Move)
        {
            playerUnit.Cipher.CurrentMove = playerUnit.Cipher.Moves[currentMove];
            enemyUnit.Cipher.CurrentMove = enemyUnit.Cipher.GetRandomMove();

            int playerMovePriority = playerUnit.Cipher.CurrentMove.Base.Priority;
            int enemyMovePriority = enemyUnit.Cipher.CurrentMove.Base.Priority;

            // Check who goes first
            bool playerGoesFirst = true;
            if (enemyMovePriority > playerMovePriority)
                playerGoesFirst = false;
            else if (enemyMovePriority == playerMovePriority)
                playerGoesFirst = playerUnit.Cipher.Speed > enemyUnit.Cipher.Speed;

            var firstUnit = (playerGoesFirst) ? playerUnit : enemyUnit;
            var secondUnit = (playerGoesFirst) ? enemyUnit : playerUnit;

            var secondCipher = secondUnit.Cipher;

            // First Turn
            yield return RunMove(firstUnit, secondUnit, firstUnit.Cipher.CurrentMove);
            yield return RunAfterTurn(firstUnit);
            if (state == BattleState.BattleOver) yield break;

            if (secondCipher.HP > 0)
            {
                // Second Turn
                yield return RunMove(secondUnit, firstUnit, secondUnit.Cipher.CurrentMove);
                yield return RunAfterTurn(secondUnit);
                if (state == BattleState.BattleOver) yield break;
            }
        }
        else
        {
            if (playerAction == BattleAction.SwitchCipher)
            {
                var selectedCipher = playerParty.Ciphers[currentMember];
                state = BattleState.Busy;
                yield return SwitchCipher(selectedCipher);
            }
            else if (playerAction == BattleAction.UseItem)
            {
                dialogBox.EnableActionSelector(false);
                yield return ThrowCipherball();
            }
            else if (playerAction == BattleAction.Run)
            {
                yield return TryToEscape();
            }

            // Enemy Turn
            var enemyMove = enemyUnit.Cipher.GetRandomMove();
            yield return RunMove(enemyUnit, playerUnit, enemyMove);
            yield return RunAfterTurn(enemyUnit);
            if (state == BattleState.BattleOver) yield break;
        }

        if (state != BattleState.BattleOver)
            ActionSelection();
    }

    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move)
    {
        bool canRunMove = sourceUnit.Cipher.OnBeforeMove();
        if (!canRunMove)
        {
            yield return ShowStatusChanges(sourceUnit.Cipher);
            yield return sourceUnit.Hud.UpdateHP();
            yield break;
        }
        yield return ShowStatusChanges(sourceUnit.Cipher);

        move.PP--;
        yield return dialogBox.TypeDialog($"{sourceUnit.Cipher.Base.Name} used {move.Base.Name}!");

        if (CheckIfMoveHits(move, sourceUnit.Cipher, targetUnit.Cipher))
        {
            sourceUnit.PlayAttackAnimation();
            yield return new WaitForSeconds(1f);
            targetUnit.PlayHitAnimation();

            if (move.Base.Category == MoveCategory.Status)
            {
                yield return RunMoveEffects(move.Base.Effects, sourceUnit.Cipher, targetUnit.Cipher, move.Base.Target);
            }
            else
            {
                var damageDetails = targetUnit.Cipher.TakeDamage(move, sourceUnit.Cipher);
                yield return targetUnit.Hud.UpdateHP();
                yield return ShowDamageDetails(damageDetails);
            }

            if (move.Base.Secondaries != null && move.Base.Secondaries.Count > 0 && targetUnit.Cipher.HP > 0)
            {
                foreach (var secondary in move.Base.Secondaries)
                {
                    var rnd = UnityEngine.Random.Range(1, 101);
                    if (rnd <= secondary.Chance)
                        yield return RunMoveEffects(secondary, sourceUnit.Cipher, targetUnit.Cipher, secondary.Target);
                }
            }

            if (targetUnit.Cipher.HP <= 0)
            {
                yield return HandleCipherFainted(targetUnit);
            }
           
        }
        else
        {
            yield return dialogBox.TypeDialog($"{sourceUnit.Cipher.Base.Name}'s attack missed.");
        }
    }

    IEnumerator RunMoveEffects(MoveEffects effects, Cipher source, Cipher target, MoveTarget moveTarget)
    {
        // Stat Boosting
        if (effects.Boosts != null)
        {
            if (moveTarget == MoveTarget.Self)
                source.ApplyBoosts(effects.Boosts);
            else
                target.ApplyBoosts(effects.Boosts);
        }

        // Status Condition
        if (effects.Status != ConditionID.none)
        {
            target.SetStatus(effects.Status);
        }

        // Volatile Status Condition
        if (effects.VolatileStatus != ConditionID.none)
        {
            target.SetVolatileStatus(effects.VolatileStatus);
        }

        yield return ShowStatusChanges(source);
        yield return ShowStatusChanges(target);
    }

    IEnumerator RunAfterTurn(BattleUnit sourceUnit)
    {
        if (state == BattleState.BattleOver) yield break;
        yield return new WaitUntil(() => state == BattleState.RunningTurn);

        // Statuses like burn or psn will hurt the cipher after the turn
        sourceUnit.Cipher.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Cipher);
        yield return sourceUnit.Hud.UpdateHP();
        if (sourceUnit.Cipher.HP <= 0)
        {
            yield return HandleCipherFainted(sourceUnit);
            yield return new WaitUntil(() => state == BattleState.RunningTurn);
        }
    }

    bool CheckIfMoveHits(Move move, Cipher source, Cipher target)
    {
        if (move.Base.AlwaysHits)
            return true;

        float moveAccuracy = move.Base.Accuracy;

        int accuracy = source.StatBoosts[Stat.Accuracy];
        int evasion = target.StatBoosts[Stat.Evasion];

        var boostValues = new float[] { 1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f };

        if (accuracy > 0)
            moveAccuracy *= boostValues[accuracy];
        else
            moveAccuracy /= boostValues[accuracy];

        if (evasion > 0)
            moveAccuracy /= boostValues[evasion];
        else
            moveAccuracy *= boostValues[evasion];

        return UnityEngine.Random.Range(1, 101) <= moveAccuracy;
    }

    IEnumerator ShowStatusChanges(Cipher cipher)
    {
        while (cipher.StatusChanges.Count > 0)
        {
            var message = cipher.StatusChanges.Dequeue();
            yield return dialogBox.TypeDialog(message);
        }
    }

    IEnumerator HandleCipherFainted(BattleUnit faintedUnit)
    {
        yield return dialogBox.TypeDialog($"{faintedUnit.Cipher.Base.Name} Fainted. Poor guy.");
        faintedUnit.PlayFaintAnimation();
        yield return new WaitForSeconds(2f);

        if (!faintedUnit.IsPlayerUnit)
        {
            // Exp Gain
            int expYield = faintedUnit.Cipher.Base.ExpYield;
            int enemyLevel = faintedUnit.Cipher.Level;
            float trainerBonus = (isTrainerBattle) ? 1.5f : 1f;

            int expGain = Mathf.FloorToInt((expYield * enemyLevel * trainerBonus) / 7);
            playerUnit.Cipher.Exp += expGain;
            yield return dialogBox.TypeDialog($"{playerUnit.Cipher.Base.Name} gained {expGain} exp.");
            yield return playerUnit.Hud.SetExpSmooth();

            // Check Level Up
            while (playerUnit.Cipher.CheckForLevelUp())
            {
                playerUnit.Hud.SetLevel();
                yield return dialogBox.TypeDialog($"{playerUnit.Cipher.Base.Name} grew to {playerUnit.Cipher.Level}.");

                // Try to learn a new Move
                var newMove = playerUnit.Cipher.GetLearnableMoveAtCurrLevel();
                if (newMove != null)
                {
                    if (playerUnit.Cipher.Moves.Count < CipherBase.MaxNumOfMoves)
                    {
                        playerUnit.Cipher.LearnMove(newMove);
                        yield return dialogBox.TypeDialog($"{playerUnit.Cipher.Base.Name} learned {newMove.Base.Name}.");
                        dialogBox.SetMoveNames(playerUnit.Cipher.Moves);
                    }
                    else
                    {
                        yield return dialogBox.TypeDialog($"{playerUnit.Cipher.Base.Name} trying to learn {newMove.Base.Name}.");
                        yield return dialogBox.TypeDialog($"But it cannot learn more than {CipherBase.MaxNumOfMoves} moves.");
                        yield return ChooseMoveToForget(playerUnit.Cipher, newMove.Base);
                        yield return new WaitUntil(() => state != BattleState.MoveToForget);
                        yield return new WaitForSeconds(2f);
                    }
                }

                yield return playerUnit.Hud.SetExpSmooth(true);
            }


            yield return new WaitForSeconds(1f);
        }

        CheckForBattleOver(faintedUnit);
    }

    void CheckForBattleOver(BattleUnit faintedUnit)
    {
        if (faintedUnit.IsPlayerUnit)
        {
            var nextCipher = playerParty.GetHealthyCipher();
            if (nextCipher != null)
                OpenPartyScreen();
            else
                BattleOver(false);
        }
        else
        {
            if (!isTrainerBattle)
            {
                BattleOver(true);
            }
            else
            {
                var nextCipher = trainerParty.GetHealthyCipher();
                if (nextCipher != null)
                    StartCoroutine(AboutToUse(nextCipher));
                else
                   BattleOver(true);
            }
        }
    }

    IEnumerator ShowDamageDetails(DamageDetails damageDetails)
    {
        if (damageDetails.Critical > 1f)
            yield return dialogBox.TypeDialog("A critical hit! Yamete Kudasai >_< !");

        if (damageDetails.TypeEffectiveness > 1f)
            yield return dialogBox.TypeDialog("It's super effective! Ahh!");
        else if (damageDetails.TypeEffectiveness < 1f)
            yield return dialogBox.TypeDialog("It's not very effective! :P");
    }

    public void HandleUpdate()
    {
        if (state == BattleState.ActionSelection)
        {
            HandleActionSelection();
        }
        else if (state == BattleState.MoveSelection)
        {
            HandleMoveSelection();
        }
        else if (state == BattleState.PartyScreen)
        {
            HandlePartySelection();
        }
        else if (state == BattleState.AboutToUse)
        {
            HandleAboutToUse();
        }
        else if (state == BattleState.MoveToForget)
        {
            Action<int> onMoveSelected = (moveIndex) =>
            {
                moveSelectionUI.gameObject.SetActive(false);
                if (moveIndex == CipherBase.MaxNumOfMoves)
                {
                    // Don't learn the new move
                    StartCoroutine(dialogBox.TypeDialog($"{playerUnit.Cipher.Base.Name} did not learn {moveToLearn.Name}."));
                }
                else
                {
                    // Forget the selected move and learn new move
                    var selectedMove = playerUnit.Cipher.Moves[moveIndex].Base;
                    StartCoroutine(dialogBox.TypeDialog($"{playerUnit.Cipher.Base.Name} forgot {selectedMove.Name} and learned {moveToLearn.Name}."));
                    playerUnit.Cipher.Moves[moveIndex] = new Move(moveToLearn);
                }

                moveToLearn = null;
                state = BattleState.RunningTurn;
            };

            moveSelectionUI.HandleMoveSelection(onMoveSelected);
        }
    }

    void HandleActionSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            ++currentAction;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            --currentAction;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentAction += 2;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentAction -= 2;

        currentAction = Mathf.Clamp(currentAction, 0, 3);

            dialogBox.UpdateActionSlection(currentAction);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (currentAction == 0)
            {
                // Fight
                MoveSelection();
            }
            else if (currentAction == 1)
            {
                // Bag
                StartCoroutine(RunTurns(BattleAction.UseItem));
            }
            else if (currentAction == 2)
            {
                // Ciphers
                prevState = state;
                OpenPartyScreen();
            }
            else if (currentAction == 3)
            {
                // Run
                StartCoroutine(RunTurns(BattleAction.Run));
            }
        }
    }

    void HandleMoveSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            ++currentMove;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            --currentMove;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentMove += 2;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentMove -= 2;

        currentMove = Mathf.Clamp(currentMove, 0, playerUnit.Cipher.Moves.Count - 1);

        dialogBox.UpdateMoveSelection(currentMove, playerUnit.Cipher.Moves[currentMove]);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            var move = playerUnit.Cipher.Moves[currentMove];
            if (move.PP == 0) return;

            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            StartCoroutine(RunTurns(BattleAction.Move));
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            ActionSelection();
        }
    }

    void HandlePartySelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
            ++currentMember;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            --currentMember;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentMember += 2;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentMember -= 2;

        currentMember = Mathf.Clamp(currentMember, 0, playerParty.Ciphers.Count - 1);

        partyScreen.UpdateMemberSelection(currentMember);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            var selectedMember = playerParty.Ciphers[currentMember];
            if (selectedMember.HP <= 0)
            {
                partyScreen.SetMessageText("You can't send out a fainted cipher. xD");
                return;
            }
            if (selectedMember == playerUnit.Cipher)
            {
                partyScreen.SetMessageText("You can't switch with the same cipher LOL.");
                return;
            }

            partyScreen.gameObject.SetActive(false);

            if (prevState == BattleState.ActionSelection)
            {
                prevState = null;
                StartCoroutine(RunTurns(BattleAction.SwitchCipher));
            }
            else
            {
                state = BattleState.Busy;
                StartCoroutine(SwitchCipher(selectedMember));
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            if (playerUnit.Cipher.HP <= 0)
            {
                partyScreen.SetMessageText("You have to choose a cipher to continue.");
                return;
            }

            partyScreen.gameObject.SetActive(false);

            if (prevState == BattleState.AboutToUse)
            {
                prevState = null;
                StartCoroutine(SendNextTrainerCipher());
            }
            else
                ActionSelection();
        }
    }

    void HandleAboutToUse()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
            aboutToUseChoice = !aboutToUseChoice;

        dialogBox.UpdateChoiceBox(aboutToUseChoice);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            dialogBox.EnableChoiceBox(false);
            if (aboutToUseChoice == true)
            {
                // Yes Option
                prevState = BattleState.AboutToUse;
                OpenPartyScreen();
            }
            else
            {
                // No Option
                StartCoroutine(SendNextTrainerCipher());
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            dialogBox.EnableChoiceBox(false);
            StartCoroutine(SendNextTrainerCipher());
        }
    }

    IEnumerator SwitchCipher(Cipher newCipher)
    {
        if (playerUnit.Cipher.HP > 0)
        {
            yield return dialogBox.TypeDialog($"Get the hell outta there {playerUnit.Cipher.Base.Name}!");
            playerUnit.PlayFaintAnimation();
            yield return new WaitForSeconds(2f);
        }

        playerUnit.Setup(newCipher);
        dialogBox.SetMoveNames(newCipher.Moves);
        yield return dialogBox.TypeDialog($"Cipher summoning - {newCipher.Base.Name}!");

        if (prevState == null)
        {
            state = BattleState.RunningTurn;
        }
        else if (prevState == BattleState.AboutToUse)
        {
            prevState = null;
            StartCoroutine(SendNextTrainerCipher());
        }
    }

    IEnumerator SendNextTrainerCipher()
    {
        state = BattleState.Busy;

        var nextCipher = trainerParty.GetHealthyCipher();

        enemyUnit.Setup(nextCipher);
        yield return dialogBox.TypeDialog($"{trainer.Name} send out {nextCipher.Base.Name}!");

        state = BattleState.RunningTurn;
    }

    IEnumerator ThrowCipherball()
    {
        state = BattleState.Busy;

        if (isTrainerBattle)
        {
            yield return dialogBox.TypeDialog($"You can't steal a trainer's cipher! LOL.");
            state = BattleState.RunningTurn;
            yield break;
        }

        yield return dialogBox.TypeDialog($"{player.Name} used CIPHERBALL!");

        var cipherballObj = Instantiate(cipherballSprite, playerUnit.transform.position - new Vector3(2, 0), Quaternion.identity);
        var cipherball = cipherballObj.GetComponent<SpriteRenderer>();

        // Animations
        yield return cipherball.transform.DOJump(enemyUnit.transform.position + new Vector3(0, 2), 2f, 1, 1f).WaitForCompletion();
        yield return enemyUnit.PlayCaptureAnimation();
        yield return cipherball.transform.DOMoveY(enemyUnit.transform.position.y - 1.3f, 0.5f).WaitForCompletion();

        int shakeCount = TryToCatchCipher(enemyUnit.Cipher);

        for (int i=0; i< Mathf.Min(shakeCount, 3); ++i)
        {
            yield return new WaitForSeconds(0.5f);
            yield return cipherball.transform.DOPunchRotation(new Vector3(0, 0, 10f), 0.8f).WaitForCompletion();
        }

        if (shakeCount == 4)
        {
            // Cipher is caught
            yield return dialogBox.TypeDialog($"{enemyUnit.Cipher.Base.Name} was caught.");
            yield return cipherball.DOFade(0, 1.5f).WaitForCompletion();

            playerParty.AddCipher(enemyUnit.Cipher);
            yield return dialogBox.TypeDialog($"{enemyUnit.Cipher.Base.Name} has been added to your party.");

            Destroy(cipherball);
            BattleOver(true);
        }
        else
        {
            // Cipher Broke out
            yield return new WaitForSeconds(1f);
            cipherball.DOFade(0, 0.2f);
            yield return enemyUnit.PlayBreakOutAnimation();

            if (shakeCount < 2)
                yield return dialogBox.TypeDialog($"{enemyUnit.Cipher.Base.Name} broke free. #NOTOABUSE. ");
            else
                yield return dialogBox.TypeDialog($"Almost caught it.");

            Destroy(cipherball);
            state = BattleState.RunningTurn;
        }

    }

    int TryToCatchCipher(Cipher cipher)
    {
        float a = (3 * cipher.MaxHp - 2 * cipher.HP) * cipher.Base.CatchRate * ConditionsDB.GetStatusBonus(cipher.Status) / (3 * cipher.MaxHp);

        if (a >= 255)
            return 4;

        float b = 1048560 / Mathf.Sqrt(Mathf.Sqrt(16711680 / a));

        int shakeCount = 0;
        while (shakeCount < 4)
        {
            if (UnityEngine.Random.Range(0, 65535) >= b)
                break;

            shakeCount++;
        }

        return shakeCount;
    }

    IEnumerator TryToEscape()
    {
        state = BattleState.Busy;

        if (isTrainerBattle)
        {
            yield return dialogBox.TypeDialog($"You can't run from trainer battles!");
            state = BattleState.RunningTurn;
            yield break;
        }

        ++escapeAttempts;

        int playerSpeed = playerUnit.Cipher.Speed;
        int enemySpeed = enemyUnit.Cipher.Speed;

        if (enemySpeed < playerSpeed)
        {
            yield return dialogBox.TypeDialog($"Ran away safely!");
            BattleOver(true);
        }
        else
        {
            float f = (playerSpeed * 128) / enemySpeed + 30 * escapeAttempts;
            f = f % 256;

            if (UnityEngine.Random.Range(0, 256) < f)
            {
                yield return dialogBox.TypeDialog($"Ran away safely!");
                BattleOver(true);
            }
            else
            {
                yield return dialogBox.TypeDialog($"Can't escape");
                state = BattleState.RunningTurn;
            }
        }
    }
}
