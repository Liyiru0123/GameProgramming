using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAbilities : MonoBehaviour
{
    [SerializeField] private bool dashUnlocked;

    public bool DashUnlocked => dashUnlocked;

    public void SetDashUnlocked(bool unlocked, bool save = true)
    {
        dashUnlocked = unlocked;

        if (save)
        {
            SaveGameSystem.Instance?.SaveCurrentSlot();
        }
    }
}
