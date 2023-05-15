﻿// Copyright © 2023-Present Chocolatey Software, Inc
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.tests.infrastructure.app.commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commandline;
    using Moq;
    using Should;

    public static class ChocolateyListCommandSpecs
    {
        [ConcernFor("list")]
        public abstract class ChocolateyListCommandSpecsBase : TinySpec
        {
            protected ChocolateyListCommand Command;
            protected Mock<IChocolateyPackageService> PackageService = new Mock<IChocolateyPackageService>();
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                Command = new ChocolateyListCommand(PackageService.Object);
            }
        }

        public class When_implementing_command_for : ChocolateyListCommandSpecsBase
        {
            private List<string> _results;

            public override void Because()
            {
                _results = Command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_list()
            {
                _results.ShouldContain("list");
            }

            [Fact]
            public void Should_not_implement_search()
            {
                _results.ShouldNotContain("search");
            }

            [Fact]
            public void Should_not_implement_find()
            {
                _results.ShouldNotContain("find");
            }

            public class When_configurating_the_argument_parser : ChocolateyListCommandSpecsBase
            {
                private OptionSet _optionSet;

                public override void Context()
                {
                    base.Context();
                    _optionSet = new OptionSet();
                }

                public override void Because()
                {
                    Command.ConfigureArgumentParser(_optionSet, Configuration);
                }

                [NUnit.Framework.TestCase("source")]
                [NUnit.Framework.TestCase("s")]
                [NUnit.Framework.TestCase("prerelease")]
                [NUnit.Framework.TestCase("pre")]
                [NUnit.Framework.TestCase("includeprograms")]
                [NUnit.Framework.TestCase("i")]
                public void Should_add_to_option_set(string option)
                {
                    _optionSet.Contains(option).ShouldBeTrue();
                }

                [NUnit.Framework.TestCase("localonly")]
                [NUnit.Framework.TestCase("l")]
                [NUnit.Framework.TestCase("user")]
                [NUnit.Framework.TestCase("u")]
                [NUnit.Framework.TestCase("password")]
                [NUnit.Framework.TestCase("p")]
                [NUnit.Framework.TestCase("allversions")]
                [NUnit.Framework.TestCase("a")]
                public void Should_not_add_to_option_set(string option)
                {
                    _optionSet.Contains(option).ShouldBeFalse();
                }
            }

            public class When_handling_additional_argument_parsing : ChocolateyListCommandSpecsBase
            {
                private readonly IList<string> _unparsedArgs = new List<string>();
                private readonly string _source = "https://somewhereoutthere";
                private Action _because;

                public override void Context()
                {
                    base.Context();
                    _unparsedArgs.Add("pkg1");
                    _unparsedArgs.Add("pkg2");
                    _unparsedArgs.Add("-l");
                    _unparsedArgs.Add("--local-only");
                    _unparsedArgs.Add("--localonly");
                    _unparsedArgs.Add("-li");
                    _unparsedArgs.Add("-lai");
                    Configuration.Sources = _source;
                }

                public override void Because()
                {
                    _because = () => Command.ParseAdditionalArguments(_unparsedArgs, Configuration);
                }

                [Fact]
                public void Should_set_unparsed_arguments_to_configuration_input()
                {
                    _because();
                    Configuration.Input.ShouldEqual("pkg1 pkg2");
                }

                [NUnit.Framework.TestCase("-l")]
                [NUnit.Framework.TestCase("--local-only")]
                [NUnit.Framework.TestCase("--localonly")]
                public void Should_output_warning_message_about_unsupported_argument(string argument)
                {
                    _because();
                    MockLogger.Messages.Keys.ShouldContain("Warn");
                    MockLogger.Messages["Warn"].ShouldContain(@"
UNSUPPORTED ARGUMENT: Ignoring the argument {0}. This argument is unsupported for locally installed packages, and will be treated as a package name in Chocolatey CLI v3!".FormatWith(argument));
                }

                [NUnit.Framework.TestCase("-li")]
                [NUnit.Framework.TestCase("-lai")]
                public void Should_output_warning_message_about_unsupported_argument_and_set_include_programs(string argument)
                {
                    _because();
                    MockLogger.Messages.Keys.ShouldContain("Warn");
                    MockLogger.Messages["Warn"].ShouldContain(@"
UNSUPPORTED ARGUMENT: Ignoring the argument {0}. This argument is unsupported for locally installed packages, and will be treated as a package name in Chocolatey CLI v3!".FormatWith(argument));
                    Configuration.ListCommand.IncludeRegistryPrograms.ShouldBeTrue();
                }
            }

            public class When_noop_is_called_with_list_command : ChocolateyListCommandSpecsBase
            {
                public override void Context()
                {
                    base.Context();
                    Configuration.CommandName = "search";
                    Configuration.ListCommand.LocalOnly = false;
                }

                public override void Because()
                {
                    Command.DryRun(Configuration);
                }

                [Fact]
                public void Should_call_service_list_noop()
                {
                    PackageService.Verify(c => c.ListDryRun(Configuration), Times.Once);
                }

                [Fact]
                public void Should_not_report_any_warning_messages()
                {
                    MockLogger.Messages.Keys.ShouldNotContain("Warn");
                }
            }

            public class When_run_is_called_with_search_command_and_local_only : ChocolateyListCommandSpecsBase
            {
                public override void Context()
                {
                    base.Context();
                    Configuration.CommandName = "list";
                }

                public override void Because()
                {
                    Command.Run(Configuration);
                }

                [Fact]
                public void Should_call_service_list_run()
                {
                    PackageService.Verify(c => c.List(Configuration), Times.Once);
                }

                [Fact]
                public void Should_not_report_any_warning_messages()
                {
                    MockLogger.Messages.Keys.ShouldNotContain("Warn");
                }
            }
        }
    }
}
