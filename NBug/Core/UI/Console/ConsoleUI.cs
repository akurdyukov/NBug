﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConsoleUI.cs" company="NBusy Project">
//   Copyright (c) 2010 - 2011 Teoman Soygul. Licensed under LGPLv3 (http://www.gnu.org/licenses/lgpl.html).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

using NBug.Core.Reporting.Info;
using NBug.Core.Util.Exceptions;
using NBug.Core.Util.Serialization;
using NBug.Enums;

namespace NBug.Core.UI.Console
{
	internal static class ConsoleUI
	{
		internal static UIDialogResult ShowDialog(UIMode uiMode, SerializableException exception, Report report)
		{
		    if (uiMode == UIMode.Minimal)
			{
				// Do not interact with the user
				System.Console.WriteLine(Environment.NewLine + Settings.Resources.UI_Console_Minimal_Message);
				return new UIDialogResult(ExecutionFlow.BreakExecution, SendReport.Send);
			}
		    if (uiMode == UIMode.Normal)
		    {
		        // ToDo: Create normal console UI
		        System.Console.WriteLine(Environment.NewLine + Settings.Resources.UI_Console_Normal_Message);
		        return new UIDialogResult(ExecutionFlow.BreakExecution, SendReport.Send);
		    }
		    if (uiMode == UIMode.Full)
		    {
		        // ToDo: Create full console UI
		        System.Console.WriteLine(Environment.NewLine + Settings.Resources.UI_Console_Full_Message);
		        return new UIDialogResult(ExecutionFlow.BreakExecution, SendReport.Send);
		    }

		    throw NBugConfigurationException.Create(() => Settings.UIMode, "Parameter supplied for settings property is invalid.");
		}
	}
}
