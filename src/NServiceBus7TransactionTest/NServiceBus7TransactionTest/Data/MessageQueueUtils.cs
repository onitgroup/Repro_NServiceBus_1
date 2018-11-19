using System.Linq;
using System.Messaging;

namespace NServiceBus7TransactionTest.Data
{
    public static class MessageQueueUtils
    {
        public static MessageQueue GetPrivateQueueByName(string queueName)
        {
            var searchName = string.Format("private$\\{0}", queueName.ToLower());

            MessageQueue[] QueueList = MessageQueue.GetPrivateQueuesByMachine(".");

            var queue = QueueList.Where(x => x.QueueName.ToLower() == searchName).SingleOrDefault();

            return queue;
        }
    }
}
