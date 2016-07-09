using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Runtime.CompilerServices;
using Mono.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class CecilImporter
{
	public delegate TResult Func<TIn, TResult>(TIn param);

    // Match names like "Mod1" "Mod10" "Mod11"
    // also match names like "Mod_Whatever"
    static Regex ModRegex = new Regex(@"^Mod(?:(?:\d+)|(?:_.*))$");

#if LOADGAMEPATCH
    public struct FilePatch
    {
        public string file;
        public string newCode;
        public int firstLine;
        public int lastLine;
    }

    public static List<FilePatch> Patches = new List<FilePatch>();
    public static HashSet<string> IgnoreMethods = new HashSet<string>();

    public static void WriteFilePatches()
    {
        foreach (var fileGroup in Patches.GroupBy(p => p.file))
        {
            var file = fileGroup.Key;
            var fileContents = System.IO.File.ReadAllLines(file);
            var newContents = new List<string>();
            var nextLine = 1;
            foreach (var patch in fileGroup.OrderBy(p => p.firstLine))
            {
                while (nextLine < patch.firstLine)
                {
                    newContents.Add(fileContents[nextLine++ - 1]);
                }

                // if the old contents were tab-indented, then lets try to preserve that
                var useTabs = fileContents[nextLine - 1].StartsWith("\t");

                foreach (var n in patch.newCode.Split('\n'))
                {
                    if (n.Contains("base..ctor"))
                    {
                        continue;
                    }

                    var indentedLine = "    " + n;
                    if (useTabs)
                    {
                        indentedLine = indentedLine.Replace("    ", "\t");
                    }

                    newContents.Add(indentedLine);
                }
                nextLine = patch.lastLine + 1;
            }

            while (nextLine <= fileContents.Length)
            {
                newContents.Add(fileContents[nextLine++ - 1]);
            }
            System.IO.File.WriteAllLines(file/* + ".new"*/, newContents, System.Text.Encoding.UTF8);
        }
    }

    public class CustomCSharpLanguage : ICSharpCode.ILSpy.CSharpLanguage
    {
        public override void DecompileMethod(MethodDefinition method, ICSharpCode.Decompiler.ITextOutput output, ICSharpCode.ILSpy.DecompilationOptions options)
        {
            // code-gen instance constructors the same way you'd code-gen static constructors (with fields defined WITHIN the constructor instead of WITHOUT)

            //var astBuilder = this.CreateAstBuilder(options, null, method.DeclaringType, true);
            var createAstBuilder = typeof(ICSharpCode.ILSpy.CSharpLanguage).GetMethod("CreateAstBuilder", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var astBuilder = (ICSharpCode.Decompiler.Ast.AstBuilder)createAstBuilder.Invoke(this, new object[] { options, null, method.DeclaringType, true });

            astBuilder.AddMethod(method);

            //this.RunTransformsAndGenerateCode(astBuilder, output, options, null);
            var runTransformsAndGenerateCode = typeof(ICSharpCode.ILSpy.CSharpLanguage).GetMethod("RunTransformsAndGenerateCode", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            runTransformsAndGenerateCode.Invoke(this, new object[] { astBuilder, output, options, null });
        }
    }

#endif

	static void correctMethodReferences(MethodDefinition mtd)
	{
		foreach (Instruction inst in mtd.Body.Instructions)
		{
			MethodReference oldMtr = inst.Operand as MethodReference;
			if (oldMtr != null) // if we have a method reference in the operand
			{
				MethodReference newMtr = MainClass.mainMod.Import (oldMtr);
				if (newMtr.DeclaringType != null)
				{
                    Match match = ModRegex.Match(newMtr.DeclaringType.ToString());

					if (match.Success)
					{
						// we find what class our Mod4 (or whatever) inherits from
						string parentsClassName = newMtr.DeclaringType.Resolve ().BaseType.Name;
	
						// we find this class in the assembly
						TypeDefinition parent = MainClass.mainMod.Types.FirstOrDefault (x => x.Name == parentsClassName); 
	
						// and we find its Method with the same name and type
						MethodDefinition correctMeth = parent.Methods.FirstOrDefault (x => x.Name.ToString () == newMtr.Name.ToString () && x.MethodReturnType.ToString () == newMtr.MethodReturnType.ToString ());
	
						// and we import it
						newMtr = MainClass.mainMod.Import (correctMeth);
	
					}
				}

				if (newMtr != null) // we reimport it from our main assembly
					inst.Operand = newMtr;
			}
		}
	}

	static void addVariablesToMethod(MethodDefinition mtdFrom, MethodDefinition mtdTo)
	{
		mtdTo.Body.Variables.Clear ();
	
		foreach (VariableDefinition def in mtdFrom.Body.Variables)
		{
			mtdTo.Body.Variables.Add (def);
		}
	}

	static void correctVariableReferences(MethodDefinition mtd)
	{
		foreach (VariableDefinition vd in mtd.Body.Variables)
		{
			TypeReference oldVd = vd.VariableType;
			if (oldVd != null)
			{
				TypeReference newTr = MainClass.mainMod.Import (oldVd);

				if (newTr.DeclaringType != null)
				{
                    Match match = ModRegex.Match(newTr.DeclaringType.ToString());

					if (match.Success)
					{
						// we find what class our Mod4 (or whatever) inherits from
						string parentsClassName = newTr.DeclaringType.Resolve ().BaseType.ToString ();

						// we find its type
						TypeDefinition parent = MainClass.mainMod.Types.FirstOrDefault (x => x.Name == parentsClassName); 

						// we find the needed nested type
						TypeDefinition nestedType = parent.NestedTypes.FirstOrDefault (x => x.Name.ToString () == newTr.Name.ToString ());

						// and we import it for this field
						newTr = MainClass.mainMod.Import (nestedType);
					}
				}


				if (newTr != null)
					vd.VariableType = newTr;
			}
		}
	}

	public static void MakeFieldPublic(string ParentClass, string FieldName)
	{
		TypeDefinition typeDefInAssemblyToPatch = MainClass.mainMod.Types.FirstOrDefault (x => x.Name == ParentClass); 
		FieldDefinition field = typeDefInAssemblyToPatch.Fields.FirstOrDefault (x => x.Name == FieldName);
		field.IsPublic = true;
	}

	public static void MakePropertyPublic(string ParentClass, string PropertyName)
	{
		TypeDefinition typeDefInAssemblyToPatch = MainClass.mainMod.Types.FirstOrDefault (x => x.Name == ParentClass); 
		PropertyDefinition property = typeDefInAssemblyToPatch.Properties.FirstOrDefault (x => x.Name == PropertyName);

		if (property.GetMethod != null)
			property.GetMethod.IsPublic = true;
		if (property.SetMethod != null)
			property.SetMethod.IsPublic = true;
	}

	public static void MakeMethodPublic(string ParentClass, string MethodName)
	{
		TypeDefinition typeDefInAssemblyToPatch = MainClass.mainMod.Types.FirstOrDefault (x => x.Name == ParentClass); 
		MethodDefinition method = typeDefInAssemblyToPatch.Methods.FirstOrDefault (x => x.Name == MethodName);
		method.IsPublic = true;
	}

	// injects a void public static method
	public static void InjectMethodIntoType(string ParentClass, string MethodName, string attributes = "PublicStatic")
	{
#if LOADGAMEPATCH
        IgnoreMethods.Add(ParentClass + "." + MethodName);
#else
		TypeDefinition typeDefInAssemblyToPatch = MainClass.mainMod.Types.FirstOrDefault (x => x.Name == ParentClass); 
		MethodDefinition newMethod;
		if (attributes == "PublicStatic")
		{
			newMethod = new MethodDefinition (MethodName, MethodAttributes.Public | MethodAttributes.Static, MainClass.mainMod.Import (typeof(void)));
			typeDefInAssemblyToPatch.Methods.Add (newMethod);
		}
        else if (attributes == "Private")
		{
			newMethod = new MethodDefinition (MethodName, MethodAttributes.Private, MainClass.mainMod.Import (typeof(void)));
			typeDefInAssemblyToPatch.Methods.Add (newMethod);
		}
        else if (attributes == "Public")
        {
            newMethod = new MethodDefinition(MethodName, MethodAttributes.Public, MainClass.mainMod.Import(typeof(void)));
            typeDefInAssemblyToPatch.Methods.Add(newMethod);
        }
#endif
	}

	// not finished
	public static void InjectConstructorIntoType(string ParentClass)
	{
#if LOADGAMEPATCH
        return;
#else
		TypeDefinition typeDefInAssemblyToPatch = MainClass.mainMod.Types.FirstOrDefault (x => x.Name == ParentClass); 

		//TODO: should be static if class is static...
		MethodDefinition newMethod = new MethodDefinition (".cctor", MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName , MainClass.mainMod.Import (typeof(void)));

		//MethodDefinition baseConstructor = Extensions.FirstOrDefault<MethodDefinition> (typeDefInAssemblyToPatch.Methods, m => m.Name == ".ctor"); 

		//newMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
		//newMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, MainClass.mainMod.Import(baseConstructor)));
		newMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
		typeDefInAssemblyToPatch.Methods.Add (newMethod);
#endif
	}

	static void CopyAttributes(MethodDefinition originalMtd, MethodDefinition newMtd)
	{
		newMtd.Attributes = originalMtd.Attributes;
	}

	public static void PatchExistingMethod(Type type, string classNameToPatch, string methodNameToPatch, string copyInstructionsFromMethod, int index = 0)
	{
		TypeDefinition typeDefInAssemblyToPatch = MainClass.mainMod.Types.FirstOrDefault (x => x.Name == classNameToPatch);
        AssemblyDefinition currentAsm = AssemblyDefinition.ReadAssembly(type.Assembly.Location, new ReaderParameters { ReadSymbols = true });
		TypeDefinition targetType = Extensions.FirstOrDefault<TypeDefinition> (currentAsm.MainModule.Types, t => t.FullName == type.FullName);
		MethodDefinition originalMtd = Extensions.FirstOrDefault<MethodDefinition> (targetType.Methods, m => m.Name == copyInstructionsFromMethod); // this is the method with our custom code
        var newMtds = typeDefInAssemblyToPatch.Methods.Where(x => x.Name == methodNameToPatch).ToList();
        MethodDefinition newMtd = newMtds.Count > index ? newMtds[index] : null; // this is the method that we're going to patch... need to find better names...

#if LOADGAMEPATCH
        if (newMtd == null || IgnoreMethods.Contains(classNameToPatch + "." + methodNameToPatch))
        {
            // must be an injected method?
            return;
        }

        var lang = new CustomCSharpLanguage();
        var decompilerSettings = new ICSharpCode.Decompiler.DecompilerSettings { ShowXmlDocumentation = false };
        var decompilerOptions = new ICSharpCode.ILSpy.DecompilationOptions { DecompilerSettings = decompilerSettings };
        var output = new ICSharpCode.Decompiler.PlainTextOutput();
        lang.DecompileMethod(newMtd, output, decompilerOptions);
        var methodCode = output.ToString();
        // replace tabs with spaces
        methodCode = methodCode.Replace("\t", "    ");
        // remove "\r"
        methodCode = methodCode.Replace("\r", "");
        // remove final "\n"
        while (methodCode.EndsWith("\n"))
        {
            methodCode = methodCode.Substring(0, methodCode.Length - 1);
        }

        // remove top comment line
        if (methodCode.StartsWith("//"))
        {
            methodCode = methodCode.Substring(methodCode.IndexOf('\n') + 1);
        }

        // Remove any attributes
        var attrRE = new Regex(@"^(?:\[[^]]+]\s*){1,}");
        methodCode = attrRE.Replace(methodCode, "", 1);

        // change the method name to the mod's name for the method, and replace parameter names with game names
        var methodName = newMtd.IsConstructor ? classNameToPatch : methodNameToPatch;
        var nameLocation = methodCode.IndexOf(" " + methodName) + 1;
        var nameEnd = nameLocation + methodName.Length;

        // Prepend "void " if this was a constructor (since methodCode won't have a return type)
        var correctName = newMtd.IsConstructor ? ("void " + copyInstructionsFromMethod) : copyInstructionsFromMethod;
        methodCode = methodCode.Substring(0, nameLocation) + correctName + methodCode.Substring(nameEnd);

        // Remove 'override' keyword if source method is not virtual...
        if (!originalMtd.IsVirtual)
        {
            var firstLine = methodCode.IndexOf('\n');
            var o = methodCode.IndexOf(" override ");
            if (o != -1 && o < firstLine)
            {
                methodCode = methodCode.Substring(0, o + 1) + methodCode.Substring(o + 10);
            }
        }

        var firstSP = originalMtd.Body.Instructions.First(i => i.SequencePoint != null).SequencePoint;
        var lastSP = originalMtd.Body.Instructions.Last(i => i.SequencePoint != null).SequencePoint;
        Patches.Add(new FilePatch
        {
            file = firstSP.Document.Url,
            firstLine = firstSP.StartLine - 1,
            lastLine = lastSP.EndLine,
            newCode = methodCode
        });
#else
		newMtd.ReturnType = MainClass.mainMod.Import (originalMtd.ReturnType);

		newMtd.Body.Instructions.Clear ();


		newMtd.Body.ExceptionHandlers.Clear ();

		if (originalMtd.Body.HasExceptionHandlers)
		{
			foreach (var exhandlr in originalMtd.Body.ExceptionHandlers)
			{
				newMtd.Body.ExceptionHandlers.Add (exhandlr);
			}

			foreach (var exhandlr in newMtd.Body.ExceptionHandlers)
			{
				if(exhandlr.CatchType != null)
					exhandlr.CatchType = MainClass.mainMod.Import (exhandlr.CatchType);
			}
		}

		foreach (Instruction inst in originalMtd.Body.Instructions)
			newMtd.Body.Instructions.Add (inst);

		//CopyAttributes (originalMtd, newMtd);

		AddParamsToMethod (originalMtd, newMtd);
		correctParamReferences (newMtd);

		addVariablesToMethod (originalMtd, newMtd);
		correctVariableReferences (newMtd);

		correctMethodReferences (newMtd); // in operands
		correctFieldReferencesInMtd (newMtd); // in operands

		correctNestedTypeReferencesInMtd (newMtd); // in operands... also deals with incorrect types due to casting, like ((Mod7)GameState.s_playerCharacter).showModelViewer

		//TODO: manage custom attributes
#endif
    }

	public static void PatchExistingProperty(Type type, string classNameToPatch, string propertyNameToPatch, string copyFromProperty)
	{
#if LOADGAMEPATCH
        System.Console.WriteLine("Properties not supported.  Please get yourself {0}.{1}", classNameToPatch, propertyNameToPatch);
#else

		TypeDefinition typeDefInAssemblyToPatch = MainClass.mainMod.Types.FirstOrDefault (x => x.Name == classNameToPatch); 

		AssemblyDefinition currentAsm = AssemblyDefinition.ReadAssembly (type.Assembly.Location);
		TypeDefinition targetType = Extensions.FirstOrDefault<TypeDefinition> (currentAsm.MainModule.Types, t => t.FullName == type.FullName);


		PropertyDefinition ourAssemblyProperty = Extensions.FirstOrDefault<PropertyDefinition> (targetType.Properties, m => m.Name == copyFromProperty); 
		PropertyDefinition targetAssemblyProperty = typeDefInAssemblyToPatch.Properties.FirstOrDefault (x => x.Name == propertyNameToPatch);

		MethodDefinition ourGetMethod = ourAssemblyProperty.GetMethod;
		MethodDefinition targetGetMethod = targetAssemblyProperty.GetMethod;

		MethodDefinition ourSetMethod = ourAssemblyProperty.SetMethod;
		MethodDefinition targetSetMethod = targetAssemblyProperty.SetMethod;

		if (ourGetMethod != null)
		{
            PatchPropertyMethod(ourGetMethod, targetGetMethod);
		}

		if (ourSetMethod != null)
		{
            PatchPropertyMethod(ourSetMethod, targetSetMethod);
		}
#endif
	}

    private static void PatchPropertyMethod(MethodDefinition ourMethod, MethodDefinition targetMethod)
    {
        targetMethod.Body.Instructions.Clear();
        foreach (Instruction inst in ourMethod.Body.Instructions)
            targetMethod.Body.Instructions.Add(inst);

        AddParamsToMethod(ourMethod, targetMethod);
        correctParamReferences(targetMethod);

        addVariablesToMethod(ourMethod, targetMethod);
        correctVariableReferences(targetMethod);

        correctMethodReferences(targetMethod); // in operands
        correctFieldReferencesInMtd(targetMethod); // in operands

        correctNestedTypeReferencesInMtd(targetMethod);
    }

	static void correctParamReferences(MethodDefinition mtd)
	{
		foreach (ParameterDefinition pd in mtd.Parameters)
		{
			TypeReference oldPd = pd.ParameterType;
			if (oldPd != null)
			{
				TypeReference newPd = MainClass.mainMod.Import (oldPd);
		
				if (newPd.DeclaringType != null)
				{
                    Match match = ModRegex.Match(newPd.DeclaringType.ToString());

					if (match.Success)
					{
						// we find what class our Mod4 (or whatever) inherits from
						string parentsClassName = newPd.DeclaringType.Resolve ().BaseType.ToString ();

						// we find its type
						TypeDefinition parent = MainClass.mainMod.Types.FirstOrDefault (x => x.Name == parentsClassName); 

						// we find the needed nested type
						TypeDefinition nestedType = parent.NestedTypes.FirstOrDefault (x => x.Name.ToString () == newPd.Name.ToString ());

						// and we import it for this field
						newPd = MainClass.mainMod.Import (nestedType);
					}
				}

				if (newPd != null)
					pd.ParameterType = newPd;
			}
		}
	}

	static void AddParamsToMethod(MethodDefinition originalMtd, MethodDefinition newMtd)
	{
		newMtd.Parameters.Clear ();
		foreach (ParameterDefinition param in originalMtd.Parameters)
		{
			newMtd.Parameters.Add (param);
		}
	}

	static void correctFieldReferencesInMtd(MethodDefinition mtd)
	{
		foreach (Instruction inst in mtd.Body.Instructions)
		{
			FieldReference oldFr = inst.Operand as FieldReference;
			if (oldFr != null) // if we have a field reference in the operand
			{
				FieldReference newFr = MainClass.mainMod.Import (oldFr);

				// if we've got a FieldReference that is like Mod4::test instead of ProperClass::test (which happens when we use local fields, 
				// because they don't exist in the Assembly yet, since we're injecting them as well), then we need to assign them the correct Class
				if (newFr.DeclaringType != null)
				{
                    Match match = ModRegex.Match(newFr.DeclaringType.ToString());

					if (match.Success)
					{
						// we find what class our Mod4 (or whatever) inherits from
						string parentsClassName = newFr.DeclaringType.Resolve ().BaseType.ToString ();

						// we find this class in the assembly
						TypeDefinition parent = MainClass.mainMod.Types.FirstOrDefault (x => x.Name == parentsClassName); 

						// and we find its Field with the same name and type
						FieldDefinition correctField = parent.Fields.FirstOrDefault (x => x.Name.ToString () == newFr.Name.ToString () && x.FieldType.ToString () == newFr.FieldType.ToString ());

						// and we import it
						newFr = MainClass.mainMod.Import (correctField);

					}
				}

				if (newFr != null) // we reimport it from our main assembly
					inst.Operand = newFr;

			}
		}
	}

	static void correctNestedTypeReferencesInMtd(MethodDefinition mtd)
	{
		foreach (Instruction inst in mtd.Body.Instructions)
		{
			TypeReference oldTr = inst.Operand as TypeReference;
			if (oldTr != null) // if we have a type reference in the operand
			{
				TypeReference newTr = MainClass.mainMod.Import (oldTr); // if we're importing a type that isn't defined in the mainmod... like Mod7... then what?
	
				if (newTr.DeclaringType != null)
				{
                    Match match = ModRegex.Match(newTr.DeclaringType.ToString());

					if (match.Success)
					{
						// we find what class our Mod4 (or whatever) inherits from
						string parentsClassName = newTr.DeclaringType.Resolve ().BaseType.ToString ();

						// we find this class
						TypeDefinition parent = MainClass.mainMod.Types.FirstOrDefault (x => x.Name == parentsClassName); 

						// we find the needed nested type
						TypeDefinition nestedType = parent.NestedTypes.FirstOrDefault (x => x.Name.ToString () == newTr.Name.ToString ());

						// and we import it for this field
						newTr = MainClass.mainMod.Import (nestedType);

					}
				}
				else {
                    Match match = ModRegex.Match(oldTr.ToString());
					if (match.Success)
					{
						string parentsClassName = oldTr.Resolve ().BaseType.ToString (); // find who we inherited from
						TypeDefinition parent = MainClass.mainMod.Types.FirstOrDefault (x => x.Name == parentsClassName || x.FullName == parentsClassName);
						newTr = MainClass.mainMod.Import (parent);
					}

				}

				if (newTr != null) // we reimport it from our main assembly
					inst.Operand = newTr;

			}
		}
	}

	static void correctFieldReferencesInType(TypeDefinition td)
	{
		for (int i = 0; i < td.Fields.Count; i++)
		{
			var newFr = MainClass.mainMod.Import (td.Fields [i].FieldType);


			if (newFr != null)
			{
				td.Fields [i].FieldType = newFr;
			}
		}
	}

	static void correctFieldReferencesInNestedType(TypeDefinition td)
	{
		for (int i = 0; i < td.Fields.Count; i++)
		{
			var newFr = MainClass.mainMod.Import (td.Fields [i].FieldType);



			if (newFr.DeclaringType != null)
			{
                Match match = ModRegex.Match(newFr.DeclaringType.ToString());

				if (match.Success)
				{
					// we find what class our Mod4 (or whatever) inherits from
					string parentsClassName = newFr.DeclaringType.Resolve ().BaseType.ToString ();

					// we find its type
					TypeDefinition parent = MainClass.mainMod.Types.FirstOrDefault (x => x.Name == parentsClassName); 

					// we find the needed nested type
					TypeDefinition nestedType = parent.NestedTypes.FirstOrDefault (x => x.Name.ToString () == newFr.Name.ToString ());

					// and we import it for this field
					newFr = MainClass.mainMod.Import (nestedType);
				}

			}

			if (newFr != null)
			{
				td.Fields [i].FieldType = newFr;
			}
		}
	}
		
	public static void InjectFieldIntoType(Type type, string classNameToPatch, string FieldNameToInject)
	{
#if LOADGAMEPATCH
        return;
#else
		TypeDefinition typeDefInAssemblyToPatch = MainClass.mainMod.Types.FirstOrDefault (x => x.Name == classNameToPatch); 
		AssemblyDefinition currentAsm = AssemblyDefinition.ReadAssembly (type.Assembly.Location);
		TypeDefinition targetType = Extensions.FirstOrDefault<TypeDefinition> (currentAsm.MainModule.Types, t => t.FullName == type.FullName);

		FieldDefinition newFd = Extensions.FirstOrDefault<FieldDefinition> (targetType.Fields, m => m.Name == FieldNameToInject); 

		FieldDefinition toAdd = new FieldDefinition (newFd.Name, newFd.Attributes, newFd.FieldType);

		typeDefInAssemblyToPatch.Fields.Add (toAdd);

		correctFieldReferencesInType (typeDefInAssemblyToPatch);
		correctFieldReferencesInNestedType (typeDefInAssemblyToPatch);
#endif
	}

	public static void RemoveFieldFromType(string className, string FieldName)
	{
		TypeDefinition typeDefInAssemblyToPatch = MainClass.mainMod.Types.FirstOrDefault (x => x.Name == className); 
		FieldDefinition toremove = typeDefInAssemblyToPatch.Fields.FirstOrDefault (x => x.Name == FieldName);			
		typeDefInAssemblyToPatch.Fields.Remove (toremove);
	}

	public static void RemoveMethodFromType(string className, string methodName)
	{
		TypeDefinition typeDefInAssemblyToPatch = MainClass.mainMod.Types.FirstOrDefault (x => x.Name == className); 
		MethodDefinition toremove = typeDefInAssemblyToPatch.Methods.FirstOrDefault (x => x.Name == methodName);			
		typeDefInAssemblyToPatch.Methods.Remove (toremove);
	}

	public static void PatchExistingNestedType(Type type, string classNameToPatch, string NestedTypeToPatch, string NestedTypeToPatchFrom)
	{
		TypeDefinition typeDefInAssemblyToPatch = MainClass.mainMod.Types.FirstOrDefault (x => x.Name == classNameToPatch); 
		TypeDefinition nestedDefInAssemblyToPatch = typeDefInAssemblyToPatch.NestedTypes.FirstOrDefault (x => x.Name == NestedTypeToPatch);

		AssemblyDefinition currentAsm = AssemblyDefinition.ReadAssembly (type.Assembly.Location);
		TypeDefinition targetType = Extensions.FirstOrDefault<TypeDefinition> (currentAsm.MainModule.Types, t => t.FullName == type.FullName);
		TypeDefinition targetNestedType = Extensions.FirstOrDefault<TypeDefinition> (targetType.NestedTypes, t => t.Name == NestedTypeToPatchFrom);

#if LOADGAMEPATCH
        System.Console.WriteLine("Warning: retrieving enums not supported.  Please retrieve yourself. {0}", nestedDefInAssemblyToPatch.FullName);
#else
		nestedDefInAssemblyToPatch.Fields.Clear ();

		foreach (FieldDefinition fd in targetNestedType.Fields)
		{
			nestedDefInAssemblyToPatch.Fields.Add (new FieldDefinition (fd.Name, fd.Attributes, fd.FieldType));
			nestedDefInAssemblyToPatch.Fields.Last ().Constant = fd.Constant;
		}
			
		correctFieldReferencesInNestedType (nestedDefInAssemblyToPatch);
#endif
	}

}

public static class Extensions
{
	public static T FirstOrDefault<T>(Collection<T> array, CecilImporter.Func<T, bool> condition)
	{
		foreach (T local in array)
		{
			if (condition(local))
			{
				return local;
			}
		}
		return default(T);
	}
}
