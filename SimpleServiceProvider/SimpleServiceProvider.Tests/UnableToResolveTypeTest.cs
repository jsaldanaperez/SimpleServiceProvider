using System;
using Xunit;

namespace SimpleServiceProvider.Tests
{
    public class UnableToResolveTypeTest
    {
        [Fact]
        public void WhenUnableToResolve1_ItShouldThrow()
        {
            //Arrange
            var serviceProvider = new ServiceProvider();

            //Act
            var invalidOperationException = Assert.Throws<InvalidOperationException>(() => serviceProvider.Get<RootClass>());

            //Assert
            var expectedMessage = $"Unable to activate type '{typeof(RootClass).FullName}'.";
            Assert.Equal(expectedMessage, invalidOperationException.Message);
        }

        [Fact]
        public void WhenUnableToResolve2_ItShouldThrow()
        {
            //Arrange
            var serviceProvider = new ServiceProvider();
            serviceProvider.Add<RootClass>();

            //Act
            var invalidOperationException = Assert.Throws<InvalidOperationException>(() => serviceProvider.Get<RootClass>());

            //Assert
            var expectedMessage = $"Unable to resolve type '{typeof(InnerClass1).FullName}' while attempting to activate '{typeof(RootClass).FullName}'.";
            Assert.Equal(expectedMessage, invalidOperationException.Message);
        }

        [Fact]
        public void WhenUnableToResolve3_ItShouldThrow()
        {
            //Arrange
            var serviceProvider = new ServiceProvider();
            serviceProvider.Add<RootClass>();
            serviceProvider.Add<InnerClass1>();

            //Act
            var invalidOperationException = Assert.Throws<InvalidOperationException>(() => serviceProvider.Get<RootClass>());

            //Assert
            var expectedMessage = $"Unable to resolve type '{typeof(InnerClass2).FullName}' while attempting to activate '{typeof(InnerClass1).FullName}'.";
            Assert.Equal(expectedMessage, invalidOperationException.Message);
        }

        #region Test classes
        private class RootClass
        {
            public RootClass(InnerClass1 innerClass1)
            {

            }
        }

        private class InnerClass1
        {
            public InnerClass1(InnerClass2 innerClass2)
            {

            }
        }

        private class InnerClass2
        {

        }
        #endregion
    }
}