﻿#region Copyright (c) OPTANO GmbH

// ////////////////////////////////////////////////////////////////////////////////
// 
//        OPTANO GmbH Source Code
//        Copyright (c) 2010-2020 OPTANO GmbH
//        ALL RIGHTS RESERVED.
// 
//    The entire contents of this file is protected by German and
//    International Copyright Laws. Unauthorized reproduction,
//    reverse-engineering, and distribution of all or any portion of
//    the code contained in this file is strictly prohibited and may
//    result in severe civil and criminal penalties and will be
//    prosecuted to the maximum extent possible under the law.
// 
//    RESTRICTIONS
// 
//    THIS SOURCE CODE AND ALL RESULTING INTERMEDIATE FILES
//    ARE CONFIDENTIAL AND PROPRIETARY TRADE SECRETS OF
//    OPTANO GMBH.
// 
//    THE SOURCE CODE CONTAINED WITHIN THIS FILE AND ALL RELATED
//    FILES OR ANY PORTION OF ITS CONTENTS SHALL AT NO TIME BE
//    COPIED, TRANSFERRED, SOLD, DISTRIBUTED, OR OTHERWISE MADE
//    AVAILABLE TO OTHER INDIVIDUALS WITHOUT WRITTEN CONSENT
//    AND PERMISSION FROM OPTANO GMBH.
// 
// ////////////////////////////////////////////////////////////////////////////////

#endregion

namespace Optano.Algorithm.Tuner.Lingeling.Tests
{
    using System;
    using System.IO;
    using System.Linq;

    using NDesk.Options;

    using Shouldly;

    using Xunit;

    /// <summary>
    /// Containts tests for the <see cref="LingelingRunnerConfiguration"/> class and the <see cref="LingelingRunnerConfigurationParser"/> class.
    /// </summary>
    [Collection("NonParallel")]
    public class LingelingRunnerConfigurationTests : IDisposable
    {
        #region Fields

        /// <summary>
        /// The <see cref="LingelingRunnerConfigurationParser"/> used in tests.
        /// </summary>
        private LingelingRunnerConfigurationParser _parser;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LingelingRunnerConfigurationTests"/> class.
        /// Behaves like the [TestInitialize] of MSTest framework.
        /// </summary>
        public LingelingRunnerConfigurationTests()
        {
            this._parser = new LingelingRunnerConfigurationParser();
        }

        #endregion

        #region Public Methods and Operators

        /// <inheritdoc/>
        public void Dispose()
        {
            this._parser = null;
        }

        /// <summary>
        /// Checks that trying to use the builder throws an
        /// <see cref="InvalidOperationException"/> if <see cref="LingelingRunnerConfigurationParser.ParseArguments"/>
        /// was not called beforehand.
        /// </summary>
        [Fact]
        public void ConfigurationParserThrowsIfNoPreprocessingWasDone()
        {
            Exception exception =
                Assert.Throws<InvalidOperationException>(
                    () =>
                        {
                            var builder = this._parser.ConfigurationBuilder;
                        });
        }

        /// <summary>
        /// Checks that all possible arguments to an instance acting as master get parsed correctly.
        /// </summary>
        [Fact]
        public void MasterArgumentsAreParsedCorrectly()
        {
            const string Executable = "dummy exe";
            const GenericParameterization GenericParameterization = GenericParameterization.RandomForestAverageRank;
            const int Factor = 12;
            const int Seed = 22;
            const int NumberOfSeeds = 5;
            const int MemoryLimit = 2000;
            var args = new[]
                           {
                               "--master", $"--executable={Executable}", $"--genericParameterization={GenericParameterization}",
                               $"--factorParK={Factor}", $"--rngSeed={Seed}", $"--numberOfSeeds={NumberOfSeeds}",
                               $"--memoryLimitMegabyte={MemoryLimit}",
                           };

            this._parser.ParseArguments(args);

            var config = this._parser.ConfigurationBuilder.Build();

            config.IsMaster.ShouldBeTrue("Expected master to be requested.");
            config.PathToExecutable.ShouldBe(Executable, "Expected different path to executable.");
            config.GenericParameterization.ShouldBe(GenericParameterization, "Expected different generic parameterization.");
            config.FactorParK.ShouldBe(Factor, "Expected different factor.");
            config.RngSeed.ShouldBe(Seed, "Expected different random number generator seed.");
            config.NumberOfSeeds.ShouldBe(NumberOfSeeds, "Expected different number of seeds.");
            config.MemoryLimitMegabyte.ShouldBe(MemoryLimit, "Expected different memory limit.");
        }

