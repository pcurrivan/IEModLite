using System.Collections;
using System.Collections.Generic;
using Mono.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

#if !FIRSTRUN

public class Mod_Attributes
{
    public static void main()
    {
        Mod_Attributes_UICharacterCreationAttributeSetter.patch();
    }
}

public class Mod_Attributes_UICharacterCreationAttributeSetter : UICharacterCreationAttributeSetter
{
    public static void patch()
    {
        CecilImporter.PatchExistingMethod(typeof(Mod_Attributes_UICharacterCreationAttributeSetter), "UICharacterCreationAttributeSetter", "IncAllowed", "IncAllowedNew");
    }
    private bool IncAllowedNew()
    {
        return true;
    }
}
#endif