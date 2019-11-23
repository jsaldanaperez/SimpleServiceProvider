using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SimpleServiceProvider.Tests
{
    public class ServiceProviderFuncTest
    {
        [Fact]
        public void OnGet_ItShouldResolveType()
        {
            //Arrange
            var serviceProvider = new ServiceProvider();
            var parts = new List<Type> {typeof(PartOne), typeof(PartTwo), typeof(PartTwo)};
            parts.ForEach(type => serviceProvider.Add(type));
            serviceProvider.Add<IPart[]>(provider => parts.Select(p => (IPart)provider.Get(p)).ToArray());
            serviceProvider.Add<FuncTestClass>();
            serviceProvider.Add<TestPartClass>();
            
            //Act
            var result = serviceProvider.Get<FuncTestClass>();

            //Assert 
            Assert.NotNull(result.Parts);
            Assert.Equal(3, result.Parts.Length);
            Assert.Single(result.Parts.Where(p => p is PartOne));
            Assert.Equal(2, result.Parts.Count(p => p is PartTwo));
        }
    }

    public class FuncTestClass
    {
        public IPart[] Parts { get; }
        public FuncTestClass(IPart[] parts) { Parts = parts; }
    }

    public class PartOne : IPart
    {
        public TestPartClass TestPartClass { get; }
        public PartOne(TestPartClass testPartClass) { TestPartClass = testPartClass; }
    }
    public class PartTwo : IPart { }
    public interface IPart { }
    public class TestPartClass { }
}