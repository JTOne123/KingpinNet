﻿using KingpinNet.Help;
using System;
using System.IO;

namespace KingpinNet
{
    public class HelpGenerator
    {
        private readonly KingpinApplication _application;
        private static readonly string Nl = Environment.NewLine;

        public HelpGenerator(KingpinApplication application)
        {
            _application = application;
        }

        public void Generate(TextWriter output, IHelpTemplate template = default(ApplicationHelp))
        {
            if (template == null)
                template = new ApplicationHelp();
            template.Application = _application;
            output.Write(template.TransformText().Replace("\r\n", $"{Nl}"));
        }

        public void Generate(CommandItem command, TextWriter output, IHelpTemplate template = default(CommandHelp))
        {
            if (template == null)
                template = new CommandHelp();
            template.Application = _application;
            template.Command = command;
            output.WriteLine(template.TransformText().Replace("\r\n", $"{Nl}"));
        }

    }
}
