﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CuttingEdge.ServiceLocation.Tests.Unit
{
    [TestClass]
    public class RegisterByGenericArgumentTests
    {
        [TestMethod]
        public void RegisterByGenericArgument_WithValidGenericArguments_Succeeds()
        {
            // Arrange
            var container = new SimpleServiceLocator();

            // Act
            container.Register<IWeapon, Katana>();
        }

        [TestMethod]
        public void GetInstance_OnRegisteredType_ReturnsInstanceOfExpectedType()
        {
            // Arrange
            var container = new SimpleServiceLocator();

            container.Register<IWeapon, Katana>();

            // Act
            var instance = container.GetInstance<IWeapon>();

            // Assert
            Assert.IsInstanceOfType(instance, typeof(Katana));
        }

        [TestMethod]
        public void GetInstance_OnRegisteredType_ReturnsANewInstanceOnEachCall()
        {
            // Arrange
            var container = new SimpleServiceLocator();

            container.Register<IWeapon, Tanto>();

            // Act
            var instance1 = container.GetInstance<IWeapon>();
            var instance2 = container.GetInstance<IWeapon>();

            // Assert
            Assert.AreNotEqual(instance1, instance2, "Register<TService, TImplementation>() should " + 
                "return transient objects.");
        }

        [TestMethod]
        public void RegisterByGenericArgument_GenericArgumentOfInvalidType_ThrowsException()
        {
            // Arrange
            var container = new SimpleServiceLocator();

            try
            {
                // Act
                container.Register<object, ConcreteTypeWithValueTypeConstructorArgument>();

                // Assert
                Assert.Fail("Registration of ConcreteTypeWithValueTypeConstructorArgument should fail.");
            }
            catch (ArgumentException ex)
            {
                Assert.IsInstanceOfType(ex, typeof(ArgumentException), "No subtype was expected.");

                Assert.AreEqual(ex.ParamName, "TImplementation");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void RegisterByGenericArgument_CalledAfterTheContainerWasLocked_ThrowsException()
        {
            // Arrange
            var container = new SimpleServiceLocator();

            container.GetInstance<PluginManager>();

            // Act
            container.Register<IWeapon, Katana>();
        }
    }
}