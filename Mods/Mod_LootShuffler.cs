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
        Mod_LootShuffler_LootList.patch();
        //Mod_LootShuffler_Container.patch();
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
        ResetSeed();
    }
}

public class Mod_LootShuffler_LootList : LootList
{
    public static void patch()
    {
        CecilImporter.PatchExistingMethod(typeof(Mod_LootShuffler_LootList), "LootList", "Evaluate", "EvaluateNew");
    }

    //adding console output to understand how it works
    public object[] EvaluateNew()
    {
        ArrayList arrayList = new ArrayList();
        int num = this.TotalWeight;

        int num2 = UnityEngine.Random.Range(0, num);

        num = 0; //total weight added to container
        bool flag = false;

        for (int i = 0; i < this.items.Length; i++)
        {
            bool flag2 = false; //flag2=true means the item will actually appear in the container
            if (!this.items[i].Always) //item is randomly included or not
            {
                num += this.items[i].Weight;
                if (num2 < num && !flag)
                {
                    flag2 = true; //select this high quality item
                    flag = true; //you don't get any other high quality items
                }
            }

            else //item is always included
            {
                flag2 = true;
            }
            if (!(this.items[i].Item == null))
            {
                if (!(this.items[i].Item is global::LootList))
                {
                    Console.AddMessage(this.items[i].Item.name);
                }

                //UIMessageBox uIMessageBox = UIWindowManager.ShowMessageBox(UIMessageBox.ButtonStyle.YESNO, "Accept item?", GUIUtils.Format(417, new object[]
				//{
				//	CharacterStats.Name(UIGrimoireManager.Instance.SelectedCharacter.get_gameObject()),
				//	GenericAbility.Name(componentInParent.Spell)
				//}));

                //global::Scripts.LogItemGet(this.items[i].Item as Item, 1, false);
                //Console.AddMessage("  LootItem's item is not null");

                //add all items?
                //flag2 = true;

                if (flag2)
                {
                    //Console.AddMessage("  flag2 is true");
                    for (int j = 0; j < this.items[i].Count; j++)
                    {
                        if (this.items[i].Item is global::LootList)
                        {
                            arrayList.AddRange((this.items[i].Item as global::LootList).Evaluate());
                        }
                        else
                        {
                            arrayList.Add(this.items[i].Item);
                        }
                    }
                }
                
                //Console.AddMessage("skipping: " + this.items[i].Item. + "(" + this.items[i].Item.name + ")");
            }
        }
        //Console.AddMessage("End EvaluateNew\n");
        return arrayList.ToArray();
    }
}

public class Mod_LootShuffler_Container : Container
{
    public static void patch()
    {
        CecilImporter.PatchExistingMethod(typeof(Mod_LootShuffler_Container), "Container", "Close", "CloseNew");
    }

}

#endif