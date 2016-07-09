using System.Collections;
using System.Collections.Generic;
using Mono.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

#if !FIRSTRUN

public class Mod_GameSpeed : TimeController
{
    public static void main()
    {
        CecilImporter.PatchExistingMethod(typeof(Mod_GameSpeed), "TimeController", "ToggleFast", "ToggleFastNew");
        CecilImporter.PatchExistingMethod(typeof(Mod_GameSpeed), "TimeController", "ToggleSlow", "ToggleSlowNew");
        CecilImporter.PatchExistingMethod(typeof(Mod_GameSpeed), "TimeController", "Update", "UpdateNew");
    }
    public void ToggleFastNew()
    {
        if (GameInput.GetControlkey())
        {
            if (GameInput.GetShiftkey())
            {
                foreach (var partymember in PartyMemberAI.GetSelectedPartyMembers())
                {
                    partymember.transform.position = GameInput.WorldMousePosition;
                }
            }
            else
            {
                if (this.TimeScale == 5.0f)
                {
                    this.TimeScale = this.NormalTime;
                }
                else
                {
                    this.TimeScale = 5.0f;
                }
                this.UpdateTimeScale();
            }
        }
        else if (!GameInput.GetControlkey()) //default behavior is that ctrl+d does not trigger fast mode toggle
        {
            if (this.TimeScale == this.FastTime)
            {
                this.TimeScale = this.NormalTime;
            }
            else
            {
                this.TimeScale = this.FastTime;
            }
            this.UpdateTimeScale();
        }
        this.UpdateTimeScale();
    }
    public void ToggleSlowNew()
    {
        if (GameInput.GetControlkey())
        {
            if (this.TimeScale == 0.16f)
            {
                this.TimeScale = this.NormalTime;
            }
            else
            {
                this.TimeScale = 0.16f;
            }
            this.UpdateTimeScale();
        }
        else if (!GameInput.GetControlkey())
        {
            if (this.TimeScale == this.SlowTime)
            {
                this.TimeScale = this.NormalTime;
            }
            else
            {
                this.TimeScale = this.SlowTime;
            }
            this.UpdateTimeScale();
        }
        this.UpdateTimeScale();
    }
    private void UpdateNew()
    {
        if (GameState.InCombat && this.TimeScale > this.NormalTime)
        {
            this.TimeScale = 1f;
        }
        if (!GameState.IsLoading)
        {
            this.UpdateTimeScale();
        }
        if (UIWindowManager.KeyInputAvailable)
        {
            if (GameInput.GetControlDown(MappedControl.RESTORE_SPEED, true))
            {
                this.TimeScale = this.NormalTime;
            }
            else if (GameInput.GetControlDownWithoutModifiers(MappedControl.SLOW_TOGGLE))
            {
                this.ToggleSlow();
            }
            else if (!GameState.InCombat && GameInput.GetControlDownWithoutModifiers(MappedControl.FAST_TOGGLE))
            {
                this.ToggleFast();
            }
        }
    }
}
#endif