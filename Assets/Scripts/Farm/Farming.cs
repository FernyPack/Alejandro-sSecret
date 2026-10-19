using UnityEngine;

public class Farming : Interactable
{
    public enum GrowthStage { Empty, Growing, ReadyToHarvest }

    [Header("Growth Settings")]
    public GrowthStage currentStage = GrowthStage.Empty;
    public bool isWatered = false;

    [Header("Visual Component Links")]
    public SpriteRenderer plantSpriteRenderer;
    public Animator       plantAnimator;

    [Header("3D Soil Visuals")]
    public MeshRenderer soilMeshRenderer;
    public Material     drySoilMaterial;

    [Header("UI Reference")]
    [Tooltip("Assign the OptionUI component from your OptionCanvas.")]
    public OptionUI optionUI;

    [Header("Farming Dialogue (Editable in Inspector)")]
    [TextArea(2, 5)]
    public string emptyPlotPrompt = "It's empty, should I plant something?";

    [TextArea(2, 5)]
    [Tooltip("Use {0} for seed name.")]
    public string waterPrompt = "Water the {0}?";

    [TextArea(2, 5)]
    [Tooltip("Use {0} for name, {1} for progress %, {2} for days left.")]
    public string growthStatusDialogue = "{0} is growing!\nProgress: {1:F0}%\nDays remaining: {2:F1}";

    [TextArea(2, 5)]
    public string waterSuccessDialogue = "You watered the soil patch.";

    [TextArea(2, 5)]
    [Tooltip("Use {0} for yield amount, {1} for crop name.")]
    public string harvestSuccessDialogue = "Harvested {0} {1}(s)!";

    private SeedData      activeSeed;
    private Material      wetSoilMaterial;
    private float         growthTimer = 0f;
    private ITimeProvider timeProvider;
    private float         lastTotalDays;

    void Start()
    {
        if (soilMeshRenderer != null)
            wetSoilMaterial = soilMeshRenderer.material;

        timeProvider = ServiceLocator.Get<ITimeProvider>();
        if (timeProvider != null)
            lastTotalDays = timeProvider.TotalElapsedDays;

        if (optionUI == null)
            optionUI = FindFirstObjectByType<OptionUI>();

        RefreshVisuals();
    }

    void Update()
    {
        HandleGrowthSimulation();
    }

    public override void Interact(GameObject playerObject)
    {
        IInventory inventory = playerObject.GetComponent<PlayerInventory>()
                            ?? ServiceLocator.Get<IInventory>();
        if (inventory == null) return;

        PlayerAction playerAction = playerObject.GetComponent<PlayerAction>();

        if (optionUI == null)
            optionUI = FindFirstObjectByType<OptionUI>();

        if (currentStage == GrowthStage.Empty)
            HandleEmptyPlot(inventory, playerAction);
        else if (currentStage == GrowthStage.Growing)
            HandleGrowingPlot(inventory, playerAction);
        else if (currentStage == GrowthStage.ReadyToHarvest)
            HandleHarvest(inventory, playerAction);
    }

    private void HandleEmptyPlot(IInventory inventory, PlayerAction playerAction)
    {
        playerAction?.DisplayDialogue(emptyPlotPrompt);

        optionUI?.ShowYesNo(
            onYes: () =>
            {
                optionUI.ShowInventoryList(inventory.SeedInventory, (selectedSeed) =>
                {
                    if (inventory.UseSeed(selectedSeed))
                        PlantSeed(selectedSeed);

                    playerAction?.CloseDialogue();
                });
            },
            onNo: () => playerAction?.CloseDialogue()
        );
    }

    private void HandleGrowingPlot(IInventory inventory, PlayerAction playerAction)
    {
        if (!isWatered)
        {
            if (activeSeed != null)
                playerAction?.DisplayDialogue(string.Format(waterPrompt, activeSeed.seedName));

            optionUI?.ShowYesNo(
                onYes: () =>
                {
                    WaterCrop();
                    playerAction?.CloseDialogue();
                    playerAction?.DisplayDialogue(waterSuccessDialogue);
                },
                onNo: () =>
                {
                    optionUI.HidePanels();
                    playerAction?.CloseDialogue();
                }
            );
        }
        else
        {
            if (activeSeed != null)
            {
                float progress  = (growthTimer / activeSeed.daysToGrow) * 100f;
                float remaining = Mathf.Max(0f, activeSeed.daysToGrow - growthTimer);
                playerAction?.DisplayDialogue(
                    string.Format(growthStatusDialogue, activeSeed.seedName, progress, remaining));
            }
        }
    }

    private void HandleHarvest(IInventory inventory, PlayerAction playerAction)
    {
        if (activeSeed != null)
        {
            inventory.AddCrop(activeSeed.cropYieldAmount);
            playerAction?.DisplayDialogue(
                string.Format(harvestSuccessDialogue, activeSeed.cropYieldAmount, activeSeed.seedName));
        }

        activeSeed = null;
        TransitionToStage(GrowthStage.Empty);
    }

    private void HandleGrowthSimulation()
    {
        if (currentStage != GrowthStage.Growing || !isWatered || activeSeed == null) return;

        if (timeProvider != null)
        {
            float now   = timeProvider.TotalElapsedDays;
            float delta = Mathf.Max(0f, now - lastTotalDays);
            growthTimer   += delta;
            lastTotalDays  = now;
        }
        else
        {
            growthTimer += Time.deltaTime / 60f;
        }

        if (growthTimer >= activeSeed.daysToGrow)
            TransitionToStage(GrowthStage.ReadyToHarvest);
    }

    private void TransitionToStage(GrowthStage next)
    {
        currentStage = next;
        if (next == GrowthStage.Empty) isWatered = false;
        RefreshVisuals();
    }

    private void PlantSeed(SeedData seed)
    {
        activeSeed  = seed;
        growthTimer = 0f;
        isWatered   = false;

        if (timeProvider != null)
            lastTotalDays = timeProvider.TotalElapsedDays;

        TransitionToStage(GrowthStage.Growing);
    }

    private void WaterCrop()
    {
        isWatered = true;
        if (timeProvider != null)
            lastTotalDays = timeProvider.TotalElapsedDays;
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        if (plantSpriteRenderer != null)
        {
            bool visible = currentStage != GrowthStage.Empty;
            plantSpriteRenderer.gameObject.SetActive(visible);
            if (visible && activeSeed != null)
                plantSpriteRenderer.sprite = activeSeed.plantSprite;
        }

        if (soilMeshRenderer != null)
            soilMeshRenderer.material = isWatered ? wetSoilMaterial : drySoilMaterial;

        if (plantAnimator != null)
            plantAnimator.SetInteger("GrowthStage", (int)currentStage);
    }
}
