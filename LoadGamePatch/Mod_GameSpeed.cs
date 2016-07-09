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
        if (this.TimeScale == this.FastTime)
        {
            this.TimeScale = this.NormalTime;
        }
        else if (!GameState.InCombat)
        {
            this.TimeScale = this.FastTime;
        }
        this.UpdateTimeScale();
    }
    public void ToggleSlowNew()
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
    private void UpdateNew()
    {
        this.RealtimeSinceStartupThisFrame = Time.realtimeSinceStartup;
        if (GameState.InCombat && this.TimeScale == this.FastTime)
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
            else if (GameInput.GetControlDown(MappedControl.SLOW_TOGGLE, true))
            {
                this.ToggleSlow();
            }
            else if (GameInput.GetControlDown(MappedControl.FAST_TOGGLE, true))
            {
                this.ToggleFast();
            }
            else if (GameInput.GetControlDown(MappedControl.GAME_SPEED_CYCLE, true))
            {
                if (this.Fast)
                {
                    this.Slow = true;
                }
                else if (this.Slow)
                {
                    this.Slow = false;
                }
                else if (GameState.InCombat)
                {
                    this.Slow = true;
                }
                else
                {
                    this.Fast = true;
                }
            }
        }
    }
}
#endif
