using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SimpleServiceProvider.Tests
{
    public class ScopeTest
    {
        [Fact]
        public void OnCreateScope_WithSingleton_ItShouldReturnSameInstanceAcrossScopes()
        {
            //Arrange
            var subject = new ServiceProvider();
            subject.Add<IScopedDep, ScopedDep>(ServiceLifetime.Singleton);

            //Act
            using var scope1 = subject.CreateScope();
            using var scope2 = subject.CreateScope();
            var fromScope1 = scope1.Provider.Get<IScopedDep>();
            var fromScope2 = scope2.Provider.Get<IScopedDep>();
            var fromRoot = subject.Get<IScopedDep>();

            //Assert
            Assert.Same(fromScope1, fromScope2);
            Assert.Same(fromScope1, fromRoot);
        }

        [Fact]
        public void OnCreateScope_WithScoped_ItShouldReturnSameInstanceWithinScope()
        {
            //Arrange
            var subject = new ServiceProvider();
            subject.Add<IScopedDep, ScopedDep>(ServiceLifetime.Scoped);

            //Act
            using var scope = subject.CreateScope();
            var first = scope.Provider.Get<IScopedDep>();
            var second = scope.Provider.Get<IScopedDep>();

            //Assert
            Assert.Same(first, second);
        }

        [Fact]
        public void OnCreateScope_WithScoped_ItShouldReturnDifferentInstancesAcrossScopes()
        {
            //Arrange
            var subject = new ServiceProvider();
            subject.Add<IScopedDep, ScopedDep>(ServiceLifetime.Scoped);

            //Act
            using var scope1 = subject.CreateScope();
            using var scope2 = subject.CreateScope();
            var fromScope1 = scope1.Provider.Get<IScopedDep>();
            var fromScope2 = scope2.Provider.Get<IScopedDep>();

            //Assert
            Assert.NotSame(fromScope1, fromScope2);
        }

        [Fact]
        public void OnGet_WithScopedFromRoot_ItShouldThrow()
        {
            //Arrange
            var subject = new ServiceProvider();
            subject.Add<IScopedDep, ScopedDep>(ServiceLifetime.Scoped);

            //Act
            var exception = Assert.Throws<InvalidOperationException>(() => subject.Get<IScopedDep>());

            //Assert
            Assert.Contains("scoped service", exception.Message);
            Assert.Contains(typeof(IScopedDep).FullName, exception.Message);
        }

        [Fact]
        public void OnGet_WithTransient_ItShouldReturnNewInstanceEachCall()
        {
            //Arrange
            var subject = new ServiceProvider();
            subject.Add<IScopedDep, ScopedDep>(ServiceLifetime.Transient);

            //Act
            var first = subject.Get<IScopedDep>();
            var second = subject.Get<IScopedDep>();

            //Assert
            Assert.NotSame(first, second);
        }

        [Fact]
        public void OnGet_WithTransientInScope_ItShouldReturnNewInstanceEachCall()
        {
            //Arrange
            var subject = new ServiceProvider();
            subject.Add<IScopedDep, ScopedDep>(ServiceLifetime.Transient);

            //Act
            using var scope = subject.CreateScope();
            var first = scope.Provider.Get<IScopedDep>();
            var second = scope.Provider.Get<IScopedDep>();

            //Assert
            Assert.NotSame(first, second);
        }

        [Fact]
        public void OnDispose_WithScopedDisposable_ItShouldDisposeScopedInstance()
        {
            //Arrange
            var subject = new ServiceProvider();
            subject.Add<DisposableService>(ServiceLifetime.Scoped);

            DisposableService instance;
            using (var scope = subject.CreateScope())
            {
                instance = scope.Provider.Get<DisposableService>();
                Assert.False(instance.Disposed);
            }

            //Assert
            Assert.True(instance.Disposed);
        }

        [Fact]
        public void OnDispose_WithSingletonDisposable_ItShouldNotDisposeRootInstance()
        {
            //Arrange
            var subject = new ServiceProvider();
            subject.Add<DisposableService>(ServiceLifetime.Singleton);

            //Act
            DisposableService instance;
            using (var scope = subject.CreateScope())
            {
                instance = scope.Provider.Get<DisposableService>();
            }

            //Assert
            Assert.False(instance.Disposed);
        }

        [Fact]
        public void OnGet_AfterScopeDisposed_ItShouldThrow()
        {
            //Arrange
            var subject = new ServiceProvider();
            subject.Add<IScopedDep, ScopedDep>(ServiceLifetime.Scoped);
            var scope = subject.CreateScope();
            scope.Dispose();

            //Act / Assert
            Assert.Throws<ObjectDisposedException>(() => scope.Provider.Get<IScopedDep>());
        }

        [Fact]
        public void OnGet_IServiceScopeFactory_ItShouldResolveProvider()
        {
            //Arrange
            var subject = new ServiceProvider();

            //Act
            var factory = subject.Get<IServiceScopeFactory>();

            //Assert
            Assert.NotNull(factory);
            using var scope = factory.CreateScope();
            Assert.NotNull(scope.Provider);
        }

        [Fact]
        public void OnGet_IServiceScopeFactoryFromScope_ItShouldReturnRootFactory()
        {
            //Arrange
            var subject = new ServiceProvider();

            //Act
            using var scope = subject.CreateScope();
            var factoryFromScope = scope.Provider.Get<IServiceScopeFactory>();
            var factoryFromRoot = subject.Get<IServiceScopeFactory>();

            //Assert
            Assert.Same(factoryFromScope, factoryFromRoot);
        }

        [Fact]
        public void OnConcurrentGet_Singleton_ItShouldReturnSameInstance()
        {
            //Arrange
            SlowDep.ResetConstructionCount();
            var subject = new ServiceProvider();
            subject.Add<SlowDep>(ServiceLifetime.Singleton);
            const int threadCount = 32;

            //Act
            var resolved = new ConcurrentBag<SlowDep>();
            Parallel.For(0, threadCount, _ => resolved.Add(subject.Get<SlowDep>()));

            //Assert
            Assert.Equal(threadCount, resolved.Count);
            var distinct = resolved.Distinct().Count();
            Assert.Equal(1, distinct);
            Assert.Equal(1, SlowDep.ConstructionCount);
        }

        [Fact]
        public void OnConcurrentScopeCreateAndGet_ItShouldReturnConsistentResults()
        {
            //Arrange
            var subject = new ServiceProvider();
            subject.Add<IScopedDep, ScopedDep>(ServiceLifetime.Scoped);
            subject.Add<SlowDep>(ServiceLifetime.Singleton);
            const int scopeCount = 16;

            //Act
            var scopedInstances = new ConcurrentBag<IScopedDep>();
            var singletonInstances = new ConcurrentBag<SlowDep>();
            Parallel.For(0, scopeCount, _ =>
            {
                using var scope = subject.CreateScope();
                var scoped1 = scope.Provider.Get<IScopedDep>();
                var scoped2 = scope.Provider.Get<IScopedDep>();
                Assert.Same(scoped1, scoped2);
                scopedInstances.Add(scoped1);
                singletonInstances.Add(scope.Provider.Get<SlowDep>());
            });

            //Assert
            Assert.Equal(scopeCount, scopedInstances.Distinct().Count());
            Assert.Single(singletonInstances.Distinct());
        }

        #region Test classes

        public interface IScopedDep { }

        public class ScopedDep : IScopedDep { }

        public class DisposableService : IDisposable
        {
            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        public class SlowDep
        {
            private static int _constructionCount;
            public static int ConstructionCount => _constructionCount;
            public static void ResetConstructionCount() => Interlocked.Exchange(ref _constructionCount, 0);

            public SlowDep()
            {
                Interlocked.Increment(ref _constructionCount);
                Thread.Sleep(5);
            }
        }

        #endregion
    }
}
