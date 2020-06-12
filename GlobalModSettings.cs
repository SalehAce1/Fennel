using System;
using Modding;
using UnityEngine;

namespace Fennel
{
    [Serializable]
    public class GlobalModSettings : ModSettings
    {
        public BossStatue.Completion CompletionFennel = new BossStatue.Completion
        {
            isUnlocked = true,
            hasBeenSeen = true,
        };

        public void OnBeforeSerialize()
        {
            StringValues["CompletionFennel"] = JsonUtility.ToJson(CompletionFennel);
        }

        public void OnAfterDeserialize()
        {
            StringValues.TryGetValue("CompletionFennel", out string @out1);
            if (string.IsNullOrEmpty(@out1)) return;
            CompletionFennel = JsonUtility.FromJson<BossStatue.Completion>(@out1);
        }
    }
}