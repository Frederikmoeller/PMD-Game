using UnityEngine;

public static class GameLogger
{
    private static ActionLogManager _actionLog;
    
    public static void Initialize(ActionLogManager actionLogManager)
    {
        _actionLog = actionLogManager;
        LogSystem("Game logger initialized");
    }
    
    public static void LogAction(string message)
    {
        _actionLog?.AddEntry(message, Color.white);
    }
    
    public static void LogCombat(string attacker, string action, string target, int damage = 0)
    {
        _actionLog?.AddCombatEntry(attacker, action, target, damage);
    }
    
    public static void LogItem(string playerName, string itemName)
    {
        _actionLog?.AddItemEntry(playerName, itemName);
    }
    
    public static void LogSystem(string message)
    {
        _actionLog?.AddSystemEntry(message);
    }
    
    // Helper for quick logging
    public static void Log(string message)
    {
        LogAction(message);
    }
}
