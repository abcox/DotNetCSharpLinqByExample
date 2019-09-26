using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace LinqLibTest
{
    [TestClass]
    public class Joins
    {
        public Joins()
        {
            SeedData();
        }

        public class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
        }

        public class Pet
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
        }

        public class PetOwner
        {
            public int Id { get; set; }
            public Person Person { get; set; }
            public Pet Pet { get; set; }
        }

        [TestMethod]
        public void DefaultIfEmptyEx1()
        {
            // Create a list of Pet objects.
            List<Pet> pets =
                new List<Pet>
                {
                    new Pet { Id = 1, Name = "Barley", Age = 8 },
                    new Pet { Id = 2, Name = "Boots", Age = 4 },
                    new Pet { Id = 3, Name = "Whiskers", Age = 1 },
                };

            // Call DefaultIfEmtpy() on the collection that Select()
            // returns, so that if the initial list is empty, there
            // will always be at least one item in the returned array.
            string[] names =
                pets.AsQueryable()
                .Select(pet => pet.Name)
                .DefaultIfEmpty()
                .ToArray();

            //string first = names[0];
            //Console.WriteLine(first);
            Assert.AreEqual(names[0], pets[0].Name);
        }

        private void SeedData()
        {
            People =
                new List<Person>
                {
                    new Person { Id = 1, Name = "Joe", Age = 16 },
                    new Person { Id = 2, Name = "Lisa", Age = 8 },
                    new Person { Id = 3, Name = "Mark", Age = 15 },
                    new Person { Id = 4, Name = "Sally", Age = 25 },
                };

            Pets = new List<Pet>
            {
                new Pet { Id = 1, Name = "Barley", Age = 8 },
                new Pet { Id = 2, Name = "Boots", Age = 4 },
                new Pet { Id = 3, Name = "Whiskers", Age = 1 },
            };

            PetOwners = new List<PetOwner>();

            People.ForEach(person =>
            {
                PetOwners.Add(new PetOwner
                {
                    Person = person,
                    Pet = Pets.FirstOrDefault(p => p.Id == person.Id),
                });
            });

        }

        public List<Person> People { get; private set; }

        public List<Pet> Pets { get; private set; }

        public List<PetOwner> PetOwners { get; private set; }

        [TestMethod]
        public void Test_Lambda_Join()
        {
            // Example of an "inner" join of People to Pets, and
            // because there are only 3 pets, 1 person
            // will be left out of the resulting owners.

            var owners = People.Join(Pets,
                person => person.Id,
                pet => pet.Id,
                (person, pet) => new 
                {
                    Person = person,
                    Pet = pet,
                });

            Assert.AreEqual(3, owners.Count());
        }

        [TestMethod]
        public void Test_Lambda_Join_Outer_Left()
        {
            // Example, using lamdba expression, of a
            // left outer join of People to Pets, and
            // because there are only 3 pets, 1 person
            // will be without a pet.

            var owners = People.GroupJoin(
                Pets,
                person => person.Id,
                pet => pet.Id,
                (person, pet) => new
                {
                    Person = person,
                    Pets = pet,
                }).SelectMany(
                pet => pet.Pets.DefaultIfEmpty(),
                (people, pet) => new
                {
                    people.Person,
                    Pet = pet,
                });

            Assert.IsNull(owners.Last().Pet);
        }

        [TestMethod]
        public void Test_Lambda_Join_Outer_Left_Compound() // todo..
        {
            // Example, using lamdba expression, of a
            // left outer join of People to Pets on Id
            // and where Person Age is twice the Pet, and
            // because there are only 3 pets, 1 person
            // will be without a pet.

            var owners = People.GroupJoin(
                Pets,
                person => new { person.Id, person.Age },
                pet => new { pet.Id, Age = pet.Age * 2 },
                (person, pet) => new
                {
                    Person = person,
                    Pets = pet,
                }).SelectMany(
                pet => pet.Pets.DefaultIfEmpty(),
                (people, pet) => new
                {
                    people.Person,
                    Pet = pet,
                });

            Assert.IsNull(owners.Last().Pet);
        }

        [TestMethod]
        public void Test_Query_Join_Outer_Left()
        {
            // Example of a left outer join of People to Pets, and
            // because there are only 3 pets, 1 person
            // will be without a pet.

            var owners =
                from person in People
                join pet in Pets on person.Id equals pet.Id into pets
                from pet in pets.DefaultIfEmpty()
                select new PetOwner
                {
                    Person = person,
                    Pet = pet,
                };

            Assert.IsNull(owners.Last().Pet);
        }

        [TestMethod]
        public void Test_Query_Join_Outer_Left_Compound()
        {
            // Example of a left outer join, using query expression
            // and anonymous types, of People to Pets, and
            // because there are only 3 pets, 1 person
            // will be without a pet.

            var owners =
                from person in People
                join pet in Pets
                on new
                {
                    person.Id,
                    person.Age,
                }
                equals new
                {
                    pet.Id,
                    Age = pet.Age * 2, // when owner is twice age of pet
                }
                into pets
                from pet in pets.DefaultIfEmpty()
                select new PetOwner
                {
                    Person = person,
                    Pet = pet,
                };

            var countOfOwnersWithoutPets = owners.Count(o => o.Pet is null);
            var petNameOfOwnerJoe = owners.FirstOrDefault(o => o.Person.Name == "Joe").Pet.Name;

            Assert.AreEqual(countOfOwnersWithoutPets, 2);            
            Assert.AreEqual(petNameOfOwnerJoe, "Barley");
        }

        [TestMethod]
        public void Test_Query_Join_Outer_Left_Compound__Right_Clear()
        {
            // Example of a left outer join, using query expression
            // and anonymous types, of People to Pets, and
            // because there are no pets, all 4 people
            // will be without a pet.

            Pets.Clear();

            var owners =
                from person in People
                join pet in Pets
                on new
                {
                    person.Id,
                    person.Age,
                }
                equals new
                {
                    pet.Id,
                    Age = pet.Age * 2, // when owner is twice age of pet
                }
                into pets
                from pet in pets.DefaultIfEmpty()
                select new PetOwner
                {
                    Person = person,
                    Pet = pet,
                };

            var countOfOwnersWithoutPets = owners.Count(o => o.Pet is null);
            var petNameOfOwnerJoe = owners.FirstOrDefault(o => o.Person.Name == "Joe").Pet?.Name;

            Assert.AreEqual(countOfOwnersWithoutPets, 4);
            Assert.IsNull(petNameOfOwnerJoe);
        }
    }

}
