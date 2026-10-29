Add-Type -Path "D:\Projects\overthehillvr-fan\MelonLoader\net6\Mono.Cecil.dll"
Add-Type -Path "D:\Projects\overthehillvr-fan\MelonLoader\net6\Mono.Cecil.Rocks.dll"

$dllPath = "D:\Projects\overthehillvr-fan\Mods\OverTheHillVR.dll"
$bakPath = "D:\Projects\overthehillvr-fan\Mods\OverTheHillVR.dll.pre_safety_bak"

# Always restore clean pre_safety_bak first to guarantee pristine source
if (Test-Path $bakPath) {
    Copy-Item $bakPath $dllPath -Force
    Write-Output "Restored from clean pre_safety backup"
} else {
    Copy-Item $dllPath $bakPath -Force
    Write-Output "Created initial backup at $bakPath"
}

$rawBytes = [System.IO.File]::ReadAllBytes($dllPath)
$memStream = New-Object System.IO.MemoryStream($rawBytes, $true)
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($memStream)
$mod = $asm.MainModule
$driverType = $mod.GetType("OverTheHillVR.VrDriver")

Write-Output "Patching OverTheHillVR.VrDriver for Fanatec Direct Drive safeties..."

# 1. Add fields: isDirectDrive (bool) and ffbLastTick (float)
$f_isDD = New-Object Mono.Cecil.FieldDefinition("isDirectDrive", [Mono.Cecil.FieldAttributes]::Private, $mod.TypeSystem.Boolean)
$driverType.Fields.Add($f_isDD)
Write-Output "Added field: isDirectDrive"

$f_lastTick = New-Object Mono.Cecil.FieldDefinition("ffbLastTick", [Mono.Cecil.FieldAttributes]::Private, $mod.TypeSystem.Single)
$driverType.Fields.Add($f_lastTick)
Write-Output "Added field: ffbLastTick"

# 2. Add SafeFfb static method
$attrs = [Mono.Cecil.MethodAttributes]([int][Mono.Cecil.MethodAttributes]::Public -bor [int][Mono.Cecil.MethodAttributes]::Static)
$m_safeFfb = New-Object Mono.Cecil.MethodDefinition("SafeFfb", $attrs, $mod.TypeSystem.Int32)
$p_targetConst = New-Object Mono.Cecil.ParameterDefinition("targetConst", [Mono.Cecil.ParameterAttributes]::None, $mod.TypeSystem.Int32)
$p_lastConst = New-Object Mono.Cecil.ParameterDefinition("lastConst", [Mono.Cecil.ParameterAttributes]::None, $mod.TypeSystem.Int32)
$p_dt = New-Object Mono.Cecil.ParameterDefinition("dt", [Mono.Cecil.ParameterAttributes]::None, $mod.TypeSystem.Single)
$p_driving = New-Object Mono.Cecil.ParameterDefinition("driving", [Mono.Cecil.ParameterAttributes]::None, $mod.TypeSystem.Boolean)
$p_focused = New-Object Mono.Cecil.ParameterDefinition("focused", [Mono.Cecil.ParameterAttributes]::None, $mod.TypeSystem.Boolean)
$p_isDD = New-Object Mono.Cecil.ParameterDefinition("isDD", [Mono.Cecil.ParameterAttributes]::None, $mod.TypeSystem.Boolean)

$m_safeFfb.Parameters.Add($p_targetConst)
$m_safeFfb.Parameters.Add($p_lastConst)
$m_safeFfb.Parameters.Add($p_dt)
$m_safeFfb.Parameters.Add($p_driving)
$m_safeFfb.Parameters.Add($p_focused)
$m_safeFfb.Parameters.Add($p_isDD)

