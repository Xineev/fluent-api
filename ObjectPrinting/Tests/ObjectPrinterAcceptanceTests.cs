using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using NUnit.Framework;

namespace ObjectPrinting.Tests
{
    [TestFixture]
    public class ObjectPrinterAcceptanceTests
    {
        private Person testPerson;
        private PrintingConfig<Person> personConfig;
        private PrintingConfig<Node> nodeConfig;
        private CultureInfo originalCulture;

        [SetUp]
        public void SetUp()
        {
            originalCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            testPerson = new Person
            {
                Name = "John Doe",
                Age = 30,
                Height = 180.5,
                BirthDate = new DateTime(1990, 1, 1),
                Id = Guid.NewGuid()
            };

            personConfig = new PrintingConfig<Person>();
            nodeConfig = new PrintingConfig<Node>();
        }

        [TearDown]
        public void TearDown()
        {
            // Восстанавливаем оригинальную культуру после каждого теста
            Thread.CurrentThread.CurrentCulture = originalCulture;
            Thread.CurrentThread.CurrentUICulture = originalCulture;
        }

        [Test]
        public void PrintToString_ReturnsCorrectFormat_WithSimpleObject()
        {
            var result = personConfig.PrintToString(testPerson);

            Assert.That(result, Contains.Substring("Person"));
            Assert.That(result, Contains.Substring("Name = John Doe"));
            Assert.That(result, Contains.Substring("Age = 30"));
            Assert.That(result, Contains.Substring("Height = 180.5"));
        }

        [Test]
        public void PrintToString_ReturnsNullString_WithNullObject()
        {
            var result = personConfig.PrintToString(null);

            Assert.That(result, Is.EqualTo("null" + Environment.NewLine));
        }

        [Test]
        public void PrintToString_DetectsAndHandlesCyclicReference_WhenObjectsReferenceEachOther()
        {
            var node1 = new Node { Name = "Node1" };
            var node2 = new Node { Name = "Node2" };

            node1.Next = node2;
            node2.Next = node1;

            var result = nodeConfig.PrintToString(node1);

            Assert.That(result, Contains.Substring("Cyclic reference detected"));
            Assert.DoesNotThrow(() => nodeConfig.PrintToString(node1));
        }

        [Test]
        public void PrintToString_WorksCorrectly_WhenMultipleConfigurationsAreCombined()
        {
            var config = new PrintingConfig<Person>();
            config.Exclude(p => p.Height)
                .SetSerialization(p => p.Name, name => name.ToUpper())
                .SetSerialization<int>(i => $"{i} years old")
                .TrimStringsTo(7);

            var result = config.PrintToString(testPerson);

            Assert.That(result, Contains.Substring("Name = JOHN DO"));
            Assert.That(result, Contains.Substring("Age = 30 years old"));
            Assert.That(result, Does.Not.Contain("Height = 180.5"));
        }

        [TestFixture]
        public class TypeExcludeTests
        {
            private Person testPerson;
            private PrintingConfig<Person> personConfig;
            private CultureInfo originalCulture;

            [SetUp]
            public void SetUp()
            {
                originalCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

                testPerson = new Person
                {
                    Name = "John Doe",
                    Age = 30,
                    Height = 180.5,
                    BirthDate = new DateTime(1990, 1, 1),
                    Id = Guid.NewGuid()
                };

                personConfig = new PrintingConfig<Person>();
            }

            [TearDown]
            public void TearDown()
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
                Thread.CurrentThread.CurrentUICulture = originalCulture;
            }

            [Test]
            public void Exclude_ExcludesProperties_WhenTypeIsSpecified()
            {
                personConfig.Exclude<int>();

                var result = personConfig.PrintToString(testPerson);

                Assert.That(result, Contains.Substring("Name = John Doe"));
                Assert.That(result, Does.Not.Contain("Age = 30"));
                Assert.That(result, Contains.Substring("Height = 180.5"));
            }

            [Test]
            public void Exclude_ExcludesAllProperties_WhenMultipleTypesAreSpecified()
            {
                personConfig.Exclude<int>()
                    .Exclude<double>();

                var result = personConfig.PrintToString(testPerson);

                Assert.That(result, Contains.Substring("Name = John Doe"));
                Assert.That(result, Does.Not.Contain("Age = 30"));
                Assert.That(result, Does.Not.Contain("Height = 180.5"));
            }

