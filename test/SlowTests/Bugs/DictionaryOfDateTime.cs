using System;
using System.Collections.Generic;
using System.IO;
using FastTests;
using Newtonsoft.Json;
using Raven.Client.Document;
using Xunit;

namespace SlowTests.Bugs
{
    public class DictionaryOfDateTime : RavenNewTestBase
    {
        private class WithDic
        {
            public Dictionary<DateTime, int> Items { get; set; }
        }
        
        [Fact]
        public void CanBeSerializedProperly()
        {
            var jsonSerializer = new DocumentConventions().CreateSerializer();
            using (var stringWriter = new StringWriter())
            {
                var item = new WithDic
                {
                    Items = new Dictionary<DateTime, int>
                    {
                        {new DateTime(2011, 11, 24), 1}
                    }
                };

                jsonSerializer.Serialize(stringWriter, item);

                var s = stringWriter.GetStringBuilder().ToString();
                Assert.Equal("{\"Items\":{\"2011-11-24T00:00:00\":1}}", s);
            }
        }

        [Fact]
        public void CanBeDeSerializedProperly()
        {
            var jsonSerializer = new DocumentConventions().CreateSerializer();
            using (var stringWriter = new StringWriter())
            {
                var item = new WithDic
                {
                    Items = new Dictionary<DateTime, int>
                    {
                        {new DateTime(2011, 11, 24), 1}
                    }
                };

                jsonSerializer.Serialize(stringWriter, item);

                var s = stringWriter.GetStringBuilder().ToString();
                var withDic = jsonSerializer.Deserialize<WithDic>(new JsonTextReader(new StringReader(s)));

                Assert.Equal(1, withDic.Items[new DateTime(2011, 11, 24)]);
            }
        }
    }
}