$m_safeFfb.Body.InitLocals = $true
$m_safeFfb.Body.Variables.Add((New-Object Mono.Cecil.Cil.VariableDefinition($mod.TypeSystem.Int32))) # 0: baseline
$m_safeFfb.Body.Variables.Add((New-Object Mono.Cecil.Cil.VariableDefinition($mod.TypeSystem.Int32))) # 1: maxDelta
$m_safeFfb.Body.Variables.Add((New-Object Mono.Cecil.Cil.VariableDefinition($mod.TypeSystem.Int32))) # 2: delta

$il = $m_safeFfb.Body.GetILProcessor()
$lbl_retZero = $il.Create([Mono.Cecil.Cil.OpCodes]::Ldc_I4_0)
$lbl_skipDD = $il.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_1)
$lbl_notInit = $il.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_1)
$lbl_storeBase = $il.Create([Mono.Cecil.Cil.OpCodes]::Stloc_0)
$lbl_skipMin = $il.Create([Mono.Cecil.Cil.OpCodes]::Conv_I4)
$lbl_checkNeg = $il.Create([Mono.Cecil.Cil.OpCodes]::Ldloc_2)
$lbl_clampBounds = $il.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$lbl_checkMin = $il.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$lbl_retTarget = $il.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0)

# If (!driving) return 0;
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_3)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Brfalse, $lbl_retZero)
# If (!focused) return 0;
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg, $p_focused)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Brfalse, $lbl_retZero)

# If (isDD) targetConst = (int)(targetConst * 0.65f);
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg, $p_isDD)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Brfalse, $lbl_skipDD)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Conv_R4)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_R4, [float]0.65)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Mul)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Conv_I4)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Starg, $p_targetConst)

# Baseline calc
$il.Append($lbl_skipDD)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4, [int]99999)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Bne_Un, $lbl_notInit)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Br, $lbl_storeBase)
$il.Append($lbl_notInit)
$il.Append($lbl_storeBase)

# Max slew delta: (int)max(50f, 12000f * dt)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_R4, [float]12000.0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_2)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Mul)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Dup)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_R4, [float]50.0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Bge_Un, $lbl_skipMin)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Pop)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_R4, [float]50.0)
$il.Append($lbl_skipMin)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Stloc_1)

# Delta: targetConst - baseline
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldloc_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Sub)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Stloc_2)

# Slew limit clamp
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldloc_2)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldloc_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ble, $lbl_checkNeg)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldloc_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldloc_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Add)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Starg, $p_targetConst)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Br, $lbl_clampBounds)

$il.Append($lbl_checkNeg)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldloc_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Neg)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Bge, $lbl_clampBounds)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldloc_0)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldloc_1)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Sub)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Starg, $p_targetConst)

# Hardware boundary clamp [-8500, 8500]
$il.Append($lbl_clampBounds)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4, [int]8500)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ble, $lbl_checkMin)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4, [int]8500)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Starg, $p_targetConst)

$il.Append($lbl_checkMin)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4, [int]-8500)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Bge, $lbl_retTarget)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4, [int]-8500)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Starg, $p_targetConst)

$il.Append($lbl_retTarget)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)

$il.Append($lbl_retZero)
$il.Emit([Mono.Cecil.Cil.OpCodes]::Ret)

$driverType.Methods.Add($m_safeFfb)
Write-Output "Added method: SafeFfb"

# 3. Add SafeDamper static method
$m_safeDamper = New-Object Mono.Cecil.MethodDefinition("SafeDamper", $attrs, $mod.TypeSystem.Int32)
$p_targetDamper = New-Object Mono.Cecil.ParameterDefinition("targetDamper", [Mono.Cecil.ParameterAttributes]::None, $mod.TypeSystem.Int32)
$p_dampIsDD = New-Object Mono.Cecil.ParameterDefinition("isDD", [Mono.Cecil.ParameterAttributes]::None, $mod.TypeSystem.Boolean)
$p_dampDriving = New-Object Mono.Cecil.ParameterDefinition("driving", [Mono.Cecil.ParameterAttributes]::None, $mod.TypeSystem.Boolean)

