using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerTransitionState
{
    public int CurrentHealth;
    public int Level;
    public int CurrentExperience;
    public int SkillPoints;
    public readonly List<PlayerSkillState> Skills = new List<PlayerSkillState>();
    public readonly List<InventoryRestoreEntry> Inventory = new List<InventoryRestoreEntry>();
    public WeaponData EquippedWeapon;
}

public static class PlayerTransitionBuffer
{
    private static PlayerTransitionState pendingState;

    public static bool HasPendingState => pendingState != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        pendingState = null;
    }

    public static void Store(PlayerTransitionState state)
    {
        pendingState = state;
    }

    public static bool TryConsume(out PlayerTransitionState state)
    {
        state = pendingState;
        pendingState = null;
        return state != null;
    }

    public static void Clear()
    {
        pendingState = null;
    }
}
