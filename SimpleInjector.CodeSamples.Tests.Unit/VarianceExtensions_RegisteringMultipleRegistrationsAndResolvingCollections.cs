﻿namespace SimpleInjector.CodeSamples.Tests.Unit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SimpleInjector.Extensions;

    /// <summary>
    /// Variance Scenario 3: Multiple registrations, multiple resolve.
    /// Per service, multiple types are registered and multiple types are resolved or used behind
    /// the covers.
    /// </summary>
    [TestClass]
    public class VarianceExtensions_RegisteringMultipleRegistrationsAndResolvingCollections
    {
        // Each test gets its own test class instance and therefore its own new container and logger.
        private readonly Container container = new Container();
        private readonly ListLogger logger = new ListLogger();

        public VarianceExtensions_RegisteringMultipleRegistrationsAndResolvingCollections()
        {
            // Container configuration.
            BatchRegistrationCallback callback = (serviceType, implementations) =>
            {
                container.RegisterAll(serviceType, implementations);
            };

            this.container.RegisterManyForOpenGeneric(typeof(IEventHandler<>), callback,
                typeof(IEventHandler<>).Assembly);

            this.container.RegisterSingleOpenGeneric(typeof(IEventHandler<>),
                typeof(MultipleDispatchEventHandler<>));

            // The ILogger is used by the unit tests to test the configuration.
            this.container.RegisterSingle<ILogger>(this.logger);
        }

        public interface IEventHandler<in TEvent>
        {
            void Handle(TEvent e);
        }

        [TestMethod]
        public void Handle_CustomerMovedEvent_ExecutesExpectedEventHandlers()
        {
            // Arrange
            var handler = this.container.GetInstance<IEventHandler<CustomerMovedEvent>>();

            // Act
            handler.Handle(new CustomerMovedEvent());

            // Assert
            Assert.AreEqual(2, this.logger.Count, this.logger.ToString());
            Assert.IsTrue(this.logger.Contains("CustomerMovedEventHandler handled CustomerMovedEvent"), this.logger.ToString());
            Assert.IsTrue(this.logger.Contains("NotifyStaffWhenCustomerMovedEventHandler handled CustomerMovedEvent"), this.logger.ToString());
        }

        [TestMethod]
        public void Handle_SpecialCustomerMovedEvent_ExecutesExpectedEventHandlers()
        {
            // Arrange
            var handler = this.container.GetInstance<IEventHandler<SpecialCustomerMovedEvent>>();

            handler.Handle(new SpecialCustomerMovedEvent());

            // Assert
            Assert.AreEqual(2, this.logger.Count);
            Assert.IsTrue(this.logger.Contains("CustomerMovedEventHandler handled SpecialCustomerMovedEvent"), this.logger.ToString());
            Assert.IsTrue(this.logger.Contains("NotifyStaffWhenCustomerMovedEventHandler handled SpecialCustomerMovedEvent"), this.logger.ToString());
        }

        [TestMethod]
        public void Handle_CustomerMovedAbroadEvent_ExecutesExpectedEventHandlers()
        {
            // Arrange
            var handler = this.container.GetInstance<IEventHandler<CustomerMovedAbroadEvent>>();

            // Act
            handler.Handle(new CustomerMovedAbroadEvent());

            // Assert
            Assert.AreEqual(3, this.logger.Count, this.logger.ToString());

            Assert.IsTrue(this.logger.Contains("CustomerMovedEventHandler handled CustomerMovedAbroadEvent"), this.logger.ToString());
            Assert.IsTrue(this.logger.Contains("NotifyStaffWhenCustomerMovedEventHandler handled CustomerMovedAbroadEvent"), this.logger.ToString());
            Assert.IsTrue(this.logger.Contains("CustomerMovedAbroadEventHandler handled CustomerMovedAbroadEvent"), this.logger.ToString());
        }

        [TestMethod]
        public void MultipleDispatchEventHandler_Always_UsesTransientHandlers()
        {
            // Arrange
            var handler = this.container.GetInstance<MultipleDispatchEventHandler<CustomerMovedEvent>>();

            // Assert
            Assert.AreEqual(2, handler.Handlers.Count());
            Assert.AreEqual(0, handler.Handlers.Intersect(handler.Handlers).Count(),
                "The wrapped handlers are expected to be registered as transient, but they are singletons.");
        }

        public class CustomerMovedEvent
        {
        }

        public class CustomerMovedAbroadEvent : CustomerMovedEvent
        {
        }

        public class SpecialCustomerMovedEvent : CustomerMovedEvent
        {
        }

        public class CustomerMovedEventHandler : IEventHandler<CustomerMovedEvent>
        {
            private readonly ILogger logger;

            public CustomerMovedEventHandler(ILogger logger)
            {
                this.logger = logger;
            }

            public void Handle(CustomerMovedEvent e)
            {
                this.logger.Log(this.GetType().Name + " handled " + e.GetType().Name);
            }
        }

        public class NotifyStaffWhenCustomerMovedEventHandler : IEventHandler<CustomerMovedEvent>
        {
            private readonly ILogger logger;

            public NotifyStaffWhenCustomerMovedEventHandler(ILogger logger)
            {
                this.logger = logger;
            }

            public void Handle(CustomerMovedEvent e)
            {
                this.logger.Log(this.GetType().Name + " handled " + e.GetType().Name);
            }
        }

        public class CustomerMovedAbroadEventHandler : IEventHandler<CustomerMovedAbroadEvent>
        {
            private readonly ILogger logger;

            public CustomerMovedAbroadEventHandler(ILogger logger)
            {
                this.logger = logger;
            }

            public void Handle(CustomerMovedAbroadEvent e)
            {
                this.logger.Log(this.GetType().Name + " handled " + e.GetType().Name);
            }
        }

        public sealed class MultipleDispatchEventHandler<TEvent> : IEventHandler<TEvent>
        {
            public readonly IEnumerable<IEventHandler<TEvent>> Handlers;

            public MultipleDispatchEventHandler(IEnumerable<IEventHandler<TEvent>> handlers)
            {
                this.Handlers = handlers;
            }

            void IEventHandler<TEvent>.Handle(TEvent e)
            {
                foreach (var handler in this.Handlers)
                {
                    handler.Handle(e);
                }
            }
        }
    }
}