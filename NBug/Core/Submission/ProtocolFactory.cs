using System;
using System.Collections.Generic;
using System.IO;
using NBug.Core.Submission.Tracker;
using NBug.Core.Submission.Web;

namespace NBug.Core.Submission
{
    /// <summary>
    /// Factory for Protocol instances
    /// </summary>
    public static class ProtocolFactory
    {
        private static readonly Dictionary<string, Func<string, Stream, Protocol>> Creators = new Dictionary<string, Func<string, Stream, Protocol>>(); 

        static ProtocolFactory()
        {
            // register default handlers

            Register("http", (s, stream) => new Http(s, stream));
            Register("ftp", (s, stream) => new Ftp(s, stream));
            Register("redmine", (s, stream) => new Redmine(s, stream));

            Func<string, Stream, Protocol> creator = (conn, stream) => new Mail(conn, stream);

            Register("mail", creator);
            Register("email", creator);
            Register("e-mail", creator);
        }

        public static void Register(string destination, Func<string, Stream, Protocol> creator)
        {
            Creators[destination.ToLower()] = creator;
        }

        public static Protocol CreateProtocol(string destination, string connectionString, Stream reportFile)
        {
            if (Creators.ContainsKey(destination.ToLower()))
            {
                return Creators[destination.ToLower()](connectionString, reportFile);
            }
            return null;
        }
    }
}
