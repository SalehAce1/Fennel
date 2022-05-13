using System;

namespace Fennel
{
    [Serializable]
    public class GlobalModSettings
    {
        public BossStatue.Completion CompletionFennel = new BossStatue.Completion
        {
            isUnlocked = true,
            hasBeenSeen = true,
        };
    }
}