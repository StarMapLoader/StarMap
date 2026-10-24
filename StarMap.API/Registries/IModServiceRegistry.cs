namespace StarMap.API.Registries
{
    public interface IModServiceRegistry
    {
        public void RegisterService<T>(T implementation);

        public T Get<T>();
    }
}
