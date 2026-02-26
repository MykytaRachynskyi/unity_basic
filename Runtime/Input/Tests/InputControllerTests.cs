using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Basic.Input.Tests
{
    [TestFixture]
    public class InputControllerTests
    {
        public class TestInputService : InputService<TestInputActions> { }

        public class TestInputActions { }

        private IInputService<TestInputActions> _service;
        private InputRegion testRegion1;
        private InputRegion testRegion2;
        private InputRegion testRegion3;

        [SetUp]
        public void SetUp()
        {
            _service = new TestInputService();

            // Create test regions
            testRegion1 = new InputRegion("TestRegion1", GUID.Generate());
            testRegion2 = new InputRegion("TestRegion2", GUID.Generate());
            testRegion3 = new InputRegion("TestRegion3", GUID.Generate());
        }

        [TearDown]
        public void TearDown()
        {
            _service.ClearHandlers();
            _service.ClearRegions();
            _service = null;
        }

        #region Region Handler Registration Tests

        [Test]
        public void RegisterRegionHandler_AddsHandlerSuccessfully()
        {
            // Arrange
            bool callbackInvoked = false;

            // Act
            _service.RegisterRegionHandler(
                new()
                {
                    Region = testRegion1,
                    HandleInputCallback = (actions) => callbackInvoked = true,
                }
            );
            _service.PushRegion(testRegion1);
            _service.DispatchInput();

            // Assert
            Assert.IsTrue(callbackInvoked);
        }

        [Test]
        public void RegisterRegionHandler_MultipleHandlersSameRegion_BothInvoked()
        {
            // Arrange
            bool callback1Invoked = false;
            bool callback2Invoked = false;

            // Act
            _service.RegisterRegionHandler(
                new()
                {
                    Region = testRegion1,
                    HandleInputCallback = (actions) => callback1Invoked = true,
                }
            );
            _service.RegisterRegionHandler(
                new()
                {
                    Region = testRegion1,
                    HandleInputCallback = (actions) => callback2Invoked = true,
                }
            );
            _service.PushRegion(testRegion1);
            _service.DispatchInput();

            // Assert
            Assert.IsTrue(callback1Invoked);
            Assert.IsTrue(callback2Invoked);
        }

        [Test]
        public void DeregisterRegionHandler_RemovesHandlerSuccessfully()
        {
            // Arrange
            bool callbackInvoked = false;
            var handler = new InputRegionHandler<TestInputActions>
            {
                Region = testRegion1,
                HandleInputCallback = (actions) => callbackInvoked = true,
            };

            // Act
            _service.RegisterRegionHandler(handler);
            _service.DeregisterRegionHandler(handler);
            _service.PushRegion(testRegion1);
            _service.DispatchInput();

            // Assert
            Assert.IsFalse(callbackInvoked);
        }

        #endregion

        #region Region Stack Tests

        [Test]
        public void PushRegion_AddsRegionToStack()
        {
            // Act
            _service.PushRegion(testRegion1);

            // Assert
            Assert.AreEqual(1, _service.RegionStack.Count);
            Assert.AreEqual(testRegion1, _service.RegionStack[0]);
        }

        [Test]
        public void PushRegion_SameRegionTwice_DoesNotDuplicate()
        {
            // Act
            _service.PushRegion(testRegion1);
            _service.PushRegion(testRegion1);

            // Assert
            Assert.AreEqual(1, _service.RegionStack.Count);
        }

        [Test]
        public void PushRegion_ExistingRegionInStack_MovesToTop()
        {
            // Act
            _service.PushRegion(testRegion1);
            _service.PushRegion(testRegion2);
            _service.PushRegion(testRegion1);

            // Assert
            Assert.AreEqual(2, _service.RegionStack.Count);
            Assert.AreEqual(testRegion1, _service.RegionStack[^1]);
            Assert.AreEqual(testRegion2, _service.RegionStack[0]);
        }

        [Test]
        public void RemoveRegion_TopRegion_RemovesSuccessfully()
        {
            // Arrange
            _service.PushRegion(testRegion1);
            _service.PushRegion(testRegion2);

            // Act
            _service.RemoveRegion(testRegion2);

            // Assert
            Assert.AreEqual(1, _service.RegionStack.Count);
            Assert.AreEqual(testRegion1, _service.RegionStack[^1]);
        }

        [Test]
        public void RemoveRegion_MiddleRegion_RemovesWithoutAffectingCallbacks()
        {
            // Arrange
            _service.PushRegion(testRegion1);
            _service.PushRegion(testRegion2);
            _service.PushRegion(testRegion3);

            // Act
            _service.RemoveRegion(testRegion2);

            // Assert
            Assert.AreEqual(2, _service.RegionStack.Count);
            Assert.IsFalse(_service.RegionStack.Contains(testRegion2));
        }

        [Test]
        public void RemoveRegion_NonExistentRegion_DoesNothing()
        {
            // Arrange
            _service.PushRegion(testRegion1);

            // Act
            _service.RemoveRegion(testRegion2);

            // Assert
            Assert.AreEqual(1, _service.RegionStack.Count);
        }

        [Test]
        public void ClearRegions_RemovesAllRegions()
        {
            // Arrange
            _service.PushRegion(testRegion1);
            _service.PushRegion(testRegion2);

            // Act
            _service.ClearRegions();

            // Assert
            Assert.AreEqual(0, _service.RegionStack.Count);
        }

        #endregion

        #region Input Dispatch Tests

        [Test]
        public void DispatchInput_EmptyStack_DoesNotInvokeCallbacks()
        {
            // Arrange
            bool callbackInvoked = false;
            var handler = new InputRegionHandler<TestInputActions>
            {
                Region = testRegion1,
                HandleInputCallback = (actions) => callbackInvoked = true,
            };
            _service.RegisterRegionHandler(handler);

            // Act
            _service.DispatchInput();

            // Assert
            Assert.IsFalse(callbackInvoked);
        }

        [Test]
        public void DispatchInput_BlockedRegion_DoesNotInvokeCallbacks()
        {
            // Arrange
            bool callbackInvoked = false;
            var handler = new InputRegionHandler<TestInputActions>
            {
                Region = testRegion1,
                HandleInputCallback = (actions) => callbackInvoked = true,
            };
            _service.RegisterRegionHandler(handler);
            _service.PushRegion(testRegion1);

            // Act
            _service.PushRegion(InputRegions.BLOCKED);
            _service.DispatchInput();

            // Assert
            Assert.IsFalse(callbackInvoked);
        }

        [Test]
        public void DispatchInput_OnlyTopRegionReceivesInput()
        {
            // Arrange
            bool region1Called = false;
            bool region2Called = false;

            var handler1 = new InputRegionHandler<TestInputActions>
            {
                Region = testRegion1,
                HandleInputCallback = (actions) => region1Called = true,
            };

            var handler2 = new InputRegionHandler<TestInputActions>
            {
                Region = testRegion2,
                HandleInputCallback = (actions) => region2Called = true,
            };

            _service.RegisterRegionHandler(handler1);
            _service.RegisterRegionHandler(handler2);
            _service.PushRegion(testRegion1);
            _service.PushRegion(testRegion2);

            // Act
            _service.DispatchInput();

            // Assert
            Assert.IsFalse(region1Called);
            Assert.IsTrue(region2Called);
        }

        #endregion

        #region Callback Tests

        [Test]
        public void PushRegion_InvokesRegionEnteredCallback()
        {
            // Arrange
            bool enteredCallbackInvoked = false;
            var handler = new InputRegionHandler<TestInputActions>
            {
                Region = testRegion1,
                RegionEnteredCallback = () => enteredCallbackInvoked = true,
            };
            _service.RegisterRegionHandler(handler);

            // Act
            _service.PushRegion(testRegion1);

            // Assert
            Assert.IsTrue(enteredCallbackInvoked);
        }

        [Test]
        public void PushRegion_InvokesRegionExitedCallbackOnPreviousRegion()
        {
            // Arrange
            bool region1Exited = false;
            bool region2Entered = false;

            var handler1 = new InputRegionHandler<TestInputActions>
            {
                Region = testRegion1,
                RegionExitedCallback = () => region1Exited = true,
            };

            var handler2 = new InputRegionHandler<TestInputActions>
            {
                Region = testRegion2,
                RegionEnteredCallback = () => region2Entered = true,
            };

            _service.RegisterRegionHandler(handler1);
            _service.RegisterRegionHandler(handler2);
            _service.PushRegion(testRegion1);

            // Act
            _service.PushRegion(testRegion2);

            // Assert
            Assert.IsTrue(region1Exited);
            Assert.IsTrue(region2Entered);
        }

        [Test]
        public void RemoveRegion_TopRegion_InvokesExitAndEnterCallbacks()
        {
            // Arrange
            bool region1Entered = false;
            bool region2Exited = false;

            var handler1 = new InputRegionHandler<TestInputActions>
            {
                Region = testRegion1,
                RegionEnteredCallback = () => region1Entered = true,
            };

            var handler2 = new InputRegionHandler<TestInputActions>
            {
                Region = testRegion2,
                RegionExitedCallback = () => region2Exited = true,
            };

            _service.RegisterRegionHandler(handler1);
            _service.RegisterRegionHandler(handler2);
            _service.PushRegion(testRegion1);
            _service.PushRegion(testRegion2);

            region1Entered = false; // Reset after initial push

            // Act
            _service.RemoveRegion(testRegion2);

            // Assert
            Assert.IsTrue(region2Exited);
            Assert.IsTrue(region1Entered);
        }

        [Test]
        public void RemoveRegion_MiddleRegion_DoesNotInvokeCallbacks()
        {
            // Arrange
            bool region2Exited = false;
            bool region3Entered = false;

            var handler2 = new InputRegionHandler<TestInputActions>
            {
                Region = testRegion2,
                RegionExitedCallback = () => region2Exited = true,
            };

            var handler3 = new InputRegionHandler<TestInputActions>
            {
                Region = testRegion3,
                RegionEnteredCallback = () => region3Entered = true,
            };

            _service.RegisterRegionHandler(handler2);
            _service.RegisterRegionHandler(handler3);
            _service.PushRegion(testRegion1);
            _service.PushRegion(testRegion2);
            _service.PushRegion(testRegion3);

            region2Exited = false; // Reset after initial push
            region3Entered = false;

            // Act
            _service.RemoveRegion(testRegion2);

            // Assert
            Assert.IsFalse(region2Exited);
            Assert.IsFalse(region3Entered);
        }

        #endregion

        #region InputRegion Tests

        [Test]
        public void InputRegion_Equality_WorksCorrectly()
        {
            // Arrange
            var guid = GUID.Generate();
            var region1 = new InputRegion("Test", guid);
            var region2 = new InputRegion("Test", guid);

            // Assert
            Assert.IsTrue(region1 == region2);
            Assert.IsTrue(region1.Equals(region2));
        }

        [Test]
        public void InputRegion_Inequality_WorksCorrectly()
        {
            // Arrange
            var region1 = new InputRegion("Test1", GUID.Generate());
            var region2 = new InputRegion("Test2", GUID.Generate());

            // Assert
            Assert.IsTrue(region1 != region2);
            Assert.IsFalse(region1.Equals(region2));
        }

        [Test]
        public void InputRegion_ToString_ReturnsExpectedFormat()
        {
            // Arrange
            var guid = GUID.Generate();
            var region = new InputRegion("TestRegion", guid);

            // Act
            var result = region.ToString();

            // Assert
            Assert.IsTrue(result.Contains("TestRegion"));
        }

        [Test]
        public void InputRegion_GetHashCode_UsesGuidHashCode()
        {
            // Arrange
            var guid = GUID.Generate();
            var region = new InputRegion("Test", guid);

            // Assert
            Assert.AreEqual(guid.GetHashCode(), region.GetHashCode());
        }

        #endregion

        #region Integration Tests

        [Test]
        public void CompleteWorkflow_PushMultipleRegionsAndDispatch()
        {
            // Arrange
            var callOrder = new List<string>();

            var handler1 = new InputRegionHandler<TestInputActions>
            {
                Region = testRegion1,
                RegionEnteredCallback = () => callOrder.Add("Region1-Enter"),
                RegionExitedCallback = () => callOrder.Add("Region1-Exit"),
                HandleInputCallback = (actions) => callOrder.Add("Region1-Input"),
            };

            var handler2 = new InputRegionHandler<TestInputActions>
            {
                Region = testRegion2,
                RegionEnteredCallback = () => callOrder.Add("Region2-Enter"),
                RegionExitedCallback = () => callOrder.Add("Region2-Exit"),
                HandleInputCallback = (actions) => callOrder.Add("Region2-Input"),
            };

            _service.RegisterRegionHandler(handler1);
            _service.RegisterRegionHandler(handler2);

            // Act
            _service.PushRegion(testRegion1);
            _service.DispatchInput();
            _service.PushRegion(testRegion2);
            _service.DispatchInput();
            _service.RemoveRegion(testRegion2);
            _service.DispatchInput();

            // Assert
            Assert.AreEqual(8, callOrder.Count);
            Assert.AreEqual("Region1-Enter", callOrder[0]);
            Assert.AreEqual("Region1-Input", callOrder[1]);
            Assert.AreEqual("Region1-Exit", callOrder[2]);
            Assert.AreEqual("Region2-Enter", callOrder[3]);
            Assert.AreEqual("Region2-Input", callOrder[4]);
            Assert.AreEqual("Region2-Exit", callOrder[5]);
            Assert.AreEqual("Region1-Enter", callOrder[6]);
            Assert.AreEqual("Region1-Input", callOrder[7]);
        }

        #endregion
    }
}
