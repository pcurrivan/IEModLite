using System.Collections;
using System.Collections.Generic;
using Mono.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

#if !FIRSTRUN

public class Mod_Recitation
{
    public static void main()
    {
        Mod_Recitation_Phrase.patch();
    }
}

public class Mod_Recitation_Phrase : Phrase
{
    public static void patch()
    {
        CecilImporter.PatchExistingMethod(typeof(Mod_Recitation_Phrase), "Phrase", "CalculateRecitation", "CalculateRecitationNew");
    }
    public float CalculateRecitationNew(global::CharacterStats character)
    {
        float num = 1f;
        float dexMod = 1f;
        if (character)
        {
            num = character.GetStatusEffectValueMultiplier(global::StatusEffect.ModifiedStat.PhraseRecitationLengthMult);
            dexMod = 0.03f - 0.01f * (float)(character.GetAttributeScore(CharacterStats.AttributeScoreType.Dexterity));
        }
        return Mathf.Max(0.25f, num * this.BaseRecitation - dexMod);
    }
}
#endif