using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDb.Embedded.Tests
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
    }
}
