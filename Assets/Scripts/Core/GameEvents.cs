using System;

public static class GameEvents
{
    public static event Action OnGameStarted;
    public static void TriggerGameStarted() => OnGameStarted?.Invoke();

    public static event Action<string> OnDeclarationMade;
    public static void TriggerDeclarationMade(string text) => OnDeclarationMade?.Invoke(text);

    public static event Action<CardDataSO, int> OnCardPlayed;
    public static void TriggerCardPlayed(CardDataSO card, int slotIndex) => OnCardPlayed?.Invoke(card, slotIndex);

    public static event Action<int, int> OnCardsDrawn;
    public static void TriggerCardsDrawn(int count, int remaining) => OnCardsDrawn?.Invoke(count, remaining);
}
