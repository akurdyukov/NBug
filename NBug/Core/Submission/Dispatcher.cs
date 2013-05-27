// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Dispatcher.cs" company="NBusy Project">
//   Copyright (c) 2010 - 2011 Teoman Soygul. Licensed under LGPLv3 (http://www.gnu.org/licenses/lgpl.html).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;

using NBug.Core.Util.Logging;
using NBug.Core.Util.Storage;

namespace NBug.Core.Submission
{
	internal class Dispatcher
	{
		/// <summary>
		/// Initializes a new instance of the Dispatcher class to send queued reports.
		/// </summary>
		/// <param name="isAsynchronous">
		/// Decides whether to start the dispatching process asynchronously on a background thread.
		/// </param>
		internal Dispatcher(bool isAsynchronous)
		{
			// Test if it has NOT been more than x many days since entry assembly was last modified)
			// This is the exact verifier code in the BugReport.cs of CreateReportZip() function
			if (Settings.StopReportingAfter < 0 || File.GetLastWriteTime(Settings.EntryAssembly.Location).AddDays(Settings.StopReportingAfter).CompareTo(DateTime.Now) > 0)
			{
				if (isAsynchronous)
				{
					// Log and swallow NBug's internal exceptions by default
					Task.Factory.StartNew(this.Dispatch).ContinueWith(
						t => Logger.Error("An exception occurred while dispatching bug report. Check the inner exception for details", t.Exception),
						TaskContinuationOptions.OnlyOnFaulted);
				}
				else
				{
					try
					{
						this.Dispatch();
					}
					catch (Exception exception)
					{
						Logger.Error("An exception occurred while dispatching bug report. Check the inner exception for details.", exception);
					}
				}
			}
			else
			{
				// ToDo: Cleanout all the remaining report files and disable NBug completely
			}
		}

		private void Dispatch()
		{
			// Make sure that we are not interfering with the crucial startup work);
			if (!Settings.RemoveThreadSleep)
			{
				System.Threading.Thread.Sleep(Settings.SleepBeforeSend * 1000);
			}

			// Turncate extra report files and try to send the first one in the queue
			Storer.TruncateReportFiles();

			// Now go through configured destinations and submit to all automatically
			for (bool hasReport = true; hasReport;)
			{
				using (Storer storer = new Storer())
				using (Stream stream = storer.GetFirstReportFile())
				{
					if (stream != null)
					{
						if (this.EnumerateDestinations(stream) == false)
						{
							break;
						}

						// Delete the file after it was sent
						storer.DeleteCurrentReportFile();
					}
					else
					{
						hasReport = false;
					}
				}
			}
		}

		/// <summary>
		/// Enumerate all protocols to see if they are properly configured and send using the ones that are configured 
		/// as many times as necessary.
		/// </summary>
		/// <param name="reportFile">The file to read the report from.</param>
		/// <returns>Returns <see langword="true"/> if the sending was successful. 
		/// Returns <see langword="true"/> if the report was submitted to at least one destination.</returns>
		private bool EnumerateDestinations(Stream reportFile)
		{
			bool sentSuccessfullyAtLeastOnce = false;
		    var destinations = new[]
		        {
		            Settings.Destination1, Settings.Destination2, Settings.Destination3, Settings.Destination4,
		            Settings.Destination5
		        };

            foreach (var destination in destinations)
		    {
                if (!string.IsNullOrEmpty(destination))
                {
                    if (EnumerateSubmitters(reportFile, GetDestinationType(destination), destination))
                    {
                        sentSuccessfullyAtLeastOnce = true;
                    }
                }
		    }

			return sentSuccessfullyAtLeastOnce;
		}

		private string GetDestinationType(string destination)
		{
			return Protocol.Parse(destination)["Type"];
		}

		// Individual submitter components should fail silently (mainly due to misconfiguration or outdated configuration)
		private bool EnumerateSubmitters(Stream reportFile, string destination, string connectionString)
		{
		    Protocol protocol = ProtocolFactory.CreateProtocol(destination, connectionString, reportFile);
            if (protocol == null)
            {
                Logger.Error("Cannot find handler for protocol " + destination);
                return false;
            }
            try
            {
                return protocol.Send();
            }
            catch (Exception exception)
            {
                Logger.Error("An exception occurred while submitting bug report. Check the inner exception for details.", exception);
                return false;
            }
		}
	}
}