$m_safeDamper.Parameters.Add($p_targetDamper)
$m_safeDamper.Parameters.Add($p_dampIsDD)
$m_safeDamper.Parameters.Add($p_dampDriving)

$ilD = $m_safeDamper.Body.GetILProcessor()
$lbl_dampCheckDD = $ilD.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_1)
$lbl_dampClampMax = $ilD.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$lbl_dampRet = $ilD.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0)

# if (!driving) return 0;
$ilD.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_2)
$ilD.Emit([Mono.Cecil.Cil.OpCodes]::Brtrue, $lbl_dampCheckDD)
$ilD.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_0)
$ilD.Emit([Mono.Cecil.Cil.OpCodes]::Ret)

# if (isDD)
$ilD.Append($lbl_dampCheckDD)
$ilD.Emit([Mono.Cecil.Cil.OpCodes]::Brfalse, $lbl_dampClampMax)
# if (targetDamper < 1200) targetDamper = 1200;
$ilD.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$ilD.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4, [int]1200)
$ilD.Emit([Mono.Cecil.Cil.OpCodes]::Bge, $lbl_dampClampMax)
$ilD.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4, [int]1200)
$ilD.Emit([Mono.Cecil.Cil.OpCodes]::Starg, $p_targetDamper)

# if (targetDamper > 10000) targetDamper = 10000;
$ilD.Append($lbl_dampClampMax)
$ilD.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4, [int]10000)
$ilD.Emit([Mono.Cecil.Cil.OpCodes]::Ble, $lbl_dampRet)
$ilD.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4, [int]10000)
$ilD.Emit([Mono.Cecil.Cil.OpCodes]::Starg, $p_targetDamper)

$ilD.Append($lbl_dampRet)
$ilD.Emit([Mono.Cecil.Cil.OpCodes]::Ret)

$driverType.Methods.Add($m_safeDamper)
Write-Output "Added method: SafeDamper"

# 4. Extract existing references from FfbTick
$m_ffbTick = $driverType.Methods | Where-Object { $_.Name -eq "FfbTick" }
$refConst = ($m_ffbTick.Body.Instructions | Where-Object { $_.Operand -ne $null -and $_.Operand.ToString() -like "*UpdateConstantForceSimple*" } | Select-Object -First 1).Operand
$refTime = ($m_ffbTick.Body.Instructions | Where-Object { $_.Operand -ne $null -and $_.Operand.ToString() -like "*get_unscaledTime*" } | Select-Object -First 1).Operand
$refDelta = ($m_ffbTick.Body.Instructions | Where-Object { $_.Operand -ne $null -and $_.Operand.ToString() -like "*get_unscaledDeltaTime*" } | Select-Object -First 1).Operand

# 5. Add WatchdogCheck method to VrDriver
$attrsW = [Mono.Cecil.MethodAttributes]([int][Mono.Cecil.MethodAttributes]::Private)
$m_watchdog = New-Object Mono.Cecil.MethodDefinition("WatchdogCheck", $attrsW, $mod.TypeSystem.Void)

$f_ffbReady = $driverType.Fields | Where-Object { $_.Name -eq "ffbReady" }
$f_ffbDev = $driverType.Fields | Where-Object { $_.Name -eq "ffbDev" }
$f_ffbLastConst = $driverType.Fields | Where-Object { $_.Name -eq "ffbLastConst" }

$ilW = $m_watchdog.Body.GetILProcessor()
$lbl_retW = $ilW.Create([Mono.Cecil.Cil.OpCodes]::Ret)

# if (!this.ffbReady) return;
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Ldfld, $f_ffbReady)
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Brfalse, $lbl_retW)

# if (this.ffbDev == null) return;
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Ldfld, $f_ffbDev)
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Brfalse, $lbl_retW)

# if (Time.unscaledTime - this.ffbLastTick <= 0.20f) return;
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Call, $refTime)
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Ldfld, $f_lastTick)
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Sub)
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_R4, [float]0.20)
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Ble_Un, $lbl_retW)

