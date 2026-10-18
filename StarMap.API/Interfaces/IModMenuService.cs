using System;
using System.Collections.Generic;
using System.Text;

namespace StarMap.API.Interfaces
{
    public interface IModMenuService
    {
        IDisposable AddModMenuItem(string label, Action OnClick);
    }
}
