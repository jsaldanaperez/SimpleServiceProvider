using System;
using Xunit;

namespace SimpleServiceProvider.Tests
{
    public class SimpleServiceProviderTest
    {
        [Fact]
        public void OnGet_ItShouldResolveAllInstances()
        {
            //Arrange
            var subject = new ServiceProvider();
            subject.Add<ITest1, Test1>();
            subject.Add<ITest2, Test2>();
            subject.Add<ITest3, Test3>();
            
            //Act
            var result = subject.Get<ITest1>();

            //Assert 
            Assert.NotNull(result.Test2.Test3);
        }
        
        [Fact]
        public void OnGet_ItShouldResolveTheSameInstance()
        {
            //Arrange
            var subject = new ServiceProvider();
            subject.Add<ITest1, Test1>();
            subject.Add<ITest2, Test2>();
            subject.Add<ITest3, Test3>();
            
            //Act
            var test1 = subject.Get<ITest1>();
            var test3 = subject.Get<ITest3>();

            //Assert 
            Assert.Same(test1.Test2.Test3, test3);
        }
        
        [Fact]
        public void OnGet_WithAddInstance_ItShouldResolveTheSameInstance()
        {
            //Arrange
            var subject = new ServiceProvider();
            var expectedInstance = new Test3();
            subject.Add<ITest1, Test1>();
            subject.Add<ITest2, Test2>();
            subject.Add<ITest3>(expectedInstance);
            
            //Act
            var result = subject.Get<ITest3>();

            //Assert 
            Assert.Same(expectedInstance, result);
        }
    }

    #region TestClasses

    public interface ITest1
    {
        ITest2 Test2 { get; }
    }

    public interface ITest2
    {
        ITest3 Test3 { get; }
    }

    public interface ITest3
    {
    }


    public class Test1 : ITest1
    {
        public ITest2 Test2 { get; }

        public Test1(ITest2 test2)
        {
            Test2 = test2;
        }
    }

    public class Test2 : ITest2
    {
        public ITest3 Test3 { get; }

        public Test2(ITest3 test3)
        {
            Test3 = test3;
        }
    }

    public class Test3 : ITest3
    {
    }

    #endregion
}