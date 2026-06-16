public static class GameSession
{
    public static bool GameplayInputBlocked { get; private set; }

    public static void SetGameplayInputBlocked(bool blocked)
    {
        GameplayInputBlocked = blocked;
    }
}
