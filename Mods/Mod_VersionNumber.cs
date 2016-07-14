using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

#if !FIRSTRUN
public class Mod_VersionNumber : UIVersionNumber
{
	public static void main()
	{
		CecilImporter.PatchExistingMethod (typeof(Mod_VersionNumber), "UIVersionNumber", ".ctor", "CtorNew");
	}

	public void CtorNew()
	{
        this.FormatString = "v{0}.{1}.{2} {3}";
        this.FormatString += " - IEModLite 1.1.0";
		this.m_stringBuilder = new StringBuilder();
	}
}
#endif