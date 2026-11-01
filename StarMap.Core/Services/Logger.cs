namespace StarMap.Core.Services
{
    internal interface ILogger
    {
        void Information(string message);
        void Warning(string message);
        void Error(string message);
    }

    internal class Logger : ILogger
    {
        //private readonly LogCategory _category;

        public Logger()
        {
            //_category = LogCategory.Make("StarMap");
        }

        public void Error(string message)
        {
            Console.WriteLine($"StarMap error: '{message}'");
            //_category.Error(message);
        }

        public void Information(string message)
        {
            Console.WriteLine($"StarMap info: '{message}'");
            //_category.Info(message);
        }

        public void Warning(string message)
        {
            Console.WriteLine($"StarMap warning: '{message}'");
            //_category.Warning(message);
        }
    }
}
