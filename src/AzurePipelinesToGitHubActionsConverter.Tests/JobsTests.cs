using AzurePipelinesToGitHubActionsConverter.Core.Conversion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AzurePipelinesToGitHubActionsConverter.Tests
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TestClass]
    public class JobsTests
    {

        [TestMethod]
        public void SimpleJobTest()
        {
            //Arrange
            Conversion conversion = new Conversion();
            string yaml = @"
jobs:
- job: Build
  displayName: 'Build job'
  pool:
    vmImage: 'windows-latest'
  steps:
  - task: CmdLine@2
    inputs:
      script: echo your commands here 
";

            //Act
            ConversionResponse gitHubOutput = conversion.ConvertAzurePipelineToGitHubAction(yaml);

            //Assert
            string expected = @"
jobs:
  Build:
    name: Build job
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v2
    - run: echo your commands here
      shell: cmd
";

            expected = UtilityTests.TrimNewLines(expected);
            Assert.AreEqual(expected, gitHubOutput.actionsYaml);
            Assert.AreEqual(true, gitHubOutput.v2ConversionSuccessful);
        }

        [TestMethod]
        public void SimpleVariablesJobTest()
        {
            //Arrange
            Conversion conversion = new Conversion();
            string yaml = @"
jobs:
- job: Build
  displayName: 'Build job'
  pool:
    vmImage: windows-latest
  variables:
    Variable1: 'new variable'
  steps:
  - task: CmdLine@2
    inputs:
      script: echo your commands here $(Variable1)
";

            //Act
            ConversionResponse gitHubOutput = conversion.ConvertAzurePipelineToGitHubAction(yaml);

            //Assert
            string expected = @"
jobs:
  Build:
    name: Build job
    runs-on: windows-latest
    env:
      Variable1: new variable
    steps:
    - uses: actions/checkout@v2
    - run: echo your commands here ${{ env.Variable1 }}
      shell: cmd
";

            expected = UtilityTests.TrimNewLines(expected);
            Assert.AreEqual(expected, gitHubOutput.actionsYaml);
            Assert.AreEqual(true, gitHubOutput.v2ConversionSuccessful);
        }


        //This test doesn't work with V1
        [TestMethod]
        public void ComplexVariablesWithComplexDependsOnJobTest()
        {
            //Arrange
            Conversion conversion = new Conversion();
            string yaml = @"
  jobs:
  - job: Build
    displayName: 'Build job'
    pool:
        vmImage: 'windows-latest'
    dependsOn: 
    - AnotherJob
    variables:
    - group: Active Login   # Contains codesigningCertPassword: Password for code signing cert
    - name: sourceArtifactName
      value: 'nuget-windows'
    - name: targetArtifactName
      value: 'nuget-windows-signed'
    - name: pathToNugetPackages
      value: '**/*.nupkg'

    steps:
    - task: CmdLine@2
      inputs:
        script: echo your commands here 
";

            //Act
            ConversionResponse gitHubOutput = conversion.ConvertAzurePipelineToGitHubAction(yaml);

            //Assert
            string expected = @"
jobs:
  Build:
    name: Build job
    runs-on: windows-latest
    needs:
    - AnotherJob
    env:
      group: Active Login
      sourceArtifactName: nuget-windows
      targetArtifactName: nuget-windows-signed
      pathToNugetPackages: '**/*.nupkg'
    steps:
    - uses: actions/checkout@v2
    - run: echo your commands here
      shell: cmd
";
            expected = UtilityTests.TrimNewLines(expected);
            Assert.AreEqual(expected, gitHubOutput.actionsYaml);
            Assert.AreEqual(true, gitHubOutput.v2ConversionSuccessful);
        }

        //This test doesn't work with V1
        [TestMethod]
        public void ComplexVariablesWithSimpleDependsOnJobTest()
        {
            //Arrange
            Conversion conversion = new Conversion();
            string yaml = @"
  jobs:
  - job: Build
    displayName: 'Build job'
    pool:
        vmImage: 'windows-latest'
    dependsOn: AnotherJob
    variables:
    - group: Active Login   # Contains codesigningCertPassword: Password for code signing cert
    - name: sourceArtifactName
      value: 'nuget-windows'
    - name: targetArtifactName
      value: 'nuget-windows-signed'
    - name: pathToNugetPackages
      value: '**/*.nupkg'

    steps:
    - task: CmdLine@2
      inputs:
        script: echo your commands here 
";

            //Act
            ConversionResponse gitHubOutput = conversion.ConvertAzurePipelineToGitHubAction(yaml);

            //Assert
            string expected = @"
jobs:
  Build:
    name: Build job
    runs-on: windows-latest
    needs:
    - AnotherJob
    env:
      group: Active Login
      sourceArtifactName: nuget-windows
      targetArtifactName: nuget-windows-signed
      pathToNugetPackages: '**/*.nupkg'
    steps:
    - uses: actions/checkout@v2
    - run: echo your commands here
      shell: cmd
";
            expected = UtilityTests.TrimNewLines(expected);
            Assert.AreEqual(expected, gitHubOutput.actionsYaml);
            Assert.AreEqual(true, gitHubOutput.v2ConversionSuccessful);
        }



       [TestMethod]
        public void SimpleVariablesWithSimpleDependsOnJobTest()
        {
            //Arrange
            Conversion conversion = new Conversion();
            string yaml = @"
jobs:
- job: Build
  displayName: 'Build job'
  pool:
    vmImage: windows-latest
  dependsOn: AnotherJob
  variables:
    Variable1: 'new variable'
  steps:
  - task: CmdLine@2
    inputs:
      script: echo your commands here $(Variable1)
";

            //Act
            ConversionResponse gitHubOutput = conversion.ConvertAzurePipelineToGitHubAction(yaml);

            //Assert
            string expected = @"
jobs:
  Build:
    name: Build job
    runs-on: windows-latest
    needs:
    - AnotherJob
    env:
      Variable1: new variable
    steps:
    - uses: actions/checkout@v2
    - run: echo your commands here ${{ env.Variable1 }}
      shell: cmd
";

            expected = UtilityTests.TrimNewLines(expected);
            Assert.AreEqual(expected, gitHubOutput.actionsYaml);
            Assert.AreEqual(true, gitHubOutput.v2ConversionSuccessful);
        }



       [TestMethod]
        public void SimpleVariablesWithComplexDependsOnJobTest()
        {
            //Arrange
            Conversion conversion = new Conversion();
            string yaml = @"
jobs:
- job: Build
  displayName: 'Build job'
  pool:
    vmImage: windows-latest
  dependsOn: 
  - AnotherJob
  variables:
    Variable1: 'new variable'
  steps:
  - task: CmdLine@2
    inputs:
      script: echo your commands here $(Variable1)
";

            //Act
            ConversionResponse gitHubOutput = conversion.ConvertAzurePipelineToGitHubAction(yaml);

            //Assert
            string expected = @"
jobs:
  Build:
    name: Build job
    runs-on: windows-latest
    needs:
    - AnotherJob
    env:
      Variable1: new variable
    steps:
    - uses: actions/checkout@v2
    - run: echo your commands here ${{ env.Variable1 }}
      shell: cmd
";

            expected = UtilityTests.TrimNewLines(expected);
            Assert.AreEqual(expected, gitHubOutput.actionsYaml);
            Assert.AreEqual(true, gitHubOutput.v2ConversionSuccessful);
        }

    }
}