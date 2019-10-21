using AzurePipelinesToGitHubActionsConverter.Core;
using AzurePipelinesToGitHubActionsConverter.Core.AzurePipelines;
using AzurePipelinesToGitHubActionsConverter.Core.GitHubActions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using YamlDotNet.Serialization;

namespace AzurePipelinesToGitHubActionsConverter.Tests
{
    [TestClass]
    public class StrategyTest
    {

        [TestMethod]
        public void TestStrategy()
        {
            //strategy:
            //  matrix:
            //    linux:
            //      imageName: "ubuntu-16.04"
            //    mac:
            //      imageName: "macos-10.13"
            //    windows:
            //      imageName: "vs2017-win2016"

            //Arrange
            Conversion conversion = new Conversion();
            string yaml = @"
trigger:
- master
strategy:
  matrix:
    linux:
      imageName: ubuntu-16.04
    mac:
      imageName: macos-10.13
    windows:
      imageName: vs2017-win2016
jobs:
- job: Build
  displayName: Build job
  pool: 
    vmImage: $(imageName)
  variables:
    buildConfiguration: Debug
  steps: 
  - script: dotnet build WebApplication1/WebApplication1.Service/WebApplication1.Service.csproj --configuration $(buildConfiguration) 
    displayName: dotnet build part 1
";

            //Act
            string output = conversion.ConvertAzurePipelineToGitHubAction(yaml);

            //Assert
            //Note that we are using the longer form, as sequence flow (showing an array like: [ubuntu-16.04, macos-10.13, vs2017-win2016]), doesn't exist in this YAML Serializer yet.
            string expectedOutput = @"
on:
  push:
    branches:
    - master
jobs:
  Build:
    name: Build job
    runs-on: ${{ matrix.imageName }}
    strategy:
      matrix:
        imageName:
        - ubuntu-16.04
        - macos-10.13
        - vs2017-win2016
    env:
      buildConfiguration: Debug
    steps:
    - uses: actions/checkout@v1
    - name: dotnet build part 1
      run: dotnet build WebApplication1/WebApplication1.Service/WebApplication1.Service.csproj --configuration $buildConfiguration
";
            Assert.AreEqual(Environment.NewLine + output, expectedOutput);
        }

    }
}