            [Test]
            public void SetSerialization_UsesCustomSerializer_WhenTypeIsSpecified()
            {
                personConfig.SetSerialization<int>(i => $"Integer: {i}");

                var result = personConfig.PrintToString(testPerson);

                Assert.That(result, Contains.Substring("Age = Integer: 30"));
            }
        }

        [TestFixture]
        public class SerializationTests
        {
            private Person testPerson;
            private PrintingConfig<Person> personConfig;
            private CultureInfo originalCulture;

            [SetUp]
            public void SetUp()
            {
                originalCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

                testPerson = new Person
                {
                    Name = "John Doe",
                    Age = 30,
                    Height = 180.5,
                    BirthDate = new DateTime(1990, 1, 1),
                    Id = Guid.NewGuid()
                };

                personConfig = new PrintingConfig<Person>();
            }

            [TearDown]
            public void TearDown()
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
                Thread.CurrentThread.CurrentUICulture = originalCulture;
            }

            [Test]
            public void SetSerialization_FormatsNumbersWithCustomLogic_WhenDoubleTypeIsSpecified()
            {
                personConfig.SetSerialization<double>(d => $"{d} centimeters");

                var result = personConfig.PrintToString(testPerson);

                Assert.That(result, Contains.Substring("Height = 180.5 centimeters"));
            }

