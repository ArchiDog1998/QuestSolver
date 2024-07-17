using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Runtime.InteropServices;
using System.Text;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace QuestSolver.Helpers;

/// <summary>
/// From https://github.com/Caraxi/SimpleTweaksPlugin/blob/main/Utility/Common.cs
/// </summary>
internal static unsafe class EventHelper
{
    public static AtkValue* SendEvent(AgentId agentId, ulong eventKind, params object[] eventparams)
    {
        var agent = AgentModule.Instance()->GetAgentByInternalId(agentId);
        return agent == null ? null : SendEvent(agent, eventKind, eventparams);
    }

    public static AtkValue* SendEvent(AgentInterface* agentInterface, ulong eventKind, params object[] eventParams)
    {
        var eventObject = stackalloc AtkValue[1];
        return SendEvent(agentInterface, eventObject, eventKind, eventParams);
    }

    public static AtkValue* SendEvent(AgentInterface* agentInterface, AtkValue* eventObject, ulong eventKind, params object[] eventParams)
    {
        var atkValues = CreateAtkValueArray(eventParams);
        if (atkValues == null) return eventObject;
        try
        {
            agentInterface->ReceiveEvent(eventObject, atkValues, (uint)eventParams.Length, eventKind);
            return eventObject;
        }
        finally
        {
            for (var i = 0; i < eventParams.Length; i++)
            {
                if (atkValues[i].Type == ValueType.String)
                {
                    Marshal.FreeHGlobal(new IntPtr(atkValues[i].String));
                }
            }

            Marshal.FreeHGlobal(new IntPtr(atkValues));
        }
    }

    public static AtkValue* CreateAtkValueArray(params object[] values)
    {
        var atkValues = (AtkValue*)Marshal.AllocHGlobal(values.Length * sizeof(AtkValue));
        if (atkValues == null) return null;
        try
        {
            for (var i = 0; i < values.Length; i++)
            {
                var v = values[i];
                switch (v)
                {
                    case uint uintValue:
                        atkValues[i].Type = ValueType.UInt;
                        atkValues[i].UInt = uintValue;
                        break;
                    case int intValue:
                        atkValues[i].Type = ValueType.Int;
                        atkValues[i].Int = intValue;
                        break;
                    case float floatValue:
                        atkValues[i].Type = ValueType.Float;
                        atkValues[i].Float = floatValue;
                        break;
                    case bool boolValue:
                        atkValues[i].Type = ValueType.Bool;
                        atkValues[i].Byte = (byte)(boolValue ? 1 : 0);
                        break;
                    case string stringValue:
                        {
                            atkValues[i].Type = ValueType.String;
                            var stringBytes = Encoding.UTF8.GetBytes(stringValue);
                            var stringAlloc = Marshal.AllocHGlobal(stringBytes.Length + 1);
                            Marshal.Copy(stringBytes, 0, stringAlloc, stringBytes.Length);
                            Marshal.WriteByte(stringAlloc, stringBytes.Length, 0);
                            atkValues[i].String = (byte*)stringAlloc;
                            break;
                        }
                    default:
                        throw new ArgumentException($"Unable to convert type {v.GetType()} to AtkValue");
                }
            }
        }
        catch
        {
            return null;
        }

        return atkValues;
    }

}