        /// <summary>
        /// Checks that all possible arguments to an instance acting as worker get parsed correctly.
        /// </summary>
        [Fact]
        public void NonMasterArgumentsAreParsedCorrectly()
        {
            const string RemainingArg = "test";
            var args = new[] { RemainingArg };
            this._parser.ParseArguments(args);

            var config = this._parser.ConfigurationBuilder.Build();

            config.IsMaster.ShouldBeFalse("Did not expect master to be requested.");
            this._parser.RemainingArguments.Count().ShouldBe(1, "Expected one remaining argument.");
            this._parser.RemainingArguments.First().ShouldBe(RemainingArg, "Expected different remaining argument.");
        }

        /// <summary>
        /// Checks that <see cref="NDesk.Options" /> parses all possible generic parameterizations correctly.
        /// </summary>
        /// <param name="genericParameterization">The generic parameterization.</param>
        /// <param name="genericParameterizationString">The generic parameterization as string.</param>
        [Theory]
        [InlineData(GenericParameterization.RandomForestReuseOldTrees, "RandomForestReuseOldTrees")]
        [InlineData(GenericParameterization.RandomForestAverageRank, "RandomForestAverageRank")]
        [InlineData(GenericParameterization.StandardRandomForest, "StandardRandomForest")]
        [InlineData(GenericParameterization.Default, "Default")]
        [InlineData(GenericParameterization.RandomForestReuseOldTrees, "randomforestreuseoldtrees")]
        [InlineData(GenericParameterization.RandomForestAverageRank, "randomforestaveragerank")]
        [InlineData(GenericParameterization.StandardRandomForest, "standardrandomforest")]
        [InlineData(GenericParameterization.Default, "default")]
        public void NDeskParsesGenericParameterizationsCorrectly(
            GenericParameterization genericParameterization,
            string genericParameterizationString)
        {
            const string Executable = "dummy exe";

            var args = new[] { "--master", $"--executable={Executable}", $"--genericParameterization={genericParameterizationString}" };

            this._parser.ParseArguments(args);

            var config = this._parser.ConfigurationBuilder.Build();

            config.IsMaster.ShouldBeTrue("Expected master to be requested.");
            config.PathToExecutable.ShouldBe(Executable, "Expected different path to executable.");
            config.GenericParameterization.ShouldBe(genericParameterization, "Expected different generic parameterization");
        }

        /// <summary>
        /// Checks that <see cref="NDesk.Options" /> throws an <see cref="OptionException" />, if unknown generic parameterizations are used.
        /// </summary>
        /// <param name="genericParameterizationString">The generic parameterization as string.</param>
        [Theory]
        [InlineData("ReuseOldTrees")]
        [InlineData("AverageRank")]
        [InlineData("Standard")]
        public void NDeskThrowsIfUnknownGenericParameterizationIsUsed(string genericParameterizationString)
        {
            const string Executable = "dummy exe";

            var args = new[] { "--master", $"--executable={Executable}", $"--genericParameterization={genericParameterizationString}" };

            Exception exception =
                Assert.Throws<OptionException>(
                    () => this._parser.ParseArguments(args));
        }

