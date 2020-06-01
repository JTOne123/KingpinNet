﻿using KingpinNet.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KingpinNet
{
    public class KingpinApplication
    {
        private readonly IConsole console;
        private List<CommandItem> _commands = new List<CommandItem>();
        private List<IItem> _flags = new List<IItem>();
        private List<IItem> _arguments = new List<IItem>();

        public IEnumerable<CommandItem> Commands => _commands;
        public IEnumerable<IItem> Flags => _flags;
        public IEnumerable<IItem> Arguments => _arguments;

        private IHelpTemplate _applicationHelp;
        private IHelpTemplate _commandHelp;

        public string Name { get; internal set; }
        public string Help { get; internal set; }
        public string VersionString { get; private set; }
        public string AuthorName { get; private set; }
        public bool HelpShownOnParsingErrors { get; private set; }
        public bool ExitOnParseErrors { get; private set; }
        public bool ExitWhenHelpIsShown { get; internal set; }
        public KingpinApplication(IConsole console)
        {
            this.console = console;
        }
        public void Initialize()
        {
            _applicationHelp = new ApplicationHelp();
            _commandHelp = new CommandHelp();
            Flag("help", "Show context-sensitive help").Short('h').IsBool().Action(x => GenerateHelp());
            Flag<bool>("suggestion-script-bash").IsHidden().Action(x => GenerateScript("bash.sh"));
            Flag<bool>("suggestion-script-zsh").IsHidden().Action(x => GenerateScript("zsh.sh"));
            Flag<bool>("suggestion-script-pwsh").IsHidden().Action(x => GenerateScript("pwsh.ps1"));
        }

        private void GenerateScript(string resource)
        {
            var content = GetResource(resource);
            console.Out.Write(content.Replace("{{AppName}}", Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location)));
            Environment.Exit(0);
        }
        private string GetResource(string name)
        {
            var stream = this.GetType().Assembly.GetManifestResourceStream($"KingpinNet.Scripts.{name}");
            var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public void GenerateHelp()
        {
            var helpGenerator = new HelpGenerator(this);
            helpGenerator.Generate(console.Out, _applicationHelp);
            if (ExitWhenHelpIsShown)
            {
                Environment.Exit(0);
            }
        }
        private void GenerateCommandHelp(CommandItem command)
        {
            var helpGenerator = new HelpGenerator(this);
            helpGenerator.Generate(command, console.Out, _commandHelp);
        }

        public CommandItem Command(string name, string help = "")
        {
            var result = new CommandItem(name, name, help);
            _commands.Add(result);
            return result;
        }

        public FlagItem<string> Flag(string name, string help = "")
        {
            var result = new FlagItem<string>(name, name, help);
            _flags.Add(result);
            return result;
        }
        public ArgumentItem<string> Argument(string name, string help = "")
        {
            var result = new ArgumentItem<string>(name, name, help);
            _arguments.Add(result);
            return result;
        }

        public FlagItem<T> Flag<T>(string name, string help = "")
        {
            var result = new FlagItem<T>(name, name, help,
                ValueTypeConverter.Convert(typeof(T)));
            _flags.Add(result);
            return result;
        }
        public ArgumentItem<T> Argument<T>(string name, string help = "")
        {
            var result = new ArgumentItem<T>(name, name, help,
                ValueTypeConverter.Convert(typeof(T)));
            _arguments.Add(result);
            return result;
        }

        public KingpinApplication ExitOnParsingErrors()
        {
            ExitOnParseErrors = true;
            return this;
        }

        public KingpinApplication ExitOnHelp()
        {
            ExitWhenHelpIsShown = true;
            return this;
        }

        public KingpinApplication ShowHelpOnParsingErrors()
        {
            HelpShownOnParsingErrors = true;
            return this;
        }



        public void AddCommandHelpOnAllCommands()
        {
            AddCommandHelpOnAllCommands(_commands);
        }

        private void AddCommandHelpOnAllCommands(IEnumerable<CommandItem> commands)
        {
            foreach (var command in commands)
            {
                if (command.Commands.Count() > 0)
                    AddCommandHelpOnAllCommands(command.Commands);
                else
                    Flag<string>("help", "Show context-sensitive help").IsHidden().Short('h').IsBool().Action(x => GenerateCommandHelp(command));
            }
        }

        public ParseResult Parse(IEnumerable<string> args)
        {
            var parser = new Parser(this);
            AddCommandHelpOnAllCommands();
            try
            {
                var result = parser.Parse(args);
                InvestigateSuggestions(result);
                return result;
            }
            catch (ParseException exception)
            {
                console.WriteLine(exception.Message);
                foreach (var error in exception.Errors)
                    console.WriteLine($"   {error}");

                if (HelpShownOnParsingErrors)
                {
                    GenerateHelp();
                }
                if (ExitOnParseErrors)
                    Environment.Exit(-1);
                throw;
            }
        }

        private void InvestigateSuggestions(ParseResult result)
        {
            if (result.IsSuggestion)
            {
                foreach (var suggestion in result.Suggestions)
                    console.Out.WriteLine(suggestion);
                Environment.Exit(0);
            }
        }

        public KingpinApplication Author(string author)
        {
            AuthorName = author;
            return this;
        }

        public KingpinApplication Version(string version)
        {
            VersionString = version;
            return this;
        }

        public KingpinApplication ApplicationHelp(string text)
        {
            Help = text;
            return this;
        }

        public KingpinApplication ApplicationName(string name)
        {
            Name = name;
            return this;
        }

        public KingpinApplication Template(IHelpTemplate applicationHelp, IHelpTemplate commandHelp)
        {
            _applicationHelp = applicationHelp;
            _commandHelp = commandHelp;
            return this;
        }
    }
}
