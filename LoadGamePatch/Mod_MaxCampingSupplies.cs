using System.Collections;
using System.Collections.Generic;
using Mono.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

#if !FIRSTRUN

public class Mod_MaxCampingSupplies : CampingSupplies
{
    public static void main()
    {
        patch();
    }
    public static void patch()
    {
        CecilImporter.PatchExistingProperty(typeof(Mod_MaxCampingSupplies), "CampingSupplies", "StackMaximum", "StackMaximumNew");
    }

    public static int StackMaximumNew
    {
        get
        {
            int result = 1;
            GameDifficulty difficulty = GameState.Instance.Difficulty;
            switch (difficulty)
            {
                case GameDifficulty.Easy:
                    result = 10;
                    break;
                case GameDifficulty.Normal:
                    result = 8;
                    break;
                case GameDifficulty.Hard:
                case GameDifficulty.PathOfTheDamned:
                    result = 6; //increased from 2
                    break;
                case GameDifficulty.StoryTime:
                    result = 99;
                    break;
                default:
                    UIDebug.Instance.LogOnceOnlyWarning("Please set StackMaximum in CampingSupplies class for difficulty '" + difficulty + "'.", UIDebug.Department.Programming, 10f);
                    break;
            }
            return result;
        }
    }
}
#endif