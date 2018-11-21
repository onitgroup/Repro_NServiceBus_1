using Microsoft.VisualStudio.TestTools.UnitTesting;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Persistence.RavenDB;
using NServiceBus7TransactionTest.Data;
using NServiceBus7TransactionTest.Data.Messages;
using System;
using System.Linq;
using System.Threading;
using System.Transactions;

namespace NServiceBus7TransactionTest.Tests
{
    [TestClass]
    public class NServiceBus7TransactionShould
    {
        string _ravenDbUrl;
        string _endpointName;


        [TestInitialize]
        public void TestInitialize()
        {
            _ravenDbUrl = "http://localhost:8090";
            _endpointName = "NServiceBus7TransactionTest";
        }

        EndpointConfiguration BuildConfiguration()
        {
            var cfg = new EndpointConfiguration(_endpointName);
            cfg.SendFailedMessagesTo(_endpointName + ".error");

            var transport = cfg.UseTransport<MsmqTransport>();
            transport.Transactions(TransportTransactionMode.SendsAtomicWithReceive);

            var asm = typeof(Event1).Assembly;
            var routingMsmq = transport.Routing();
            routingMsmq.RegisterPublisher(asm, _endpointName);
            routingMsmq.RouteToEndpoint(asm, _endpointName);

            cfg.DisableFeature<TimeoutManager>();

            return cfg;
        }

        EndpointConfiguration BuildConfigurationMSMQ()
        {
            var cfg = BuildConfiguration();
            cfg.UsePersistence<MsmqPersistence>();
            return cfg;
        }

        EndpointConfiguration BuildConfigurationRavenDb()
        {
            var cfg = BuildConfiguration();
            var connectionParams = new ConnectionParameters();
            connectionParams.DatabaseName = _endpointName;
            connectionParams.Url = _ravenDbUrl;

            var persistence = cfg.UsePersistence<RavenDBPersistence, StorageType.Subscriptions>();
            persistence.DisableSubscriptionVersioning();
            persistence.SetDefaultDocumentStore(connectionParams);
            return cfg;
        }


        [TestMethod]
        public void MsmqPersistenceTest()
        {
            var cfg = BuildConfigurationMSMQ();

            cfg.EnableInstallers();
            var instance = Endpoint.Start(cfg).ConfigureAwait(false).GetAwaiter().GetResult();
            instance.Stop().GetAwaiter().GetResult();


            cfg = BuildConfigurationMSMQ();
            cfg.SendOnly();
            instance = Endpoint.Start(cfg).ConfigureAwait(false).GetAwaiter().GetResult();

            RunTransactionTest(instance);
        }

        [TestMethod]
        public void RavenDbPersistenceTest()
        {
            var cfg = BuildConfigurationRavenDb();

            cfg.EnableInstallers();
            var instance = Endpoint.Start(cfg).ConfigureAwait(false).GetAwaiter().GetResult();
            instance.Subscribe<Event1>().GetAwaiter().GetResult();
            instance.Subscribe<Event2>().GetAwaiter().GetResult();
            instance.Stop().GetAwaiter().GetResult();


            cfg = BuildConfigurationRavenDb();
            cfg.SendOnly();
            instance = Endpoint.Start(cfg).ConfigureAwait(false).GetAwaiter().GetResult();

            RunTransactionTest(instance);
        }

        void RunTransactionTest(IEndpointInstance endpointInstance)
        {
            var queue = MessageQueueUtils.GetPrivateQueueByName(_endpointName);
            queue.Purge();

            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, TimeSpan.MaxValue))
            {
                endpointInstance.Send(new Command1()).GetAwaiter().GetResult();
                Assert.AreEqual(0, queue.GetAllMessages().Count());

                endpointInstance.Send(new Command2()).GetAwaiter().GetResult();
                Assert.AreEqual(0, queue.GetAllMessages().Count());

                endpointInstance.Publish(new Event1()).GetAwaiter().GetResult();
                Assert.AreEqual(0, queue.GetAllMessages().Count());

                endpointInstance.Publish(new Event2()).GetAwaiter().GetResult();
                Assert.AreEqual(0, queue.GetAllMessages().Count());

                scope.Complete();
            }

            Assert.AreEqual( 4, queue.GetAllMessages().Count());
        }
    }
}
