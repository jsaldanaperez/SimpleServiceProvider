using Xunit;

namespace SimpleServiceProvider.Tests
{
    public class ServiceProviderGenericsTest
    {
        [Fact]
        public void OnGet_ItShouldResolveType()
        {
            //Arrange
            var serviceProvider = new ServiceProvider();
            serviceProvider.Add(typeof(IRepository<Entity>), typeof(FakeSqlRepository<Entity>)); 
            serviceProvider.Add(typeof(IRepository<>), typeof(SqlRepository<>));
            serviceProvider.Add<IHandler, Handler>();
            
            //Act
            var result = serviceProvider.Get<IHandler>();

            //Assert 
            Assert.True(result.FakeRepository is FakeSqlRepository<Entity>);
            Assert.True(result.Repository is SqlRepository<BaseEntity>);
        }
        
        [Fact]
        public void OnGet_ItShouldResolveTheSameInstance()
        {
            //Arrange
            var serviceProvider = new ServiceProvider();
            var vehicleRepository = new SqlRepository<Vehicle>();
            
            serviceProvider.Add(typeof(IRepository<Entity>), typeof(FakeSqlRepository<Entity>)); 
            serviceProvider.Add(typeof(IRepository<>), typeof(SqlRepository<>)); 
            serviceProvider.Add<IRepository<Vehicle>>(vehicleRepository);
            serviceProvider.Add<IHandler, Handler>();
            
            //Act
            var result = serviceProvider.Get<IHandler>();
            var repository = serviceProvider.Get<IRepository<BaseEntity>>();
            var resolvedVehicleRepository = serviceProvider.Get<IRepository<Vehicle>>();

            //Assert 
            Assert.Same(result.Repository, repository); 
            Assert.Same(vehicleRepository, resolvedVehicleRepository);
        }
    }
    
    #region Test classes

    public interface IHandler
    {
        IRepository<Entity> FakeRepository { get; }
        IRepository<BaseEntity> Repository { get; }
        IRepository<Vehicle> VehicleRepository { get; }
    }
    
    public class Handler : IHandler
    {
        public IRepository<Entity> FakeRepository { get; }
        public IRepository<BaseEntity> Repository { get; }
        public IRepository<Vehicle> VehicleRepository { get; }

        public Handler(
            IRepository<Vehicle> vehicleRepository,
            IRepository<Entity> repository, 
            IRepository<BaseEntity> repository1)
        {
            VehicleRepository = vehicleRepository;
            FakeRepository = repository;
            Repository = repository1;
        }
    }
    
    public interface IRepository<T> { }

    public class SqlRepository<T> : IRepository<T> { }
    public class FakeSqlRepository<T> : IRepository<T> { }

    public class Entity { }
    public class BaseEntity { }
    public class Vehicle { }
    
    #endregion

}