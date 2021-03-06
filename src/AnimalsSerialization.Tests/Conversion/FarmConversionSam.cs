﻿using AnimalSerialization.Tests.Models;
using System;

namespace AnimalSerialization.Tests.Conversion
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class FarmConversionSam
    {

        public FarmResponse ConvertFarm(string yaml)
        {
            FarmResponse response = new FarmResponse();

            Farm<string, string> animalStringString = null;
            Farm<Dog, string> animalDogString = null;
            Farm<string, Barn> animalStringBarn = null;
            Farm<Dog, Barn> animalDogBarn = null;
            try
            {
                animalStringString = DeserializeStringString(yaml);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                try
                {
                    animalDogString = DeserializeObjectString(yaml);
                }
                catch
                {
                    try
                    {
                        animalStringBarn = DeserializeStringObject(yaml);
                    }
                    catch
                    {
                        animalDogBarn = DeserializeObjectObject(yaml);
                    }
                    Console.WriteLine(ex.ToString());
                }
            }
            if (animalStringString != null)
            {
                response.Items.Add(animalStringString.FarmItem1);
                response.AnimalLegCount += 0;
                response.Items.Add(animalStringString.FarmItem2);
                response.BuildingCount += 0;
            }
            if (animalDogString != null)
            {
                response.Items.Add(animalDogString.FarmItem1.Name);
                response.AnimalLegCount += 4;

                response.Items.Add(animalDogString.FarmItem2);
                response.BuildingCount += 0;
            }
            if (animalStringBarn != null)
            {
                response.Items.Add(animalStringBarn.FarmItem1);
                response.AnimalLegCount += 0;

                response.Items.Add(animalStringBarn.FarmItem2.BarnType);
                response.BuildingCount += 1;
                response.BarnTools.AddRange(animalStringBarn.FarmItem2.Tools);
            }
            if (animalDogBarn != null)
            {
                response.Items.Add(animalDogBarn.FarmItem1.Name);
                response.AnimalLegCount += 4;

                response.Items.Add(animalDogBarn.FarmItem2.BarnType);
                response.BuildingCount += 1;
                response.BarnTools.AddRange(animalDogBarn.FarmItem2.Tools);
            }

            return response;
        }

        //DANGER WILL ROBINSON, DANGER!!!
        private Farm<string, string> DeserializeStringString(string yaml)
        {
            return (Farm<string, string>)FarmSerialization.Deserialize<Farm<string, string>>(yaml);
        }

        private Farm<Dog, string> DeserializeObjectString(string yaml)
        {
            return FarmSerialization.Deserialize<Farm<Dog, string>>(yaml);
        }

        private Farm<string, Barn> DeserializeStringObject(string yaml)
        {
            return FarmSerialization.Deserialize<Farm<string, Barn>>(yaml);
        }

        private Farm<Dog, Barn> DeserializeObjectObject(string yaml)
        {
            return FarmSerialization.Deserialize<Farm<Dog, Barn>>(yaml);
        }
    }
}