# if (this.ffbLastConst == 0) return;
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Ldfld, $f_ffbLastConst)
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Brfalse, $lbl_retW)

# DIManager.UpdateConstantForceSimple(this.ffbDev, 0);
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Ldfld, $f_ffbDev)
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_0)
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Call, $refConst)
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Pop)

# this.ffbLastConst = 0;
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Ldc_I4_0)
$ilW.Emit([Mono.Cecil.Cil.OpCodes]::Stfld, $f_ffbLastConst)

$ilW.Append($lbl_retW)

$driverType.Methods.Add($m_watchdog)
Write-Output "Added method: WatchdogCheck"

# 6. Patch Quit(): Add FfbStop() call before ret
$m_quit = $driverType.Methods | Where-Object { $_.Name -eq "Quit" }
$m_ffbStop = $driverType.Methods | Where-Object { $_.Name -eq "FfbStop" }
$lastInst = $m_quit.Body.Instructions[$m_quit.Body.Instructions.Count - 1]
if ($lastInst.OpCode.Name -eq "ret") {
    $ilQ = $m_quit.Body.GetILProcessor()
    $ins1 = $ilQ.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
    $ins2 = $ilQ.Create([Mono.Cecil.Cil.OpCodes]::Call, $m_ffbStop)
    $ilQ.InsertBefore($lastInst, $ins1)
    $ilQ.InsertBefore($lastInst, $ins2)
    Write-Output "Patched Quit(): Added FfbStop() teardown"
}

# 7. Patch Update(): Call WatchdogCheck() at start
$m_update = $driverType.Methods | Where-Object { $_.Name -eq "Update" }
$ilU = $m_update.Body.GetILProcessor()
$firstInst = $m_update.Body.Instructions[0]
$callW1 = $ilU.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$callW2 = $ilU.Create([Mono.Cecil.Cil.OpCodes]::Call, $m_watchdog)
$ilU.InsertBefore($firstInst, $callW1)
$ilU.InsertBefore($firstInst, $callW2)
Write-Output "Patched Update(): Injected WatchdogCheck()"

# 8. Patch FfbTick():
$ilTick = $m_ffbTick.Body.GetILProcessor()

# Inject ffbLastTick = Time.unscaledTime at beginning of FfbTick
$firstTickInst = $m_ffbTick.Body.Instructions[0]
$t1 = $ilTick.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
$t2 = $ilTick.Create([Mono.Cecil.Cil.OpCodes]::Call, $refTime)
$t3 = $ilTick.Create([Mono.Cecil.Cil.OpCodes]::Stfld, $f_lastTick)
$ilTick.InsertBefore($firstTickInst, $t1)
$ilTick.InsertBefore($firstTickInst, $t2)
$ilTick.InsertBefore($firstTickInst, $t3)

$m_gameFocused = $driverType.Methods | Where-Object { $_.Name -eq "GameFocused" }

# Find stloc.s V_5 where num20 is stored
for ($i = 0; $i -lt $m_ffbTick.Body.Instructions.Count; $i++) {
    $inst = $m_ffbTick.Body.Instructions[$i]
    if ($inst.OpCode.Name -eq "stloc.s" -and $inst.Operand.Index -eq 5) {
        Write-Output "Found num20 store at index $i"
        # num20 is on stack. Pass (num20, ffbLastConst, dt, flag, focused, isDD) to SafeFfb
        $p1 = $ilTick.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
        $p2 = $ilTick.Create([Mono.Cecil.Cil.OpCodes]::Ldfld, $f_ffbLastConst)
        $p3 = $ilTick.Create([Mono.Cecil.Cil.OpCodes]::Call, $refDelta)
        $p4 = $ilTick.Create([Mono.Cecil.Cil.OpCodes]::Ldloc_0) # flag (driving)
        $p5 = $ilTick.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
        $p6 = $ilTick.Create([Mono.Cecil.Cil.OpCodes]::Call, $m_gameFocused)
        $p7 = $ilTick.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
        $p8 = $ilTick.Create([Mono.Cecil.Cil.OpCodes]::Ldfld, $f_isDD)
        $p9 = $ilTick.Create([Mono.Cecil.Cil.OpCodes]::Call, $m_safeFfb)

        $ilTick.InsertBefore($inst, $p1)
        $ilTick.InsertBefore($inst, $p2)
        $ilTick.InsertBefore($inst, $p3)
        $ilTick.InsertBefore($inst, $p4)
        $ilTick.InsertBefore($inst, $p5)
        $ilTick.InsertBefore($inst, $p6)
        $ilTick.InsertBefore($inst, $p7)
        $ilTick.InsertBefore($inst, $p8)
        $ilTick.InsertBefore($inst, $p9)
        Write-Output "Patched FfbTick(): num20 filtered through SafeFfb with hardware limits"
        break
    }
}

