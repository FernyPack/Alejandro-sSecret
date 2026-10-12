using UnityEngine;

public class Collect : Interactable
{
    [Header("Reward Settings")]
    public int goldAmount = 500;
    public bool oneTimeOnly = true;

    [Header("Dialogue Settings")]
    [TextArea(2, 5)]
    public string collectDialogue = "You found {0} gold!";

    [TextArea(2, 5)]
    public string alreadyCollectedDialogue = "There is nothing left here.";

    private bool hasCollected = false;

    public override void Interact(GameObject playerObject)
    {
        PlayerAction playerAction = playerObject.GetComponent<PlayerAction>();

        if (oneTimeOnly && hasCollected)
        {
            if (playerAction != null)
                playerAction.DisplayDialogue(alreadyCollectedDialogue);
            return;
        }

        IInventory inventory = ServiceLocator.Get<IInventory>();
        if (inventory == null) return;

        inventory.AddCrop(1);
        inventory.SellCrops(1, goldAmount);
        hasCollected = true;

        if (playerAction != null)
            playerAction.DisplayDialogue(string.Format(collectDialogue, goldAmount));
    }
}