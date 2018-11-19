using Microsoft.VisualStudio.TestTools.UnitTesting;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence.RavenDB;
using NServiceBus7TransactionTest.Data;
using NServiceBus7TransactionTest.Data.Messages;
using System;
using System.Linq;
using System.Transactions;

namespace NServiceBus7TransactionTest.Tests
{
    [TestClass]
    public class NServiceBus7TransactionShould
    {
        string _ravenDbUrl;
        string _endpointName;
        EndpointConfiguration _endpointConfiguration;

        [TestInitialize]
        public void TestInitialize()
        {
            _ravenDbUrl = "http://localhost:8083";

            _endpointName = "NServiceBus7TransactionTest";
            _endpointConfiguration = new EndpointConfiguration(_endpointName);
            _endpointConfiguration.SendOnly();
            //_endpointConfiguration.EnableInstallers();
            _endpointConfiguration.SendFailedMessagesTo(_endpointName + ".error");

            var transport = _endpointConfiguration.UseTransport<MsmqTransport>();
            transport.Transactions(TransportTransactionMode.SendsAtomicWithReceive);

            var asm = typeof(Event1).Assembly;
            var routingMsmq = transport.Routing();
            routingMsmq.RegisterPublisher(asm, _endpointName);
            routingMsmq.RouteToEndpoint(asm, _endpointName);
        }

        [TestMethod]
        public void MsmqPersistenceTest()
        {
            _endpointConfiguration.DisableFeature<TimeoutManager>();
            _endpointConfiguration.UsePersistence<MsmqPersistence>();

            RunTransactionTest();
        }

        [TestMethod]
        public void RavenDbPersistenceTest()
        {
            var connectionParams = new ConnectionParameters();
            connectionParams.DatabaseName = _endpointName;
            connectionParams.Url = _ravenDbUrl;

            var persistence = _endpointConfiguration.UsePersistence<RavenDBPersistence>();
            persistence.DisableSubscriptionVersioning();
            persistence.SetDefaultDocumentStore(connectionParams);

            RunTransactionTest();
        }

        private void RunTransactionTest()
        {
            var endpointInstance = Endpoint.Start(_endpointConfiguration).ConfigureAwait(false).GetAwaiter().GetResult();

            var queue = MessageQueueUtils.GetPrivateQueueByName(_endpointName);
            queue.Purge();

            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, TimeSpan.MaxValue))
            {
                endpointInstance.Send(new Command1()).GetAwaiter().GetResult();
                Assert.AreEqual(queue.GetAllMessages().Count(), 0);

                endpointInstance.Send(new Command2()).GetAwaiter().GetResult();
                Assert.AreEqual(queue.GetAllMessages().Count(), 0);

                endpointInstance.Publish(new Event1()).GetAwaiter().GetResult();
                Assert.AreEqual(queue.GetAllMessages().Count(), 0);

                endpointInstance.Publish(new Event2()).GetAwaiter().GetResult();
                Assert.AreEqual(queue.GetAllMessages().Count(), 0);

                scope.Complete();
            }

            Assert.AreEqual(queue.GetAllMessages().Count(), 4);
        }
    }
}
