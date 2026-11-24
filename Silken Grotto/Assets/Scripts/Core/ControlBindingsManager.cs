using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilkenGrotto
{
    public enum ControlAction
    {
        MoveLeft,
        MoveRight,
        LookUp,
        LookDown,
        Sprint,
        JumpGlide,
        Dash,
        Harpoon,
        Silksoar,
        Interact,
        AttackCharge,
        SilkSkill,
        PaleNails,
        CrossStitch,
        BindHeal,
        Taunt,
        Needolin,
        CycleCrest,
        Inventory,
        Upgrades
    }

    /// <summary>
    /// Centralized persistent keybinding registry supporting full runtime keyboard & mouse
    /// manual rebinding via PlayerPrefs, automatic conflict swapping, and dynamic HUD/UI hints.
    /// </summary>
    public static class ControlBindingsManager
    {
        private const string PrefPrefix = "SG_Bind_";
        private const string PrefCustomPrefix = "SG_BindCustom_";

        private struct ActionBinding
        {
            public KeyCode Primary;
            public KeyCode Secondary;
            public KeyCode Tertiary;
            public bool IsCustomized;
        }

        private static readonly ControlAction[] AllActionsOrder = new ControlAction[]
        {
            ControlAction.MoveLeft,
            ControlAction.MoveRight,
            ControlAction.LookUp,
            ControlAction.LookDown,
            ControlAction.Sprint,
            ControlAction.JumpGlide,
            ControlAction.Dash,
            ControlAction.Harpoon,
            ControlAction.Silksoar,
            ControlAction.Interact,
            ControlAction.AttackCharge,
            ControlAction.SilkSkill,
            ControlAction.PaleNails,
            ControlAction.CrossStitch,
            ControlAction.BindHeal,
            ControlAction.Taunt,
            ControlAction.Needolin,
            ControlAction.CycleCrest,
            ControlAction.Inventory,
            ControlAction.Upgrades
        };

        private static readonly KeyCode[] RebindableKeys = new KeyCode[]
        {
            KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.Mouse2, KeyCode.Mouse3, KeyCode.Mouse4,
            KeyCode.A, KeyCode.B, KeyCode.C, KeyCode.D, KeyCode.E, KeyCode.F, KeyCode.G,
            KeyCode.H, KeyCode.I, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.M, KeyCode.N,
            KeyCode.O, KeyCode.P, KeyCode.Q, KeyCode.R, KeyCode.S, KeyCode.T, KeyCode.U,
            KeyCode.V, KeyCode.W, KeyCode.X, KeyCode.Y, KeyCode.Z,
            KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
            KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9,
            KeyCode.Space, KeyCode.Tab, KeyCode.Return, KeyCode.Backspace,
            KeyCode.LeftShift, KeyCode.RightShift, KeyCode.LeftControl, KeyCode.RightControl,
            KeyCode.LeftAlt, KeyCode.RightAlt,
            KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
            KeyCode.Minus, KeyCode.Equals, KeyCode.LeftBracket, KeyCode.RightBracket,
            KeyCode.Semicolon, KeyCode.Quote, KeyCode.Comma, KeyCode.Period, KeyCode.Slash,
            KeyCode.BackQuote,
            KeyCode.Keypad0, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4,
            KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9,
            KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, KeyCode.F6,
            KeyCode.F7, KeyCode.F8, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12
        };

        private static readonly Dictionary<ControlAction, ActionBinding> _bindings = new Dictionary<ControlAction, ActionBinding>();
        private static bool _initialized;

        public static event Action OnBindingsChanged;

        public static ControlAction[] AllActions { get { return AllActionsOrder; } }
        public static KeyCode[] SupportedRebindKeys { get { return RebindableKeys; } }

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            LoadFromPrefs();
        }

        private static ActionBinding GetDefaultBinding(ControlAction action)
        {
            switch (action)
            {
                case ControlAction.MoveLeft:     return new ActionBinding { Primary = KeyCode.A, Secondary = KeyCode.LeftArrow, Tertiary = KeyCode.None };
                case ControlAction.MoveRight:    return new ActionBinding { Primary = KeyCode.D, Secondary = KeyCode.RightArrow, Tertiary = KeyCode.None };
                case ControlAction.LookUp:       return new ActionBinding { Primary = KeyCode.W, Secondary = KeyCode.UpArrow, Tertiary = KeyCode.None };
                case ControlAction.LookDown:     return new ActionBinding { Primary = KeyCode.S, Secondary = KeyCode.DownArrow, Tertiary = KeyCode.None };
                case ControlAction.Sprint:       return new ActionBinding { Primary = KeyCode.LeftShift, Secondary = KeyCode.RightShift, Tertiary = KeyCode.None };
                case ControlAction.JumpGlide:    return new ActionBinding { Primary = KeyCode.Space, Secondary = KeyCode.Z, Tertiary = KeyCode.None };
                case ControlAction.Dash:         return new ActionBinding { Primary = KeyCode.Mouse1, Secondary = KeyCode.K, Tertiary = KeyCode.L };
                case ControlAction.Harpoon:      return new ActionBinding { Primary = KeyCode.LeftControl, Secondary = KeyCode.Mouse3, Tertiary = KeyCode.None };
                case ControlAction.Silksoar:     return new ActionBinding { Primary = KeyCode.LeftAlt, Secondary = KeyCode.Y, Tertiary = KeyCode.None };
                case ControlAction.Interact:     return new ActionBinding { Primary = KeyCode.E, Secondary = KeyCode.None, Tertiary = KeyCode.None };
                case ControlAction.AttackCharge: return new ActionBinding { Primary = KeyCode.Mouse0, Secondary = KeyCode.J, Tertiary = KeyCode.X };
                case ControlAction.SilkSkill:    return new ActionBinding { Primary = KeyCode.Q, Secondary = KeyCode.F, Tertiary = KeyCode.None };
                case ControlAction.PaleNails:    return new ActionBinding { Primary = KeyCode.C, Secondary = KeyCode.Mouse2, Tertiary = KeyCode.None };
                case ControlAction.CrossStitch:  return new ActionBinding { Primary = KeyCode.R, Secondary = KeyCode.V, Tertiary = KeyCode.None };
                case ControlAction.BindHeal:     return new ActionBinding { Primary = KeyCode.H, Secondary = KeyCode.None, Tertiary = KeyCode.None };
                case ControlAction.Taunt:        return new ActionBinding { Primary = KeyCode.T, Secondary = KeyCode.G, Tertiary = KeyCode.None };
                case ControlAction.Needolin:     return new ActionBinding { Primary = KeyCode.N, Secondary = KeyCode.None, Tertiary = KeyCode.None };
                case ControlAction.CycleCrest:   return new ActionBinding { Primary = KeyCode.Alpha1, Secondary = KeyCode.O, Tertiary = KeyCode.None };
                case ControlAction.Inventory:    return new ActionBinding { Primary = KeyCode.I, Secondary = KeyCode.B, Tertiary = KeyCode.None };
                case ControlAction.Upgrades:     return new ActionBinding { Primary = KeyCode.Tab, Secondary = KeyCode.U, Tertiary = KeyCode.None };
                default:                         return new ActionBinding { Primary = KeyCode.None, Secondary = KeyCode.None, Tertiary = KeyCode.None };
            }
        }

        private static void LoadFromPrefs()
        {
            _bindings.Clear();
            for (int i = 0; i < AllActionsOrder.Length; i++)
            {
                ControlAction act = AllActionsOrder[i];
                ActionBinding def = GetDefaultBinding(act);
                bool custom = PlayerPrefs.GetInt(PrefCustomPrefix + act.ToString(), 0) == 1;
                if (custom)
                {
                    int savedKeyInt = PlayerPrefs.GetInt(PrefPrefix + act.ToString(), (int)def.Primary);
                    KeyCode savedKey = (KeyCode)savedKeyInt;
                    bool keepArrows = act == ControlAction.MoveLeft || act == ControlAction.MoveRight
                        || act == ControlAction.LookUp || act == ControlAction.LookDown;
                    _bindings[act] = new ActionBinding
                    {
                        Primary = savedKey,
                        Secondary = keepArrows && def.Secondary != savedKey ? def.Secondary : KeyCode.None,
                        Tertiary = KeyCode.None,
                        IsCustomized = true
                    };
                }
                else
                {
                    _bindings[act] = def;
                }
            }

            // Strip any default secondary/tertiary keys that collide with a customized primary key
            for (int i = 0; i < AllActionsOrder.Length; i++)
            {
                ControlAction act = AllActionsOrder[i];
                ActionBinding cur = _bindings[act];
                if (!cur.IsCustomized || cur.Primary == KeyCode.None) continue;
                ClearConflictingSecondaries(act, cur.Primary);
            }
        }

        private static void ClearConflictingSecondaries(ControlAction owner, KeyCode claimedKey)
        {
            for (int i = 0; i < AllActionsOrder.Length; i++)
            {
                ControlAction other = AllActionsOrder[i];
                if (other == owner) continue;
                ActionBinding b = _bindings[other];
                bool dirty = false;
                if (b.Secondary == claimedKey) { b.Secondary = KeyCode.None; dirty = true; }
                if (b.Tertiary == claimedKey) { b.Tertiary = KeyCode.None; dirty = true; }
                if (dirty) _bindings[other] = b;
            }
        }

        public static KeyCode GetPrimaryKey(ControlAction action)
        {
            EnsureInitialized();
            ActionBinding b;
            return _bindings.TryGetValue(action, out b) ? b.Primary : KeyCode.None;
        }

        public static KeyCode GetSecondaryKey(ControlAction action)
        {
            EnsureInitialized();
            ActionBinding b;
            return _bindings.TryGetValue(action, out b) ? b.Secondary : KeyCode.None;
        }

        public static bool GetAction(ControlAction action)
        {
            EnsureInitialized();
            ActionBinding b;
            if (!_bindings.TryGetValue(action, out b)) return false;
            return (b.Primary != KeyCode.None && Input.GetKey(b.Primary))
                || (b.Secondary != KeyCode.None && Input.GetKey(b.Secondary))
                || (b.Tertiary != KeyCode.None && Input.GetKey(b.Tertiary));
        }

        public static bool GetActionDown(ControlAction action)
        {
            EnsureInitialized();
            ActionBinding b;
            if (!_bindings.TryGetValue(action, out b)) return false;
            return (b.Primary != KeyCode.None && Input.GetKeyDown(b.Primary))
                || (b.Secondary != KeyCode.None && Input.GetKeyDown(b.Secondary))
                || (b.Tertiary != KeyCode.None && Input.GetKeyDown(b.Tertiary));
        }

        public static bool GetActionUp(ControlAction action)
        {
            EnsureInitialized();
            ActionBinding b;
            if (!_bindings.TryGetValue(action, out b)) return false;
            return (b.Primary != KeyCode.None && Input.GetKeyUp(b.Primary))
                || (b.Secondary != KeyCode.None && Input.GetKeyUp(b.Secondary))
                || (b.Tertiary != KeyCode.None && Input.GetKeyUp(b.Tertiary));
        }

        /// <summary>
        /// Rebinds an action's Primary key to newKey, automatically swapping Primary keys if
        /// another action already used newKey, and persists cleanly to PlayerPrefs.
        /// Returns a human-readable feedback string for the Controls UI.
        /// </summary>
        public static string RebindPrimary(ControlAction action, KeyCode newKey)
        {
            EnsureInitialized();
            if (newKey == KeyCode.None || newKey == KeyCode.Escape) return "Rebind cancelled.";

            ActionBinding cur = _bindings[action];
            KeyCode prevPrimary = cur.Primary;
            string swappedActionName = null;

            // Swap cleanly if another action already uses newKey as Primary
            for (int i = 0; i < AllActionsOrder.Length; i++)
            {
                ControlAction other = AllActionsOrder[i];
                if (other == action) continue;
                ActionBinding ob = _bindings[other];
                if (ob.Primary == newKey)
                {
                    ob.Primary = prevPrimary;
                    ob.IsCustomized = true;
                    if (ob.Secondary == prevPrimary) ob.Secondary = KeyCode.None;
                    if (ob.Tertiary == prevPrimary) ob.Tertiary = KeyCode.None;
                    _bindings[other] = ob;
                    PlayerPrefs.SetInt(PrefPrefix + other.ToString(), (int)ob.Primary);
                    PlayerPrefs.SetInt(PrefCustomPrefix + other.ToString(), 1);
                    swappedActionName = GetActionDisplayName(other);
                    break;
                }
            }

            ActionBinding def = GetDefaultBinding(action);
            bool keepArrows = action == ControlAction.MoveLeft || action == ControlAction.MoveRight
                || action == ControlAction.LookUp || action == ControlAction.LookDown;
            cur.Primary = newKey;
            cur.Secondary = keepArrows && def.Secondary != newKey ? def.Secondary : KeyCode.None;
            cur.Tertiary = KeyCode.None;
            cur.IsCustomized = true;
            _bindings[action] = cur;

            ClearConflictingSecondaries(action, newKey);

            PlayerPrefs.SetInt(PrefPrefix + action.ToString(), (int)newKey);
            PlayerPrefs.SetInt(PrefCustomPrefix + action.ToString(), 1);
            PlayerPrefs.Save();

            if (OnBindingsChanged != null) OnBindingsChanged();

            string actLabel = GetActionDisplayName(action);
            string keyLabel = FormatKeyName(newKey);
            if (!string.IsNullOrEmpty(swappedActionName))
            {
                return string.Format("Bound {0} -> [{1}] (swapped with {2})", actLabel, keyLabel, swappedActionName);
            }
            return string.Format("Bound {0} -> [{1}]", actLabel, keyLabel);
        }

        public static void ResetAllToDefaults()
        {
            EnsureInitialized();
            _bindings.Clear();
            for (int i = 0; i < AllActionsOrder.Length; i++)
            {
                ControlAction act = AllActionsOrder[i];
                ActionBinding def = GetDefaultBinding(act);
                _bindings[act] = def;
                PlayerPrefs.SetInt(PrefPrefix + act.ToString(), (int)def.Primary);
                PlayerPrefs.SetInt(PrefCustomPrefix + act.ToString(), 0);
            }
            PlayerPrefs.Save();
            if (OnBindingsChanged != null) OnBindingsChanged();
        }

        public static string GetActionDisplayName(ControlAction action)
        {
            switch (action)
            {
                case ControlAction.MoveLeft:     return "Move Left";
                case ControlAction.MoveRight:    return "Move Right";
                case ControlAction.LookUp:       return "Up / Aim Up";
                case ControlAction.LookDown:     return "Down / Drop";
                case ControlAction.Sprint:       return "Sprint / Modifier";
                case ControlAction.JumpGlide:    return "Jump / Glide";
                case ControlAction.Dash:         return "Dash / Air Dash";
                case ControlAction.Harpoon:      return "Harpoon (Clawline)";
                case ControlAction.Silksoar:     return "Silksoar (Vert Launch)";
                case ControlAction.Interact:     return "Interact / Bench";
                case ControlAction.AttackCharge: return "Attack / Charge";
                case ControlAction.SilkSkill:    return "Silk Skill Cast";
                case ControlAction.PaleNails:    return "Pale Nails";
                case ControlAction.CrossStitch:  return "Cross Stitch Parry";
                case ControlAction.BindHeal:     return "Bind Heal (Air/Gnd)";
                case ControlAction.Taunt:        return "Taunt (Garama!)";
                case ControlAction.Needolin:     return "Needolin (Silk Song)";
                case ControlAction.CycleCrest:   return "Switch Combat Crest";
                case ControlAction.Inventory:    return "Inventory Pouch";
                case ControlAction.Upgrades:     return "Upgrades Altar";
                default:                         return action.ToString();
            }
        }

        public static string FormatKeyName(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.None:         return "---";
                case KeyCode.Mouse0:       return "LMB";
                case KeyCode.Mouse1:       return "RMB";
                case KeyCode.Mouse2:       return "MMB";
                case KeyCode.Mouse3:       return "Mouse 4";
                case KeyCode.Mouse4:       return "Mouse 5";
                case KeyCode.LeftShift:    return "L-Shift";
                case KeyCode.RightShift:   return "R-Shift";
                case KeyCode.LeftControl:  return "L-Ctrl";
                case KeyCode.RightControl: return "R-Ctrl";
                case KeyCode.LeftAlt:      return "L-Alt";
                case KeyCode.RightAlt:     return "R-Alt";
                case KeyCode.Space:        return "Space";
                case KeyCode.Return:       return "Enter";
                case KeyCode.Backspace:    return "Bksp";
                case KeyCode.UpArrow:      return "Up";
                case KeyCode.DownArrow:    return "Down";
                case KeyCode.LeftArrow:    return "Left";
                case KeyCode.RightArrow:   return "Right";
                case KeyCode.Alpha0:       return "0";
                case KeyCode.Alpha1:       return "1";
                case KeyCode.Alpha2:       return "2";
                case KeyCode.Alpha3:       return "3";
                case KeyCode.Alpha4:       return "4";
                case KeyCode.Alpha5:       return "5";
                case KeyCode.Alpha6:       return "6";
                case KeyCode.Alpha7:       return "7";
                case KeyCode.Alpha8:       return "8";
                case KeyCode.Alpha9:       return "9";
                case KeyCode.BackQuote:    return "`";
                case KeyCode.Minus:        return "-";
                case KeyCode.Equals:       return "=";
                case KeyCode.LeftBracket:  return "[";
                case KeyCode.RightBracket: return "]";
                case KeyCode.Semicolon:    return ";";
                case KeyCode.Quote:        return "'";
                case KeyCode.Comma:        return ",";
                case KeyCode.Period:       return ".";
                case KeyCode.Slash:        return "/";
                default:                   return key.ToString();
            }
        }

        public static string GetDisplayLabelForButton(ControlAction action)
        {
            EnsureInitialized();
            ActionBinding b;
            if (!_bindings.TryGetValue(action, out b)) return "---";
            string p = FormatKeyName(b.Primary);
            if (!b.IsCustomized && b.Secondary != KeyCode.None)
            {
                return p + " / " + FormatKeyName(b.Secondary);
            }
            return p;
        }

        public static string BuildDynamicComboTips()
        {
            string up = FormatKeyName(GetPrimaryKey(ControlAction.LookUp));
            string dn = FormatKeyName(GetPrimaryKey(ControlAction.LookDown));
            string spr = FormatKeyName(GetPrimaryKey(ControlAction.Sprint));
            string jmp = FormatKeyName(GetPrimaryKey(ControlAction.JumpGlide));
            string atk = FormatKeyName(GetPrimaryKey(ControlAction.AttackCharge));
            string slk = FormatKeyName(GetPrimaryKey(ControlAction.SilkSkill));
            string nail = FormatKeyName(GetPrimaryKey(ControlAction.PaleNails));
            string pry = FormatKeyName(GetPrimaryKey(ControlAction.CrossStitch));
            string hrp = FormatKeyName(GetPrimaryKey(ControlAction.Harpoon));
            string soar = FormatKeyName(GetPrimaryKey(ControlAction.Silksoar));
            string ndl = FormatKeyName(GetPrimaryKey(ControlAction.Needolin));
            string crst = FormatKeyName(GetPrimaryKey(ControlAction.CycleCrest));

            return string.Format(
                "TRAVERSAL & CRESTS  |  Hold [{0}] Air: Glide   ·   [{8}]: Harpoon   ·   [{9}]: Silksoar   ·   [{10}]: Needolin   ·   [{11}]: Cycle Crest (Hunter/Reaper/Witch)\n" +
                "SILK SKILLS (6/6)  |  [{2}]: Silkspear   ·   [{3}+{2}]: Thread Storm   ·   [{4}+{2}]: Rune Rage   ·   [{5}+{2}]: Sharpdart   ·   [{6}]: Pale Nails   ·   [{7}]: Cross Stitch   ·   Hold [{1}]: Charge",
                jmp, atk, slk, up, dn, spr, nail, pry, hrp, soar, ndl, crst);
        }

        public static string BuildMainMenuLegend()
        {
            string l = FormatKeyName(GetPrimaryKey(ControlAction.MoveLeft));
            string r = FormatKeyName(GetPrimaryKey(ControlAction.MoveRight));
            string up = FormatKeyName(GetPrimaryKey(ControlAction.LookUp));
            string dn = FormatKeyName(GetPrimaryKey(ControlAction.LookDown));
            string spr = FormatKeyName(GetPrimaryKey(ControlAction.Sprint));
            string jmp = FormatKeyName(GetPrimaryKey(ControlAction.JumpGlide));
            string atk = FormatKeyName(GetPrimaryKey(ControlAction.AttackCharge));
            string dsh = FormatKeyName(GetPrimaryKey(ControlAction.Dash));
            string hrp = FormatKeyName(GetPrimaryKey(ControlAction.Harpoon));
            string soar = FormatKeyName(GetPrimaryKey(ControlAction.Silksoar));
            string slk = FormatKeyName(GetPrimaryKey(ControlAction.SilkSkill));
            string nail = FormatKeyName(GetPrimaryKey(ControlAction.PaleNails));
            string pry = FormatKeyName(GetPrimaryKey(ControlAction.CrossStitch));
            string heal = FormatKeyName(GetPrimaryKey(ControlAction.BindHeal));
            string crst = FormatKeyName(GetPrimaryKey(ControlAction.CycleCrest));
            string ndl = FormatKeyName(GetPrimaryKey(ControlAction.Needolin));

            return string.Format(
                "CONTROLS (CUSTOMIZABLE IN CONTROLS MENU)\n" +
                "{0}/{1} : Move   |   {2} : Sprint   |   {3} : Jump / Air Glide   |   {7} : Dash\n" +
                "{8} : Harpoon   |   {9} : Silksoar   |   {14} : Switch Crest (Hunter/Reaper/Witch)\n" +
                "{4} : Attack / Hold Charge (+{5}/{6})   |   {10} : Silk Skills   |   {11} : Pale Nails\n" +
                "{12} : Cross Stitch   |   {13} : Bind Heal   |   {15} : Needolin",
                l, r, spr, jmp, atk, up, dn, dsh, hrp, soar, slk, nail, pry, heal, crst, ndl);
        }
    }
}
