using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mono.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;


class MainClass
{
	public static ModuleDefinition mainMod;

    public const string GAME_VERSION = "3.0.2.1008";
    public const string ROOT_FOLDER = @"..\..\";
    public const string SOURCE_ROOT = ROOT_FOLDER + @"poe-assemblies\" + GAME_VERSION + @"\";
    public const string ASSEMBLY = "Assembly-CSharp.dll";

    public static string SourceAssembly(string os)
    {
        return SOURCE_ROOT + os + @"\" + ASSEMBLY;
    }

	public static void Main (string[] args)
	{
#if FIRSTRUN
        CreatePublicAssembly(@"..\..\..\poe-assemblies\Assembly-CSharp.dll", @"..\..\..\poe-assembly-public\Assembly-CSharp.dll");
#elif PUBLISH
        Publish(ROOT_FOLDER + @"publish\");
#else
        // debug mode...write straight to game binary folder
        //ApplyModToGameFiles(SourceAssembly("win"), @"D:\SteamLibrary\SteamApps\common\Pillars of Eternity\PillarsOfEternity_Data\Managed\Assembly-CSharp.dll");
        ApplyModToGameFiles(SourceAssembly("win"), @"E:\Programs\Pillars of Eternity\PillarsOfEternity_Data\Managed\Assembly-CSharp.dll");
        //ApplyModToGameFiles(SourceAssembly("win"), @"C:\Games\Steam\SteamApps\common\Pillars of Eternity\PillarsOfEternity_Data\Managed\Assembly-CSharp.dll");
#endif
        Log("Patching complete.");
		System.Console.ReadLine();
	}

    public static void Publish(string destinationFolder)
    {
        var oses = new[] { "win", "linux", "mac" };
        foreach (var os in oses)
        {
            var source = SourceAssembly(os);
            if (File.Exists(source))
            {
                var dest = System.IO.Path.Combine(System.IO.Path.Combine(destinationFolder, os), ASSEMBLY);
                ApplyModToGameFiles(source, dest);
                Log("Finished preparing " + os + "\n");
            }
            else
                Log("No " + os + " assembly found.  Skipping it...\n");
        }
    }

    public static void ApplyModToGameFiles(string source, string destination)
    {
#if !FIRSTRUN
        // loading poe's assembly
        Log("Source Assembly: " + source + "\n" + "Destination Assembly: " + destination + "\n");
        AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(source);
        //AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(@"E:\POE\poe-modding-framework-w-nerfed-xp-table-option\poe-assemblies\1.0.4.054\win\Assembly-CSharp.dll");
        mainMod = asm.MainModule;
        //TODO: rename all compile generated fields/methods/properties/nestedtypes to something c# compliant (not types though!)
        //TODO: let's find all events and append "Event" to them, because when they're compiled generated, they hide fields of same name
        //TODO: let's also find nested types (compile generated classes inside classes) and apply everything we want to apply to them as well (make their fields public, rename them, etc)

        mainMod.Types.FirstOrDefault(x => x.Name == "GameState").Events.FirstOrDefault(x => x.Name == "OnLevelLoaded").Name = "OnLevelLoadedEvent";
        mainMod.Types.FirstOrDefault(x => x.Name == "UIWindowManager").Events.FirstOrDefault(x => x.Name == "OnWindowHidden").Name = "OnWindowHiddenFieldEvent";
        mainMod.Types.FirstOrDefault(x => x.Name == "SelectionCircle").Events.FirstOrDefault(x => x.Name == "OnSharedMaterialChanged").Name = "OnSharedMaterialChangedEvent";
        mainMod.Types.FirstOrDefault(x => x.Name == "UIRadioButtonGroup").Events.FirstOrDefault(x => x.Name == "OnRadioSelectionChanged").Name = "OnRadioSelectionChangedEvent";
        mainMod.Types.FirstOrDefault(x => x.Name == "UIDropdownMenu").Events.FirstOrDefault(x => x.Name == "OnDropdownOptionChanged").Name = "OnDropdownOptionChangedEvent";
        mainMod.Types.FirstOrDefault(x => x.Name == "InGameHUD").Events.FirstOrDefault(x => x.Name == "OnHudVisibilityChanged").Name = "OnHudVisibilityChangedEvent";
        mainMod.Types.FirstOrDefault(x => x.Name == "UIMapTooltip").Events.FirstOrDefault(x => x.Name == "OnSelectedCharacterChanged").Name = "OnSelectedCharacterChangedEvent";
        mainMod.Types.FirstOrDefault(x => x.Name == "AIController").Methods.FirstOrDefault(x => x.Name == "AddEngagedBy").IsPublic = true;

        Log("Patching Version Number Mod"); //UIVersionNumber
        Mod_VersionNumber.main();

        Log("Patching GameSpeed Mod");  //TimeController
        Mod_GameSpeed.main();

        Log("Patching LootShuffler Mod");  //Loot
        Mod_LootShuffler.main();


        //Personal mods:
        
        //Increase max camping supplies to 6 on PotD
        Log("Patching CampingSupplies Mod");  //CampingSupplies
        Mod_MaxCampingSupplies.main();

        //Modify enemy stat bonus on PotD
        Log("Patching PotD Stat Bonus Mod"); 
        Mod_PotDStatBonus.main();

        //Unlimited Attribute points
        Log("Patching Attribute Mod");
        Mod_Attributes.main();
        
#if LOADGAMEPATCH
        CecilImporter.WriteFilePatches();
#else
        //saving results
        asm.Write(destination);
#endif
#endif
    }

    public static void CreatePublicAssembly(string source, string destination)
    {
        AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(source);
        mainMod = asm.MainModule;
        FirstRun(mainMod);
        asm.Write(destination);
    }
		
	public static void Log(string text)
	{
		System.Console.WriteLine (text);
	}

	public static void FirstRun(ModuleDefinition mainMod)
	{
		int types = 0;
		int nestedTypes = 0;
		int fields = 0;
		int methods = 0;
		foreach (TypeDefinition td in mainMod.Types)
		{
			types++;
			foreach (TypeDefinition ntd in td.NestedTypes)
			{
				nestedTypes++;
				foreach (FieldDefinition fd in ntd.Fields)
				{
					fields++;
					if (fd.IsPrivate)
						fd.IsPublic = true;
				}
		
				foreach (MethodDefinition md in ntd.Methods)
				{
					if (md.IsPrivate)
						md.IsPublic = true;
					methods++;
				}
				if (ntd.IsNestedPrivate)
					ntd.IsNestedPublic = true;
			}
			foreach (FieldDefinition fd in td.Fields)
			{
				fields++;
				if (fd.IsPrivate)
					fd.IsPublic = true;
			}
		
			foreach (MethodDefinition md in td.Methods)
			{
				if (md.IsPrivate)
					md.IsPublic = true;
				methods++;
			}
		}
		Log ("Patched types: " + types + ", nested types: " + nestedTypes+", methods: "+ methods+ ", fields: "+fields);


		mainMod.Types.FirstOrDefault (x => x.Name == "AIController").Methods.FirstOrDefault (x => x.Name == "AddEngagedBy").IsPublic = true;

		// became redundant... if everything works fine, DELETE THIS
//		mainMod.Types.FirstOrDefault (x => x.Name == "UIAbilityBarButton").NestedTypes.FirstOrDefault (x => x.Name == "<OnClick>c__AnonStorey78").Name = "OnClickc__AnonStorey78"; // this line might not compile, but that's just cause the name changes in each version.. just change it here and everywhere else where it's mentionned in this .cs file
//		mainMod.Types.FirstOrDefault (x => x.Name == "UIAbilityBarButton").NestedTypes.FirstOrDefault (x => x.Name == "OnClickc__AnonStorey78").Methods.FirstOrDefault (x => x.Name == "<>m__5F").Name = "m__5F"; // same here
//		mainMod.Types.FirstOrDefault (x => x.Name == "UIAbilityBarButton").NestedTypes.FirstOrDefault (x => x.Name == "OnClickc__AnonStorey78").Methods.FirstOrDefault (x => x.Name == "m__5F").IsPublic = true;
//		mainMod.Types.FirstOrDefault (x => x.Name == "UIAbilityBarButton").NestedTypes.FirstOrDefault (x => x.Name == "OnClickc__AnonStorey78").Fields.FirstOrDefault (x => x.Name == "i").IsPublic = true;

		foreach (var type in mainMod.Types)
		{
			if (type.CustomAttributes.FirstOrDefault (x => x.AttributeType.Name == "CompilerGeneratedAttribute") == null && (type.Namespace == "" || type.Namespace == "AI" || type.Namespace == "AI.Achievement" || type.Namespace == "AI.Pet" ||type.Namespace == "AI.Plan" ||type.Namespace == "AI.Player"))
			{
				foreach (var vent in type.Events) // rename events that have the same as properties or fields
				{
					if (type.Fields.FirstOrDefault (x => x.Name == vent.Name) != null) // if the event has the same name as a field, rename it
					{
						vent.Name += "Event";
					}

					if (type.Properties.FirstOrDefault (x => x.Name == vent.Name) != null) // if the event has the same name as a property, rename it
					{
						vent.Name += "Event";
					}
				}

				foreach (var field in type.Fields) // removes forbidden characters from fields
				{
					if (field.HasCustomAttributes && field.CustomAttributes.FirstOrDefault (x => x.AttributeType.Name == "CompilerGeneratedAttribute") != null)
					{
						field.Name = field.Name.Replace ("<", "");
						field.Name = field.Name.Replace (">", "");
						field.Name = field.Name.Replace ("$", "");
					}
				}

				foreach (var mtd in type.Methods) // removes forbidden characters from methods
				{
					if (mtd.HasCustomAttributes && mtd.CustomAttributes.FirstOrDefault (x => x.AttributeType.Name == "CompilerGeneratedAttribute") != null)
					{
						mtd.Name = mtd.Name.Replace ("<", "");
						mtd.Name = mtd.Name.Replace (">", "");
						mtd.Name = mtd.Name.Replace ("$", "");
					}
				}

				foreach (var ntype in type.NestedTypes) // removes forbidden characters from nested types
				{
					if (ntype.HasCustomAttributes && ntype.CustomAttributes.FirstOrDefault (x => x.AttributeType.Name == "CompilerGeneratedAttribute") != null)
					{
						ntype.Name = ntype.Name.Replace ("<", "");
						ntype.Name = ntype.Name.Replace (">", "");
						ntype.Name = ntype.Name.Replace ("$", "");
					}
				}
			}
		}
	}
}