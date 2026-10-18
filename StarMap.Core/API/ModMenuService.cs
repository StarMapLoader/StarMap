using StarMap.API.Services;
using StarMap.Core.UI;

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
