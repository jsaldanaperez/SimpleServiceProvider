using System;
using Xunit;

namespace SimpleServiceProvider.Tests
{
    public class OptionalConstructorParameterTest
    {
        [Fact]
        public void OnGet_WhenOptionalDependencyNotRegistered_ItShouldUseDefaultNull()
        {
            //Arrange
            var subject = new ServiceProvider();
            subject.Add<ConsumerWithOptionalDependency>();

            //Act
            var result = subject.Get<ConsumerWithOptionalDependency>();

            //Assert
            Assert.Null(result.Optional);
        }

        [Fact]
        public void OnGet_WhenOptionalDependencyRegistered_ItShouldResolveRegisteredInstance()
        {
            //Arrange
            var subject = new ServiceProvider();
            subject.Add<IOptionalDependency, OptionalDependency>();
            subject.Add<ConsumerWithOptionalDependency>();

            //Act
            var result = subject.Get<ConsumerWithOptionalDependency>();

            //Assert
            Assert.NotNull(result.Optional);
            Assert.IsType<OptionalDependency>(result.Optional);
        }

        [Fact]
        public void OnGet_WhenOptionalValueParamNotRegistered_ItShouldUseDeclaredDefaultValue()
        {
            //Arrange
            var subject = new ServiceProvider();
            subject.Add<ConsumerWithDefaultValueParam>();

            //Act
            var result = subject.Get<ConsumerWithDefaultValueParam>();

            //Assert
            Assert.Equal(42, result.Number);
        }

        [Fact]
        public void OnGet_WhenRequiredDependencyMissingAndNoDefault_ItShouldStillThrow()
        {
            //Arrange
            var subject = new ServiceProvider();
            subject.Add<ConsumerWithRequiredDependency>();

            //Act
            var exception = Assert.Throws<InvalidOperationException>(() => subject.Get<ConsumerWithRequiredDependency>());

            //Assert
            var expectedMessage = $"Unable to resolve type '{typeof(IOptionalDependency).FullName}' while attempting to activate '{typeof(ConsumerWithRequiredDependency).FullName}'.";
            Assert.Equal(expectedMessage, exception.Message);
        }

        #region Test classes

        public interface IOptionalDependency { }

        public class OptionalDependency : IOptionalDependency { }

        public class ConsumerWithOptionalDependency
        {
            public IOptionalDependency Optional { get; }

            public ConsumerWithOptionalDependency(IOptionalDependency optional = null)
            {
                Optional = optional;
            }
        }

        public class ConsumerWithDefaultValueParam
        {
            public int Number { get; }

            public ConsumerWithDefaultValueParam(int number = 42)
            {
                Number = number;
            }
        }

        public class ConsumerWithRequiredDependency
        {
            public ConsumerWithRequiredDependency(IOptionalDependency required)
            {
            }
        }

        #endregion
    }
}
