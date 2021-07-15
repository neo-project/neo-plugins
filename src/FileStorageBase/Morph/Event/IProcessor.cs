using Neo.FileStorage.Morph.Event;

namespace Neo.FileStorage.Morph.Event
{
    public interface IProcessor
    {
        ParserInfo[] ListenerParsers();
        HandlerInfo[] ListenerHandlers();
        HandlerInfo[] TimersHandlers();
    }
}
