﻿namespace SimpleInjector.Tests.Unit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using SimpleInjector.Advanced;
    using SimpleInjector.Extensions;
    using SimpleInjector.Tests.Unit.Extensions;

    [TestClass]
    public class VerifyTests
    {
        [TestMethod]
        public void Verify_WithEmptyConfiguration_Succeeds()
        {
            // Arrange
            var container = ContainerFactory.New();

            // Act
            container.Verify();
        }

        [TestMethod]
        public void Verify_CalledMultipleTimes_Succeeds()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterSingle<IUserRepository>(new SqlUserRepository());

            container.Verify();

            // Act
            container.Verify();
        }

        [TestMethod]
        public void Verify_CalledAfterGetInstance_Succeeds()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterSingle<IUserRepository>(new SqlUserRepository());

            container.GetInstance<IUserRepository>();

            container.Verify();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException), "An exception was expected because the configuration is invalid without registering an IUserRepository.")]
        public void Verify_WithDependantTypeNotRegistered_ThrowsException()
        {
            // Arrange
            var container = ContainerFactory.New();

            // RealUserService has a constructor that takes an IUserRepository.
            container.RegisterSingle<RealUserService>();

            // Act
            container.Verify();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Verify_WithFailingFunc_ThrowsException()
        {
            // Arrange
            var container = ContainerFactory.New();
            container.Register<IUserRepository>(() =>
            {
                throw new ArgumentNullException();
            });

            // Act
            container.Verify();
        }

        [TestMethod]
        public void Verify_RegisteredCollectionWithValidElements_Succeeds()
        {
            // Arrange
            var container = ContainerFactory.New();
            container.RegisterAll<IUserRepository>(new IUserRepository[] { new SqlUserRepository(), new InMemoryUserRepository() });

            // Act
            container.Verify();
        }

        [TestMethod]
        public void Verify_RegisteredCollectionWithNullElements_ThrowsException()
        {
            // Arrange
            var container = ContainerFactory.New();

            IEnumerable<IUserRepository> repositories = new IUserRepository[] { null };

            container.RegisterAll<IUserRepository>(repositories);

            try
            {
                // Act
                container.Verify();

                // Assert
                Assert.Fail("Exception expected.");
            }
            catch (InvalidOperationException ex)
            {
                AssertThat.StringContains(
                    "One of the items in the collection for type IUserRepository is a null reference.",
                    ex.Message);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Verify_FailingCollection_ThrowsException()
        {
            // Arrange
            var container = ContainerFactory.New();

            IEnumerable<IUserRepository> repositories =
                from nullRepository in Enumerable.Repeat<IUserRepository>(null, 1)
                where nullRepository.ToString() == "This line fails with an NullReferenceException"
                select nullRepository;

            container.RegisterAll<IUserRepository>(repositories);

            // Act
            container.Verify();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Verify_RegisterCalledWithFuncReturningNullInstances_ThrowsExpectedException()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.Register<IUserRepository>(() => null);

            // Act
            container.Verify();
        }
        
        [TestMethod]
        public void Verify_GetRegistrationCalledOnUnregisteredAbstractType_Succeeds()
        {
            // Arrange
            var container = ContainerFactory.New();

            // This call forces the registration of a null reference to speed up performance.
            container.GetRegistration(typeof(IUserRepository));

            // Act
            container.Verify();
        }

        [TestMethod]
        public void Register_WithAnOverrideCalledAfterACallToVerify_FailsWithTheExpectedException()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.Options.AllowOverridingRegistrations = true;

            container.Register<IUserRepository, SqlUserRepository>();
            
            container.Verify();

            try
            {
                // Act
                container.RegisterSingle<IUserRepository, SqlUserRepository>();

                // Assert
                Assert.Fail("Exception expected.");
            }
            catch (InvalidOperationException ex)
            {
                AssertThat.ExceptionMessageContains("The container can't be changed", ex);
            }
        }

        [TestMethod]
        public void ResolveUnregisteredType_CalledAfterACallToVerify_FailsWithTheExpectedMessage()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.Verify();

            try
            {
                // Act
                container.ResolveUnregisteredType += (s, e) => { };

                // Assert
                Assert.Fail("Exception expected.");
            }
            catch (InvalidOperationException ex)
            {
                AssertThat.ExceptionMessageContains("The container can't be changed", ex);
            }
        }

        [TestMethod]
        public void ExpressionBuilding_CalledAfterACallToVerify_FailsWithTheExpectedMessage()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.Verify();

            try
            {
                // Act
                container.ExpressionBuilding += (s, e) => { };

                // Assert
                Assert.Fail("Exception expected.");
            }
            catch (InvalidOperationException ex)
            {
                AssertThat.ExceptionMessageContains("The container can't be changed", ex);
            }
        }

        [TestMethod]
        public void ExpressionBuilt_CalledAfterACallToVerify_FailsWithTheExpectedMessage()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.Verify();

            try
            {
                // Act
                container.ExpressionBuilt += (s, e) => { };

                // Assert
                Assert.Fail("Exception expected.");
            }
            catch (InvalidOperationException ex)
            {
                AssertThat.ExceptionMessageContains("The container can't be changed", ex);
            }
        }
        
        [TestMethod]
        public void Verify_RegisterAllCalledWithUnregisteredType_ThrowsExpectedException()
        {
            // Arrange
            string expectedException = "No registration for type IUserRepository could be found.";

            var container = ContainerFactory.New();

            var types = new[] { typeof(SqlUserRepository), typeof(IUserRepository) };

            container.RegisterAll<IUserRepository>(types);

            try
            {
                // Act
                container.Verify();

                Assert.Fail("Exception expected.");
            }
            catch (InvalidOperationException ex)
            {
                string actualMessage = ex.Message;

                AssertThat.StringContains(expectedException, actualMessage, "Info:\n" + ex.ToString());
            }
        }

        [TestMethod]
        public void Verify_OnCollection_IteratesTheCollectionOnce()
        {
            // Arrange
            const int ExpectedNumberOfCreatedPlugins = 1;
            int actualNumberOfCreatedPlugins = 0;

            var container = ContainerFactory.New();

            container.Register<PluginImpl>();

            container.RegisterAll<IPlugin>(typeof(PluginImpl));

            container.RegisterInitializer<PluginImpl>(plugin => actualNumberOfCreatedPlugins++);
            
            // Act
            container.Verify();

            // Assert
            Assert.AreEqual(ExpectedNumberOfCreatedPlugins, actualNumberOfCreatedPlugins);
        }

        [TestMethod]
        public void Verify_CollectionWithDecoratorThatCanNotBeCreatedAtRuntime_ThrowsInvalidOperationException()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterAll<IPlugin>(typeof(PluginImpl));

            // FailingConstructorDecorator constructor throws an exception.
            container.RegisterDecorator(typeof(IPlugin), typeof(FailingConstructorPluginDecorator));

            // Act
            Action action = () => container.Verify();

            // Assert
            AssertThat.Throws<InvalidOperationException>(action);
        }

        [TestMethod]
        public void Verify_RegistrationWithDecoratorThatCanNotBeCreatedAtRuntimeAndBuildExpressionCalledExplicitly_ThrowsInvalidOperationException()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.Register<IPlugin, PluginImpl>();

            container.RegisterDecorator(typeof(IPlugin), typeof(FailingConstructorPluginDecorator));

            container.GetRegistration(typeof(IPlugin)).BuildExpression();

            // Act
            Action action = () => container.Verify();

            // Assert
            // This test verifies a bug: Calling InstanceProducer.BuildExpression flagged the producer to be 
            // skipped when calling Verify() while it was still possible that creating the instance would fail.
            AssertThat.Throws<InvalidOperationException>(action,
                "The call to BuildExpression should not trigger the verification of IPlugin to be skipped.");
        }
        
        [TestMethod]
        public void Verify_WithCollectionsResolvedThroughUnregisteredTypeResolution_StillVerifiesThoseCollections()
        {
            // Arrange
            // All these collection are resolved through unregistered type resolution.
            var expectedTypes = new[]
            {
                typeof(IEnumerable<Service<Service<Service<IDisposable>>>>),
                typeof(IEnumerable<Service<Service<IDisposable>>>),
                typeof(IEnumerable<Service<IDisposable>>),
                typeof(IEnumerable<IDisposable>),
            };

            var container = ContainerFactory.New();

            // Service<T> depends on IEnumerable<T>
            container.RegisterAllOpenGeneric(typeof(Service<>), typeof(Service<>));

            container.Register<Service<Service<Service<Service<IDisposable>>>>>();

            container.Verify();

            // Act
            var registrations = container.GetCurrentRegistrations();
            var actualTypes = registrations.Select(p => p.ServiceType).ToList();

            // Assert
            var missingTypes = expectedTypes.Where(registration => !actualTypes.Contains(registration));

            // When the missingTypes list is empty, this means that the container kept looking for new 
            // registrations at the end of the verification process.
            Assert.IsFalse(missingTypes.Any(), "Missing registrations: " + missingTypes.ToFriendlyNamesText());
        }

        [TestMethod]
        public void Verify_DecoratorWithDecorateeFactoryWithFailingDecorateeOfNonRootType_ThrowsExpectedException()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.Register<PluginConsumer>();

            container.Register<IPlugin, FailingConstructorPlugin<Exception>>();

            container.RegisterSingleDecorator(typeof(IPlugin), typeof(PluginProxy));

            // Act
            Action action = () => container.Verify();

            // Assert
            AssertThat.Throws<InvalidOperationException>(action);
        }

        [TestMethod]
        public void Verify_RegisteredSingletonDelegateResolvingAnUnregisteredConcreteType_CreatesThatTypeJustOnce()
        {
            // Arrange
            var container = new Container();

            container.RegisterSingle<IPlugin>(() => container.GetInstance<PluginWithCreationCounter>());

            // Act
            container.Verify();

            // Assert
            Assert.AreEqual(1, PluginWithCreationCounter.InstanceCount);
        }

        [TestMethod]
        public void Verify_DecoratorWithFuncDecorateeWithFailingConstructor_ThrowsTheExpectedException()
        {
            // Arrange
            var container = new Container();

            container.Register<IPlugin, FailingPlugin>();
            container.RegisterSingleDecorator(typeof(IPlugin), typeof(PluginProxy));

            // This call will succeed, because it resolves: "new PluginProxy(() => new FailingPlugin())" and
            // that will not call the FailingPlugin constructor.
            container.GetInstance<IPlugin>();

            // Act
            // This should still throw an exception.
            Action action = () => container.Verify();

            // Assert
            AssertThat.Throws<InvalidOperationException>(action);
        }

        [TestMethod]
        public void Verify_RegisterAllRegistrationWithTypeReferencingAPrimitiveType_ThrowsExpectedException()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterAll<IPlugin>(typeof(PluginWithBooleanDependency));

            // Act
            Action action = () => container.Verify();
            
            // Assert
            AssertThat.ThrowsWithExceptionMessageContains<InvalidOperationException>(@"
                contains parameter 'isInUserContext' of type Boolean which can not be used for constructor 
                injection because it is a value type."
                .TrimInside(),
                action);
        }

        [TestMethod]
        public void Verify_LockedContainer_Succeeds1()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.Register<IPlugin, PluginImpl>();

            container.GetInstance<Container>();

            // Act
            // This call must succeed. Many users are depending on this behavior.
            container.Verify();
        }

        [TestMethod]
        public void Verify_LockedContainerWithRegisterAllRegisterationForOpenGenericType_Succeeds()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterAll(typeof(IEventHandler<>), new[]
            {
                typeof(StructEventHandler),
                typeof(AuditableEventEventHandler),
                typeof(AuditableEventEventHandler<>),
            });

            container.GetInstance<Container>();

            // Act
            container.Verify();
        }

        [TestMethod]
        public void Verify_ResolvingACollectionOfSingletonsBeforeAndAfterCallingVerify_ShouldStillYieldTheSameInstance()
        {
            // Arrange
            var container = ContainerFactory.New();

            container.RegisterAll(typeof(IEventHandler<>), new Type[]
            {
                typeof(StructEventHandler),
            });

            container.AppendToCollection(typeof(IEventHandler<>),
                Lifestyle.Singleton.CreateRegistration<AuditableEventEventHandler>(container));

            var handler = container.GetAllInstances<IEventHandler<AuditableEvent>>().Single();

            container.Verify();

            // Act
            var handler2 = container.GetAllInstances<IEventHandler<AuditableEvent>>().Single();

            // Assert
            Assert.AreSame(handler, handler2);
        }

        public class PluginWithBooleanDependency : IPlugin
        {
            public PluginWithBooleanDependency(bool isInUserContext)
            {
            }
        }

        public class Service<T>
        {
            public Service(IEnumerable<T> collection)
            {
            }
        }

        public sealed class FailingConstructorPluginDecorator : IPlugin
        {
            public FailingConstructorPluginDecorator(IPlugin plugin)
            {
                throw new NotImplementedException();
            }
        }

        public sealed class PluginProxy : IPlugin
        {
            public PluginProxy(Func<IPlugin> pluginFactory)
            {
            }
        }

        public sealed class FailingPlugin : IPlugin
        {
            public FailingPlugin()
            {
                throw new Exception();
            }
        }

        private sealed class PluginDecorator : IPlugin
        {
            public PluginDecorator(IPlugin plugin)
            {
            }
        }

        private sealed class FailingConstructorPlugin<TException> : IPlugin
            where TException : Exception, new()
        {
            public FailingConstructorPlugin()
            {
                throw new TException();
            }
        }

        private sealed class PluginWithCreationCounter : IPlugin
        {
            private static int counter;

            public PluginWithCreationCounter()
            {
                Interlocked.Increment(ref counter);
            }

            public static int InstanceCount
            {
                get { return counter; }
            }
        }

        private sealed class PluginConsumer
        {
            public PluginConsumer(IPlugin plugin)
            {
            }
        }
    }
}