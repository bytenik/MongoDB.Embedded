using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Embedded.Tests
{
    [TestFixture]
    public class BasicTests
    {
        [Test]
        public void BasicStartupTests()
        {
            using (var embedded = new EmbeddedMongoDbServer())
            {
                var client = embedded.Client;
            }
        }

        private class TestClass
        {
            public int Id { get; set; }
            public string TestValue { get; set; }
        }

        [Test]
        public void ReadWriteTest()
        {
            using (var embedded = new EmbeddedMongoDbServer())
            {
                var client = embedded.Client;
                var server = client.GetServer();
                var db = server.GetDatabase("test");
                var collection = db.GetCollection<TestClass>("col");
                collection.Save(new TestClass() {Id = 12345, TestValue = "Hello world."});
                var retrieved = collection.FindOneById(12345);
                Assert.NotNull(retrieved, "No object came back from the database.");
                Assert.AreEqual("Hello world.", retrieved.TestValue, "Unexpected test value came back.");
            }
        }

        [Test]
        public void DualServerReadWriteTest()
        {
            using (var embedded1 = new EmbeddedMongoDbServer())
            using (var embedded2 = new EmbeddedMongoDbServer())
            {
                var client1 = embedded1.Client;
                var server1 = client1.GetServer();
                var db1 = server1.GetDatabase("test");
                var collection1 = db1.GetCollection<TestClass>("col");
                collection1.Save(new TestClass() { Id = 12345, TestValue = "Hello world." });
                var retrieved1 = collection1.FindOneById(12345);
                Assert.NotNull(retrieved1, "No object came back from the database.");
                Assert.AreEqual("Hello world.", retrieved1.TestValue, "Unexpected test value came back.");

                var client2 = embedded2.Client;
                var server2 = client2.GetServer();
                var db2 = server2.GetDatabase("test");
                var collection2 = db2.GetCollection<TestClass>("col");
                collection2.Save(new TestClass() { Id = 12345, TestValue = "Hello world." });
                var retrieved2 = collection2.FindOneById(12345);
                Assert.NotNull(retrieved2, "No object came back from the database.");
                Assert.AreEqual("Hello world.", retrieved2.TestValue, "Unexpected test value came back.");
            }
        }
    }
}