        /// <summary>
        /// Checks that providing the --master argument, but no argument defining the path to the executable
        /// results in an <see cref="OptionException"/> when calling
        /// <see cref="LingelingRunnerConfigurationParser.ParseArguments"/>.
        /// </summary>
        [Fact]
        public void MissingPathToExecutableThrowsException()
        {
            var args = new[] { "--master" };
            Exception exception =
                Assert.Throws<OptionException>(
                    () => this._parser.ParseArguments(args));
        }

        /// <summary>
        /// Checks that the parser throws an <see cref="ArgumentOutOfRangeException" />, if the factorParK is negative or zero.
        /// </summary>
        /// <param name="factor">The factor.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ParserThrowsIfFactorParKIsNegativeOrZero(int factor)
        {
            const string Executable = "dummy exe";
            var args = new[] { "--master", $"--executable={Executable}", $"--factorParK={factor}" };
            Exception exception =
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => this._parser.ParseArguments(args));
        }

        /// <summary>
        /// Checks that the parser throws an <see cref="ArgumentOutOfRangeException" />, if the numberOfSeeds is negative or zero.
        /// </summary>
        /// <param name="numberOfSeeds">The number of seeds.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ParserThrowsIfNumberOfSeedsIsNegativeOrZero(int numberOfSeeds)
        {
            const string Executable = "dummy exe";
            var args = new[] { "--master", $"--executable={Executable}", $"--numberOfSeeds={numberOfSeeds}" };
            Exception exception =
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => this._parser.ParseArguments(args));
        }

        /// <summary>
        /// Checks that the parser throws an <see cref="ArgumentOutOfRangeException" />, if the memoryLimit is negative or zero.
        /// </summary>
        /// <param name="memoryLimit">The memory limit.</param>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ParserThrowsIfMemoryLimitIsNegativeOrZero(int memoryLimit)
        {
            const string Executable = "dummy exe";
            var args = new[] { "--master", $"--executable={Executable}", $"--memoryLimitMegabyte={memoryLimit}" };
            Exception exception =
                Assert.Throws<ArgumentOutOfRangeException>(
                    () => this._parser.ParseArguments(args));
        }

        /// <summary>
        /// Checks that <see cref="LingelingRunnerConfiguration.LingelingConfigBuilder.BuildWithFallback"/> method prioritizes the fallback arguments correctly.
        /// </summary>
        [Fact]
        public void BuildWithFallBackPrioritizesFallbackArgumentsCorrectly()
        {
            const int NumberOfSeedsFallback = 10;
            const int RngSeedFallback = 12;

            var fallback = new LingelingRunnerConfiguration.LingelingConfigBuilder()
                .SetNumberOfSeeds(NumberOfSeedsFallback)
                .SetRngSeed(RngSeedFallback)
                .Build();

            var config = new LingelingRunnerConfiguration.LingelingConfigBuilder()
                .SetRngSeed(LingelingRunnerConfiguration.LingelingConfigBuilder.RngSeedDefault)
                .BuildWithFallback(fallback);

            config.RngSeed.ShouldBe(LingelingRunnerConfiguration.LingelingConfigBuilder.RngSeedDefault);
            config.NumberOfSeeds.ShouldBe(NumberOfSeedsFallback);
        }

        /// <summary>
        /// Checks that <see cref="LingelingRunnerConfigurationParser.PrintHelp"/> prints help about general worker arguments, general
        /// master arguments, and custom Lingeling arguments.
        /// </summary>
        [Fact]
        public void PrintHelpPrintsAllArguments()
        {
            TestUtils.CheckOutput(
                action: () => this._parser.PrintHelp(),
                check: consoleOutput =>
                    {
                        var reader = new StringReader(consoleOutput.ToString());
                        var text = reader.ReadToEnd();
                        text.ShouldContain("Arguments for the application:", "Application arguments are missing.");
                        text.ShouldContain(
                            "Additional required arguments if this instance acts as master",
                            "Application master arguments are missing.");
                        text.ShouldContain("Arguments for master:", "General master arguments are missing.");
                        text.ShouldContain("Arguments for worker:", "General worker arguments are missing.");
                    });
        }

        #endregion
    }
}