            [Test]
            public void SetSerialization_UsesCustomSerializer_WhenPropertyIsSpecified()
            {
                personConfig.SetSerialization(p => p.Name, name => $"Name: {name.ToUpper()}");

                var result = personConfig.PrintToString(testPerson);

                Assert.That(result, Contains.Substring("Name = Name: JOHN DOE"));
            }
        }

        [TestFixture]
        public class SetCultureTests
        {
            private Person testPerson;
            private Company testCompany;
            private PrintingConfig<Person> personConfig;
            private PrintingConfig<Company> companyConfig;
            private CultureInfo originalCulture;

            [SetUp]
            public void SetUp()
            {
                originalCulture = Thread.CurrentThread.CurrentCulture;

                testPerson = new Person
                {
                    Name = "John Doe",
                    Age = 30,
                    Height = 180.5,
                    BirthDate = new DateTime(1990, 1, 1),
                    Id = Guid.NewGuid()
                };

                testCompany = new Company
                {
                    Name = "Test Company",
                    Employees = new List<Person> { testPerson },
                    Budget = 1000000.75m
                };

                personConfig = new PrintingConfig<Person>();
                companyConfig = new PrintingConfig<Company>();
            }

            [TearDown]
            public void TearDown()
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
                Thread.CurrentThread.CurrentUICulture = originalCulture;
            }

            [Test]
            public void SetCulture_UsesSpecifiedCulture_WhenDoubleTypeIsSpecified()
            {
                personConfig.SetCulture<double>(new CultureInfo("de-DE"));

                var result = personConfig.PrintToString(testPerson);

                Assert.That(result, Contains.Substring("Height = 180,5"));
            }

            [Test]
            public void SetCulture_UsesSpecifiedCulture_WhenDecimalTypeIsSpecified()
            {
                companyConfig.SetCulture<decimal>(new CultureInfo("fr-FR"));

                var result = companyConfig.PrintToString(testCompany);

                Assert.That(result, Contains.Substring("1000000,75"));
            }

            [Test]
            public void SetCulture_UsesSpecifiedCulture_WhenDateTimeTypeIsSpecified()
            {
                personConfig.SetCulture<DateTime>(new CultureInfo("en-US"));

                var result = personConfig.PrintToString(testPerson);

                Assert.That(result, Contains.Substring(testPerson.BirthDate.ToString(new CultureInfo("en-US"))));
            }

            [Test]
            public void SetCulture_ThrowsArgumentException_WhenCharTypeIsSpecified()
            {
                Assert.Throws<ArgumentException>(() =>
                    personConfig.SetCulture<char>(CultureInfo.CurrentCulture));
            }
        }

        [TestFixture]
        public class TrimStringsTests
        {
            private Person testPerson;
            private PrintingConfig<Person> personConfig;
            private CultureInfo originalCulture;

            [SetUp]
            public void SetUp()
            {
                originalCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

                testPerson = new Person
                {
                    Name = "John Doe",
                    Age = 30,
                    Height = 180.5,
                    BirthDate = new DateTime(1990, 1, 1),
                    Id = Guid.NewGuid()
                };

                personConfig = new PrintingConfig<Person>();
            }

            [TearDown]
            public void TearDown()
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
                Thread.CurrentThread.CurrentUICulture = originalCulture;
            }

            [Test]
            public void TrimStringsTo_TrimsLongStrings_WhenValidLengthIsSpecified()
            {
                personConfig.TrimStringsTo(5);

                var result = personConfig.PrintToString(testPerson);

                Assert.That(result, Contains.Substring("Name = John "));
            }

            [Test]
            public void TrimStringsTo_ThrowsArgumentException_WhenNegativeLengthIsSpecified()
            {
                Assert.Throws<ArgumentException>(() => personConfig.TrimStringsTo(-1));
            }
        }

        [TestFixture]
        public class PropertyExcludeTests
        {
            private Person testPerson;
            private PrintingConfig<Person> personConfig;
            private CultureInfo originalCulture;

            [SetUp]
            public void SetUp()
            {
                originalCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

                testPerson = new Person
                {
                    Name = "John Doe",
                    Age = 30,
                    Height = 180.5,
                    BirthDate = new DateTime(1990, 1, 1),
                    Id = Guid.NewGuid()
                };

                personConfig = new PrintingConfig<Person>();
            }

            [TearDown]
            public void TearDown()
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
                Thread.CurrentThread.CurrentUICulture = originalCulture;
            }

            [Test]
            public void Exclude_ExcludesSpecificProperty_WhenPropertyIsSpecified()
            {
                personConfig.Exclude(p => p.Age);

                var result = personConfig.PrintToString(testPerson);

                Assert.That(result, Contains.Substring("Name = John Doe"));
                Assert.That(result, Does.Not.Contain("Age = 30"));
                Assert.That(result, Contains.Substring("Height = 180.5"));
            }
        }

        [TestFixture]
        public class CollectionsTests
        {
            private Person testPerson;
            private Company testCompany;
            private CultureInfo originalCulture;

            [SetUp]
            public void SetUp()
            {
                originalCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

                testPerson = new Person
                {
                    Name = "John Doe",
                    Age = 30,
                    Height = 180.5,
                    BirthDate = new DateTime(1990, 1, 1),
                    Id = Guid.NewGuid()
                };

                testCompany = new Company
                {
                    Name = "Test Company",
                    Employees = new List<Person> { testPerson },
                    Budget = 1000000.75m
                };
            }

            [TearDown]
            public void TearDown()
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
                Thread.CurrentThread.CurrentUICulture = originalCulture;
            }

            [Test]
            public void PrintToString_SerializesCorrectly_WhenArrayIsPassed()
            {
                var array = new[] { 1, 2, 3, 4, 5 };
                var config = new PrintingConfig<int[]>();
                var result = config.PrintToString(array);

                Assert.That(result, Contains.Substring("Collection"));
                Assert.That(result, Contains.Substring("[0] = 1"));
                Assert.That(result, Contains.Substring("[1] = 2"));
                Assert.That(result, Contains.Substring("[2] = 3"));
                Assert.That(result, Contains.Substring("[3] = 4"));
                Assert.That(result, Contains.Substring("[4] = 5"));
            }

            [Test]
            public void PrintToString_SerializesCorrectly_WhenListIsPassed()
            {
                var list = new List<string> { "apple", "banana", "cherry" };
                var config = new PrintingConfig<List<string>>();
                var result = config.PrintToString(list);

                Assert.That(result, Contains.Substring("Collection"));
                Assert.That(result, Contains.Substring("[0] = apple"));
                Assert.That(result, Contains.Substring("[1] = banana"));
                Assert.That(result, Contains.Substring("[2] = cherry"));
            }

            [Test]
            public void PrintToString_SerializesCorrectly_WhenDictionaryIsPassed()
            {
                var dict = new Dictionary<string, int>
                {
                    ["one"] = 1,
                    ["two"] = 2,
                    ["three"] = 3
                };
                var config = new PrintingConfig<Dictionary<string, int>>();
                var result = config.PrintToString(dict);

                Assert.That(result, Contains.Substring("Dictionary"));
                Assert.That(result, Contains.Substring("[one] = 1"));
                Assert.That(result, Contains.Substring("[two] = 2"));
                Assert.That(result, Contains.Substring("[three] = 3"));
            }

            [Test]
            public void PrintToString_SerializesCorrectly_WhenEmptyCollectionIsPassed()
            {
                var emptyList = new List<string>();
                var config = new PrintingConfig<List<string>>();
                var result = config.PrintToString(emptyList);

                Assert.That(result, Contains.Substring("Collection"));
            }
        }
    }
}