using AnimalSerialization.Tests.Conversion;
using AnimalsSerialization.Tests.SampleDocs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace AnimalSerialization.Tests
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TestClass]
    public class SamFarmTests
    {
        [TestMethod]
        public void SamAnimalStringStringTest()
        {
            //Arrange
            FarmConversionSam conversion = new FarmConversionSam();
            string yaml = YAMLDocs.AnimalGenericYaml;

            //Act
            FarmResponse response = conversion.ConvertFarm(yaml);

            //Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(2, response.Items.Count);
            Assert.AreEqual("dogstring", response.Items[0]);
            Assert.AreEqual("barnstring", response.Items[1]);
            Assert.AreEqual(0, response.AnimalLegCount);
            Assert.AreEqual(0, response.BuildingCount);
        }

        [TestMethod]
        public void SamAnimalDogStringTest()
        {
            //Arrange
            FarmConversionSam conversion = new FarmConversionSam();
            string yaml = YAMLDocs.AnimalDogYaml;

            //Act
            FarmResponse response = conversion.ConvertFarm(yaml);

            //Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(2, response.Items.Count);
            Assert.AreEqual("Rover", response.Items[0]);
            Assert.AreEqual("barnstring", response.Items[1]);
            Assert.AreEqual(4, response.AnimalLegCount);
            Assert.AreEqual(0, response.BuildingCount);
        }

        [TestMethod]
        public void SamAnimalStringBarnTest()
        {
            //Arrange
            FarmConversionSam conversion = new FarmConversionSam();
            string yaml = YAMLDocs.AnimalBarnYaml;

            //Act
            FarmResponse response = conversion.ConvertFarm(yaml);

            //Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(2, response.Items.Count);
            Assert.AreEqual("dogstring", response.Items[0]);
            Assert.AreEqual("New England barn", response.Items[1]);
            Assert.AreEqual(0, response.AnimalLegCount);
            Assert.AreEqual(1, response.BuildingCount);
        }

        [TestMethod]
        public void SamAnimalDogBarnTest()
        {
            //Arrange
            FarmConversionSam conversion = new FarmConversionSam();
            string yaml = YAMLDocs.AnimalDogBarnYaml;

            //Act
            FarmResponse response = conversion.ConvertFarm(yaml);

            //Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(2, response.Items.Count);
            Assert.AreEqual("Rover", response.Items[0]);
            Assert.AreEqual("New England barn", response.Items[1]);
            Assert.AreEqual(4, response.AnimalLegCount);
            Assert.AreEqual(1, response.BuildingCount);
        }

        //This test is meant to fail, the code is not expecting the Barn/dog combination. 
        //By a quirk of the branching, it will jump into the last Dog/Barn combination, but as the barn object only matches color, most of the object will be null
        [TestMethod]
        public void SamInvalidAnimalDogTractorTest()
        {
            //Arrange
            FarmConversionSam conversion = new FarmConversionSam();
            string yaml = YAMLDocs.AnimalDogTractorYaml;
            FarmResponse response = null;

            //Act
            try
            {
                response = conversion.ConvertFarm(yaml);
            }
            catch (Exception ex)
            {
                Assert.IsNotNull(ex);
            }

            //Assert
            Assert.IsNull(response);
        }

    }
}