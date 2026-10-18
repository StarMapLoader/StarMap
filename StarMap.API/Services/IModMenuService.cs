namespace StarMap.API.Services
{
    public interface IModMenuService
    {
        IDisposable AddModMenuItem(string label, Action OnClick);
    }
}
