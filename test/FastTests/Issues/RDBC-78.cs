﻿using System.Collections.Generic;
using System.Linq;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Linq;
using Xunit;

namespace FastTests.Issues
{
    public class RDBC_78 : RavenTestBase
    {
        private class Person
        {
            public string Name { get; set; }
            public List<Pet> Pets { get; set; }
        }
        
        private class Pet
        {
            public string Name { get; set; }
        }
        
        private class PersonIndex : AbstractIndexCreationTask<Person>
        {
            public PersonIndex()
            {
                Map = persons => from person in persons
                    select new { person.Name, person.Pets };
            }
        }
                
        [Fact]
        public void CanQueryNestedClass()
        {             
            using (var store = GetDocumentStore())
            {
                new PersonIndex().Execute(store); 
                
                var fluffy = new Pet { Name = "Fluffy" };
                var john = new Person { Name = "John", Pets = new List<Pet> { fluffy } };
                
                using (var session = store.OpenSession())
                {
                    session.Store(john);
                    session.SaveChanges();
                }
                                
                WaitForIndexing(store);

                using (var session = store.OpenSession())
                {
                    var allPets = new List<Pet> { fluffy };
                    var query = session.Query<Person>().Where(p => p.Pets.ContainsAny(allPets)).ToList();                    
                    
                    Assert.Equal(1, query.Count);
                }                                
            }
        }
    }
}
