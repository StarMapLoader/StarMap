using Brutal.ImGuiApi;
using KSA;
using StarMap.API.Interfaces;
using StarMap.Core.ModRepository;
using StarMap.Core.Patches;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

namespace StarMap.Core.UI
{
    internal interface IModMenu
    {
        IDisposable AddModMenuButton(string label, Action action);
    }

    internal class ModMenu : IModMenu
    {
        private enum ModMenuAction
        {
            ADD,
            REMOVE,
        }

        private readonly IPatchManager _patchManager;
        private readonly ConcurrentDictionary<string, ModMenuButton> _buttons = [];
        private readonly string _title;

        public ModMenu(IPatchManager patchManager)
        {
            _patchManager = patchManager;

            _patchManager.RegisterInstanceVoidPatch<Program>(
                IPatchService.PatchType.Prefix,
                null,
                typeof(Program).GetMethod("DrawProgramMenusHook")!,
                CreateMenu);

            _title = new ImString("Mods");
        }

        private bool CreateMenu<T>(object[] args, T program) where T : Program
        {
            if (_buttons.Count < 1)
                return true;


            if (ImGui.BeginMenu(_title))
            {
                foreach (var (_, button) in _buttons)
                {
                    button.DrawItem();
                }

                ImGui.EndMenu();
            }

            return true;
        }

        public IDisposable AddModMenuButton(string label, Action action)
        {
            var newButton = new ModMenuButton(this, label, action);

            if (!_buttons.TryAdd(label, newButton))
                throw new Exception($"Modmenu button with label {label} already exists");

            return newButton;
        }

        public void RemoveModMenuButton(string label)
        {
            _buttons.TryRemove(label, out _);
        }
    }

    internal class ModMenuButton : IDisposable
    {
        private readonly ModMenu _menu;
        private readonly string _label;
        private readonly Action _action;

        public ModMenuButton(ModMenu menu, string label, Action action)
        {
            _menu = menu;
            _label = label;
            _action = action;
        }

        public void DrawItem()
        {
            if (ImGui.MenuItem(_label))
                _action();
        }

        public void Dispose()
        {
            _menu.RemoveModMenuButton(_label);
        }
    }
}
