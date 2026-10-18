using StarMap.API.Interfaces;
using StarMap.Core.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarMap.Core.API
{
    internal class ModMenuService : IModMenuService
    {
        private readonly IModMenu _modMenu;

        public ModMenuService(IModMenu modMenu)
        {
            _modMenu = modMenu;
        }

        public IDisposable AddModMenuItem(string label, Action OnClick)
        {
            return _modMenu.AddModMenuButton(label, OnClick);
        }
    }
}
