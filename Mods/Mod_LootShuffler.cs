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
        CecilImporter.InjectMethodIntoType("LootList", "OnItemAcceptDialogEnd", "Private");
    }

    private void OnItemAcceptDialogEnd(UIMessageBox.Result result, UIMessageBox sender)
    {
        sender.OnDialogEnd = (UIMessageBox.OnEndDialog)System.Delegate.Remove(sender.OnDialogEnd, new UIMessageBox.OnEndDialog(this.OnItemAcceptDialogEnd));
        if (result == UIMessageBox.Result.AFFIRMATIVE)
        {
            Console.AddMessage("You accepted... some item!");
        }
    }

    public object[] EvaluateNew()
    {
        UIConsole.Instance.MaxEntries = 1000; //added this

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
                    if (!(this.items[i].Item == null) && !(this.items[i].Item is global::LootList))
                    {
                        /*UIWindowManager.ShowMessageBox(UIMessageBox.ButtonStyle.YESNO, "Accept Item?", this.items[i].Item.name).OnDialogEnd = delegate(UIMessageBox.Result result, UIMessageBox boxsend)
                        {
                            if (result == UIMessageBox.Result.AFFIRMATIVE)
                            {
                                Scripts.GiveItem(this.items[i].Item.name, 1);
                                flag = true;
                            }
                        };*/
                        //UIMessageBox msgBox = UIWindowManager.ShowMessageBox(UIMessageBox.ButtonStyle.YESNO, "Accept Item?", this.items[i].Item.name);
                        //msgBox.OnDialogEnd = (UIMessageBox.OnEndDialog)System.Delegate.Combine(msgBox.OnDialogEnd, new UIMessageBox.OnEndDialog(this.OnItemAcceptDialogEnd));
                        //this.QuitButton.onClick = (UIEventListener.VoidDelegate)Delegate.Combine(this.QuitButton.onClick, new UIEventListener.VoidDelegate(this.OnQuitClicked));
                    }

                    //flag2 = true; //select this high quality item
                    //flag = true; //you don't get any other high quality items
                }
            }

            else //item is always included
            {
                flag2 = true;
            }
            if (!(this.items[i].Item == null))
            {
                //print all item codes to console
                for (int j = 0; j < this.items[i].Count; j++)
                {
                    if (this.items[i].Item is global::LootList)
                    {
                        Console.AddMessage("--------(BEGIN lootlist): " + this.items[i].Item.name);
                        object[] arr = (this.items[i].Item as global::LootList).Evaluate();
                        foreach (object o in arr)
                            Console.AddMessage("  in lootlist: " + (o as Object).name);
                        Console.AddMessage("--------(END lootlist)");
                    }
                    else
                    {
                        Console.AddMessage("item: " + this.items[i].Item.name);
                    }
                }

                //flag2 = true;

                if (flag2)
                {
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
            }
        }

        

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