# Find stloc.s V_6 where num21 (damper) is stored
for ($i = 0; $i -lt $m_ffbTick.Body.Instructions.Count; $i++) {
    $inst = $m_ffbTick.Body.Instructions[$i]
    if ($inst.OpCode.Name -eq "stloc.s" -and $inst.Operand.Index -eq 6) {
        Write-Output "Found num21 (damper) store at index $i"
        # num21 is on stack. Pass (num21, isDD, flag) to SafeDamper
        $d1 = $ilTick.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
        $d2 = $ilTick.Create([Mono.Cecil.Cil.OpCodes]::Ldfld, $f_isDD)
        $d3 = $ilTick.Create([Mono.Cecil.Cil.OpCodes]::Ldloc_0) # flag (driving)
        $d4 = $ilTick.Create([Mono.Cecil.Cil.OpCodes]::Call, $m_safeDamper)

        $ilTick.InsertBefore($inst, $d1)
        $ilTick.InsertBefore($inst, $d2)
        $ilTick.InsertBefore($inst, $d3)
        $ilTick.InsertBefore($inst, $d4)
        Write-Output "Patched FfbTick(): num21 filtered through SafeDamper for anti-oscillation"
        break
    }
}

# 9. Patch WheelConnect(): Set isDirectDrive = true for Fanatec/DD
$m_wheelConnect = $driverType.Methods | Where-Object { $_.Name -eq "WheelConnect" }
$ilWC = $m_wheelConnect.Body.GetILProcessor()
for ($i = $m_wheelConnect.Body.Instructions.Count - 1; $i -ge 0; $i--) {
    $inst = $m_wheelConnect.Body.Instructions[$i]
    if ($inst.OpCode.Name -eq "ldc.i4.1" -and $m_wheelConnect.Body.Instructions[$i+1].OpCode.Name -eq "stloc.s") {
        Write-Output "Found successful connection return at index $i"
        $wc1 = $ilWC.Create([Mono.Cecil.Cil.OpCodes]::Ldarg_0)
        $wc2 = $ilWC.Create([Mono.Cecil.Cil.OpCodes]::Ldc_I4_1)
        $wc3 = $ilWC.Create([Mono.Cecil.Cil.OpCodes]::Stfld, $f_isDD)
        $ilWC.InsertBefore($inst, $wc1)
        $ilWC.InsertBefore($inst, $wc2)
        $ilWC.InsertBefore($inst, $wc3)
        Write-Output "Patched WheelConnect(): Set isDirectDrive = true on connect"
        break
    }
}

# 10. Save modified assembly
$outPath = "D:\Projects\overthehillvr-fan\Mods\OverTheHillVR.dll.patched"
$asm.Write($outPath)
$asm.Dispose()
$memStream.Dispose()

Write-Output "Assembly written to $outPath successfully, size: $( (Get-Item $outPath).Length )"
Copy-Item $outPath $dllPath -Force
Remove-Item $outPath -Force
Write-Output "Patched OverTheHillVR.dll replaced successfully!"
