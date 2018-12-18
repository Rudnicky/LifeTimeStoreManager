using DgtPoC;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
    /// <summary>
    /// Unit tests for the LifeTimeStorage class
    /// Naming Convention: MethodName_StateUnderTest_ExpectedBehavior
    /// </summary>
    [TestClass]
    public class LifeTimeStorageTests
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        [TestMethod]
        public void Put_IsObjectInserted_ShouldReturnTrue()
        {
            // Arrange
            var lifeTimeStorage = new LifeTimeStorage<string>(_log, "TEST", 10, 0);

            // Act
            lifeTimeStorage.Put("test_key", "test_tag");
            var storedObject = lifeTimeStorage.Get("test_key");
            lifeTimeStorage.Remove("test_key");

            // Assert
            Assert.IsTrue(storedObject != null);
            lifeTimeStorage.Clear();
        }

        [TestMethod]
        public void Remove_IsObjectRemovedCorrectly_ShouldReturnTrue()
        {
            // Arrange
            var lifeTimeStorage = new LifeTimeStorage<string>(_log, "TEST", 10, 0);

            // Act
            lifeTimeStorage.Put("test_key", "test_tag");
            lifeTimeStorage.Remove("test_key");

            bool isStoredObjectStillThere = lifeTimeStorage.Exists("test_key");

            // Assert
            Assert.IsTrue(!isStoredObjectStillThere);
            lifeTimeStorage.Clear();
        }

        [TestMethod]
        public void Get_StoredObject_ShouldReturnSameObject()
        {
            // Arrange
            var lifeTimeStorage = new LifeTimeStorage<string>(_log, "TEST", 10, 0);

            // Act
            lifeTimeStorage.Put("test_key", "test_tag");
            var storedObject = lifeTimeStorage.Get("test_key");

            // Assert
            Assert.IsTrue(storedObject != null && storedObject == "test_tag");
            lifeTimeStorage.Clear();
        }

        [TestMethod]
        public void GetValueList_AfterAddingCoupleOfObjects_ShouldReturnCountOfAddedObjects()
        {
            // Arrange
            var lifeTimeStorage = new LifeTimeStorage<string>(_log, "TEST", 10, 0);

            // Act
            lifeTimeStorage.Put("test_key", "test_tag");
            lifeTimeStorage.Put("test_key2", "test_tag2");
            lifeTimeStorage.Put("test_key2", "test_tag2");

            var listOfStoredObjects = lifeTimeStorage.GetValueList();

            // Assert
            Assert.IsTrue(listOfStoredObjects.Count == 2);
            lifeTimeStorage.Clear();
        }

        [TestMethod]
        public void Clear_AfterDroppingDatabaseEntites_ShouldReturnTrue()
        {
            // Arrange
            var lifeTimeStorage = new LifeTimeStorage<string>(_log, "TEST", 10, 0);

            // Act
            lifeTimeStorage.Put("test_key", "test_tag");
            lifeTimeStorage.Put("test_key2", "test_tag2");
            lifeTimeStorage.Put("test_key2", "test_tag2");
            lifeTimeStorage.Clear();

            var listOfStoredObjects = lifeTimeStorage.GetValueList();

            // Assert
            Assert.IsTrue(listOfStoredObjects.Count == 0);
            lifeTimeStorage.Clear();
        }

        [TestMethod]
        public void GetDictionary_InsertedDictionary_ShouldReturnTrue()
        {
            // Arrange
            var lifeTimeStorage = new LifeTimeStorage<string>(_log, "TEST", 10, 0);
            var value = "test_tag";

            // Act
            lifeTimeStorage.Put("test_key", "test_tag");
            var dictionary = lifeTimeStorage.GetDictionary();
            var exists = dictionary.TryGetValue("test_key", out value);

            // Assert
            Assert.IsTrue(exists);
            lifeTimeStorage.Clear();
        }

        [TestMethod]
        public void Put_ThreeItemsAndSetDestroyingObjectAfterEach5Seconds_ShouldReturnTrue()
        {
            // Arrange
            var lifeTimeStorage = new LifeTimeStorage<string>(_log, "TEST", 10, 5);

            // Act
            lifeTimeStorage.Put("test_key", "test_tag");
            Task.Delay(1000);

            lifeTimeStorage.Put("test_key2", "test_tag2");
            Task.Delay(1000);

            lifeTimeStorage.Put("test_key2", "test_tag2");
            Task.Delay(1000);

            var listOfStoredObjects = lifeTimeStorage.GetValueList();

            // Assert
            Assert.IsTrue(listOfStoredObjects.Count == 2);
            lifeTimeStorage.Clear();
            lifeTimeStorage.RemoveJobs();
        }

        [TestMethod]
        public void Put_ThreeItemsAndSetDestroyingObjectAfterEachSecond_ShouldReturnTrue()
        {
            // Arrange
            var lifeTimeStorage = new LifeTimeStorage<string>(_log, "TEST", 10, 1);

            // Act
            lifeTimeStorage.Put("test_key", "test_tag");
            Thread.Sleep(1000);

            lifeTimeStorage.Put("test_key2", "test_tag2");
            Thread.Sleep(1000);

            lifeTimeStorage.Put("test_key2", "test_tag2");
            Thread.Sleep(1000);

            var listOfStoredObjects = lifeTimeStorage.GetValueList();

            // Assert
            Assert.IsTrue(listOfStoredObjects.Count == 0);
            lifeTimeStorage.Clear();
            lifeTimeStorage.RemoveJobs();
        }

        [TestMethod]
        public void Put_CoupleOfItemsAtOnce_ShouldReturnCountEqualZero()
        {
            // Arrange
            var lifeTimeStorage = new LifeTimeStorage<string>(_log, "TEST", 10, 1);

            // Act
            lifeTimeStorage.Put("test_key", "test_tag");
            lifeTimeStorage.Put("test_key2", "test_tag2");
            lifeTimeStorage.Put("test_key3", "test_tag3");
            lifeTimeStorage.Put("test_key4", "test_tag4");
            lifeTimeStorage.Put("test_key5", "test_tag5");
            lifeTimeStorage.Put("test_key6", "test_tag6");
            Thread.Sleep(2000);

            var listOfStoredObjects = lifeTimeStorage.GetValueList();

            // Assert
            Assert.IsTrue(listOfStoredObjects.Count == 0);
            lifeTimeStorage.Clear();
            lifeTimeStorage.RemoveJobs();
        }
    }
}
