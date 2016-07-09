using System.Collections;
using System.Collections.Generic;
using Mono.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

#if !FIRSTRUN

public class Mod_LootShuffler
{
    public static void main()
    {
        Mod_LootShuffler_Loot.patch();
    }
}

public class Mod_LootShuffler_Loot : Loot
{
    public static void patch()
    {
        CecilImporter.PatchExistingMethod(typeof(Mod_LootShuffler_Loot), "Loot", "SetSeed", "SetSeedNew");
    }
    private void SetSeedNew()
    {
        UnityEngine.Random.seed = (int)(base.transform.position.x + base.transform.position.z) * GameState.s_playerCharacter.name.GetHashCode() + WorldTime.Instance.CurrentDay;
    }
}
#endif
