namespace Vitarish.Communications.Application.Abstractions;

public interface ICommunicationCommandQueue
{
    void Enqueue(CommunicationCommand command);
}
