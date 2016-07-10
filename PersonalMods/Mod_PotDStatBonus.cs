using System.Collections;
using System.Collections.Generic;
using Mono.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

#if !FIRSTRUN

public class Mod_PotDStatBonus : CharacterStats
{
    public static void main()
    {
        patch();
    }
    public static void patch()
    {
        CecilImporter.PatchExistingProperty(typeof(Mod_PotDStatBonus), "CharacterStats", "DifficultyStatBonus", "DifficultyStatBonusNew");
    }

    public float DifficultyStatBonusNew
    {
        get
        {
            if (GameState.Instance && GameState.Instance.IsDifficultyPotd && !this.IsPartyMember)
            {
                return 35f;
            }
            return 0f;
        }
    }

}
#endif