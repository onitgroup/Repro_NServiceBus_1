using NServiceBus;
using NServiceBus7TransactionTest.Data.Messages;
using System;
using System.Threading.Tasks;

namespace NServiceBus7TransactionTest.Data.Handlers
{
    public class TestHandler :
        IHandleMessages<Event1>,
        IHandleMessages<Event2>,
        IHandleMessages<Command1>,
        IHandleMessages<Command2>
    {
        public Task Handle(Event1 message, IMessageHandlerContext context)
        {
            Console.WriteLine("Received Event1");
            return Task.FromResult(0);
        }

        public Task Handle(Event2 message, IMessageHandlerContext context)
        {
            Console.WriteLine("Received Event2");
            return Task.FromResult(0);
        }

        public Task Handle(Command1 message, IMessageHandlerContext context)
        {
            Console.WriteLine("Received Command1");
            return Task.FromResult(0);
        }

        public Task Handle(Command2 message, IMessageHandlerContext context)
        {
            Console.WriteLine("Received Command2");
            return Task.FromResult(0);
        }
    }
}
