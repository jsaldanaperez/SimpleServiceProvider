using Xunit;

namespace SimpleServiceProvider.Tests
{
    public class TryGetTest
    {
        [Fact]
        public void OnTryGet_WhenRegistered_ItShouldReturnTrueAndResolveInstance()
        {
            //Arrange
            var subject = new ServiceProvider();
            subject.Add<ITryGetService, TryGetService>();

            //Act
            var found = subject.TryGet<ITryGetService>(out var instance);

            //Assert
            Assert.True(found);
            Assert.NotNull(instance);
            Assert.IsType<TryGetService>(instance);
        }

        [Fact]
        public void OnTryGet_WhenNotRegistered_ItShouldReturnFalseAndOutNull()
        {
            //Arrange
            var subject = new ServiceProvider();

            //Act
            var found = subject.TryGet<ITryGetService>(out var instance);

            //Assert
            Assert.False(found);
            Assert.Null(instance);
        }

        [Fact]
        public void OnTryGet_WhenRegisteredViaExpression_ItShouldReturnTrueAndResolveInstance()
        {
            //Arrange
            var subject = new ServiceProvider();
            var expected = new TryGetService();
            subject.Add<ITryGetService>(_ => expected);

            //Act
            var found = subject.TryGet<ITryGetService>(out var instance);

            //Assert
            Assert.True(found);
            Assert.Same(expected, instance);
        }

        [Fact]
        public void OnTryGet_WhenRegisteredAsOpenGeneric_ItShouldReturnTrueAndResolveClosedGeneric()
        {
            //Arrange
            var subject = new ServiceProvider();
            subject.Add(typeof(ITryGetGeneric<>), typeof(TryGetGeneric<>));

            //Act
            var found = subject.TryGet<ITryGetGeneric<TryGetItem>>(out var instance);

            //Assert
            Assert.True(found);
            Assert.IsType<TryGetGeneric<TryGetItem>>(instance);
        }
    }

    #region Test classes

    public interface ITryGetService { }

    public class TryGetService : ITryGetService { }

    public interface ITryGetGeneric<T> { }

    public class TryGetGeneric<T> : ITryGetGeneric<T> { }

    public class TryGetItem { }

    #endregion
}
