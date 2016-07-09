# README #

## How to set it up: ##
- Sign up for bitbucket and get Atlassian sourcetree.
- Fork Bester's Modding Framework from https://bitbucket.org/Bester/poe-modding-framework/ and clone your forked repository to your computer, to a location of your choice.
- Add the following folder structures to that folder: 1) poe-assemblies\1.0.5.0567\win (replace version number with the newest number) and 2) poe-assembly-public\
- Copy all the DLLs in ..\Pillars of Eternity\PillarsOfEternity_Data\Managed to the newly created win folder.  Make sure these are the default game DLLs, not modded ones.
- Also put another copy of Assembly-CSharp.DLL directly in the poe-assemblies folder.
- Open PEFirst.sln with visual studio
- Go to PEFirst project properties->reference paths->add the path to the poe-assembly-public folder and another to the win folder as reference paths.  The public assembly folder should be before the win folder in the list.
- Go to Build->Configuration Manager->set the active solution configuration for to FIRSTRUN.  Make sure the PEFirst configuration in the table changed to FIRSTRUN.
- Make sure the project is set to PEFIRST (dropdown box under open file tabs in VS).  Build the solution.  Visual Studio should automatically fetch four Mono.Cecil DLLs with NuGet. Run (ctrl-f5).
- It should succeed. Now close Visual Studio.  There should now be a public version of Assembly-CSharp.DLL in the poe-assembly-public folder.  Leave it there.
- Reopen your fork in Visual Studio. Go back to build config manager and set the config for PEFirst to Debug. Again, make sure the project is set to PEFIRST.
- Go to ~line 33 of Program.cs, you should see the line: ApplyModToGameFiles(SourceAssembly("win"), @"D:\SteamLibrary\SteamApps\common\Pillars of Eternity\PillarsOfEternity_Data\Managed\Assembly-CSharp.dll"); or something like it. Comment this line out and add a line below it with the full path to Assembly-CSharp.dll in the Pillars of Eternity installation on your computer.
- The solution should now build and run.  If you look in ..\Pillars of Eternity\PillarsOfEternity_Data\Managed, Assembly-CSharp.DLL should be modified.
- You can now write your mod.

## Usage: ##
- All your classes must be named "Mod", followed by any digits, i.e.: Mod01
- Your Mod-something classes must inherit from the class that you're going to modify (you can see this in examples). Unless you want to inject some code that uses the "base" keyword, in which case you may need to inherit from its base class. Because of this, in certain rare cases it might cause problems when you use both "base" and "this" keywords, you can see why. A workaround is possible, you can see an example of it in ModFastSneak, where the code uses a work-around to successfully call base.Update()
- The order matters, kind of like in c++. You can't inject a method that references a variable before injecting that variable into the target assembly first.

### What works: ###
- PatchExistingMethod //clears all instructions in the target method and injects instructions from your custom method
- PatchExistingProperty //patches the get-set methods of an existing property... i don't remember if I tested it at all, sorry
- PatchExistingNestedType // patches an existing enum
- InjectMethodIntoType //injects an empty method into a class, only attributes currently supported are: public static or private
- InjectFieldIntoType //injects a field. It copies the type, attributes and name from the field that it receives as a paramater.
- MakeFieldPublic
- MakeMethodPublic
- MakePropertyPublic
- RemoveMethodFromType
- RemoveFieldFromType

### What doesn't work yet: ###
- delegates ("delegate" creates and adds a new method to its type... this method has to be found and copied to the corresponding type in the target assembly before it can be refereced in the target assembly)
- lambda expressions ([for a similar reason](https://groups.google.com/forum/#!msg/mono-cecil/DSXD1W49O5Q/RbxzEyLZET4J))
- The following expression wouldn't work: new int[] { 0, 3, 1, 4, 5, 2 }... The reason being, it creates a field and a type in \<PrivateImplementationDetails\> and we need to copy them and rename them. For now, simply code it differently instead: new int[7], then myint[0] = 0;, etc...

### Limitations: ###
- Sometimes you're going to have a field and a compiler generated event of the same name inside a type. There is no way to programmatically distinguish between them when referencing them in your own code, so the event would be masking the field. For this reason, you need to rename the field in the "RUN THIS CODE ONCE" section.
- The future version will rename all compiler generated events and remove forbidden characters from compiler generated types, fields, etc during the first run.

### Regarding GameObject browser: ###
- it can't change components' numerical values yet, although it should be easy to implement.

## How to apply a new Game Patch ##
- We have 2 branches.
    - __default__ - Where you write your Mod code.
    - __original-game__ - Where we track the original game code.

### When a new patch comes out... ###
- Switch to the __original-game__ branch
- Change the code to load the new version of __Assembly-CSharp.dll__
- Switch the Visual Studio Build Configuration to __LOADGAMEPATCH__
- Compile
- Run.  This will cause the program to scan the new assembly and pull out all of the code from the game that our mods want to modify.  It stores the game code right in the mod files!
- Use Mercurial to review the changes.  Revert any changes that are not significant (like whitespace or something that does not affect logic)
- Commit
- Tag this commit with the new game version.
- Switch to the __default__ branch
- Merge the __original-game__ branch into the __default__ branch
    - This will cause Mercurial to merge any game code updates with your mod's modified version of the code.
    - In many cases Mercurial will be able to automatically merge everything with no work for you :)
    - Only of the game code changed the same spots that your mod changes will you get a merge conflict.
    - If you get a merge conflict, use the diff tools to sort it out.
    - Once you resolve any conflicts, the merge will be committed
- Switch the Visual Studio Build Configuration to __Debug__
- Now fix any build errors and then build and run the new version and test the mods


### Licence: ###
If you make a mod or an improved framework based on this project, please share the sources. I believe this type of license is called **CC BY-SA**.

While it is possible to create separate .exe files for different mods, this is not recommended, because as soon as you have two mods modifying the same methods, they will become incompatible. In order to avoid this situation, I strongly urge you to make all your mods a part of this project. Commit your mods to this repository and don't be afraid to go into other people's mods for the purposes of compatibility.

### Contact: ###
Comment on nexusmods, or comment/message here on bitbucket