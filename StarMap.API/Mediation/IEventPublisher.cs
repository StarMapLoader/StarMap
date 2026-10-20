namespace StarMap.API.Mediation
{
    public interface IEventPublisher { }

    public interface IEventPublisher<T> : IEventPublisher where T : IStarMapEvent
    {
        void Publish(T message);
    }
}
