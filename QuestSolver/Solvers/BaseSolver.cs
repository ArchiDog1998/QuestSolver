﻿using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using QuestSolver.Helpers;
using QuestSolver.Windows;
using XIVConfigUI.Attributes;

namespace QuestSolver.Solvers;
internal abstract class BaseSolver
{
    public virtual string Description => string.Empty;

    public virtual Type[] SubSolvers { get; } = [];

    private bool _isEnable;
    [UI("Enable")]
    public bool IsEnable 
    {
        get => _isEnable;
        set
        {
            if (_isEnable == value) return;
            _isEnable = value;

            if (value)
            {
                foreach (var solver in Plugin.GetSolvers(SubSolvers))
                {
                    solver.IsEnable = true;
                }
                Enable();
            }
            else
            {
                Disable();
            }
        }
    }

    protected static bool Available
    {
        get
        {
            if (!Player.Available) return false;
            if (Svc.Condition[ConditionFlag.Occupied]) return false;
            if (Svc.Condition[ConditionFlag.OccupiedInQuestEvent]) return false;
            if (Svc.Condition[ConditionFlag.OccupiedInEvent]) return false;
            if (Svc.Condition[ConditionFlag.Occupied38]) return false;
            if (Svc.Condition[ConditionFlag.Unknown57]) return false;
            if (Svc.Condition[ConditionFlag.Casting]) return false;
            if (Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]) return false;
            if (MountHelper.IsJumping) return false;

            return true;
        }
    }

    public abstract SolverItemType ItemType { get; }
    protected abstract void Enable();
    protected abstract void Disable();
}
