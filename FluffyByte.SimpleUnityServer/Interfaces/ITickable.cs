namespace FluffyByte.SimpleUnityServer.Interfaces
{
    internal interface ITickable
    {
        string Name { get; }
        void Tick();
    }
}
