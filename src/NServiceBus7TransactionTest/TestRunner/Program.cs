using NServiceBus7TransactionTest.Data;
using NServiceBus7TransactionTest.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace TestRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            var context = new Context();

            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { Timeout = TimeSpan.MaxValue, IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var item = new Item
                {
                    Id = Guid.NewGuid(),
                    Name = "Test item",
                    LastModify = DateTime.Now
                };

                context.Items.Add(item);

                context.SaveChanges();
            }
        }
    }
}
