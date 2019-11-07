using Xunit;

namespace SimpleServiceProvider.Tests
{
    public class ServiceProviderTest
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
            subject.Add(typeof(ITest3), typeof(Test3));
            
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
        
        [Fact]
        public void OnGet_WithAddInstance_Clear_ItShouldResolveTheSameInstance()
        {
            //Arrange
            var subject = new ServiceProvider();
            var expectedInstance = new Test3();
            subject.Add<ITest1, Test1>();
            subject.Add<ITest2, Test2>();
            subject.Add<ITest3, Test3>();
            subject.Get<ITest1>();
            subject.Add<ITest3>(expectedInstance);
            
            //Act
            subject.Clear();
            var result = subject.Get<ITest1>();
            

            //Assert 
            Assert.Same(expectedInstance, result.Test2.Test3);
        }
        
        [Fact]
        public void OnGet_Reset_ItShouldResolveANewInstance()
        {
            //Arrange
            var subject = new ServiceProvider();
            var expectedInstance = new Test3();
            subject.Add<ITest1, Test1>();
            subject.Add<ITest2, Test2>();
            subject.Add<ITest3>(expectedInstance);
            
            
            //Act
            var result1 = subject.Get<ITest3>();
            var result2 = subject.Get<ITest3>();
            subject.Reset();
            var result3 = subject.Get<ITest3>();
            

            //Assert 
            Assert.Same(result1, result2);
            Assert.NotSame(result2, result3);
        }
    } 
    
    #region Test classes
    
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