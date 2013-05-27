// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Http.cs" company="NBusy Project">
//   Copyright (c) 2010 - 2011 Teoman Soygul. Licensed under LGPLv3 (http://www.gnu.org/licenses/lgpl.html).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.IO;
using System.Net;

using NBug.Core.Util.Logging;

namespace NBug.Core.Submission.Web
{
	internal class Http : Protocol
	{
        internal Http(string connectionString, Stream reportFile)
			: base(connectionString, reportFile, Protocols.HTTP)
		{
		}

		internal Http(string connectionString)
			: base(connectionString, Protocols.HTTP)
		{
		}

		internal Http()
			: base(Protocols.HTTP)
		{
		}

		// Connection string format (single line)
		// Warning: There should be no semicolon (;) or equals sign (=) used in any field.
		// Note: Url should be a full url with a trailing slash (/) or file extension (i.e. .php), like: http://....../ -or- http://....../upload.php

		/* Type=HTTP;
		 * Url=http://tracker.mydomain.com/myproject/upload.php;
		 */

		public string Url { get; set; }

	    public override bool Send()
		{
            Logger.Trace("Submitting bug report to via HTTP connection.");
            // Advanced method with ability to post variables along with file (do not forget to urlencode the query parameters)
			// http://www.codeproject.com/KB/cs/uploadfileex.aspx
			// http://stackoverflow.com/questions/566462/upload-files-with-httpwebrequest-multipart-form-data
			// http://stackoverflow.com/questions/767790/how-do-i-upload-an-image-file-using-a-post-request-in-c
			// http://netomatix.com/HttpPostData.aspx

			/* upload.php file my look like the one below (note that uploaded files are not statically named in this case script may need modification)
			 * 
			 * <?php
			 * $uploadDir = 'Upload/'; 
			 * $uploadFile = $uploadDir . basename($_FILES['file']['name']);
			 * if (is_uploaded_file($_FILES['file']['tmp_name'])) 
			 * {
			 *     echo "File ". $_FILES['file']['name'] ." is successfully uploaded!\r\n";
			 *     if (move_uploaded_file($_FILES['file']['tmp_name'], $uploadFile)) 
			 *     {
			 *         echo "File is successfully stored! ";
			 *     }
			 *     else print_r($_FILES);
			 * }
			 * else 
			 * {
			 *     echo "Upload Failed!";
			 *     print_r($_FILES);
			 * }
			 * ?>
			 */

            ReportFile.Position = 0;
            var files = new[]
		        {
		            new UploadFile {Name = "file", Filename = "test.zip", Stream = ReportFile}
		        };
		    var response = UploadFiles(Url, files, new NameValueCollection());
            // TODO: parse response
            Logger.Info("Response from HTTP server: " + Encoding.ASCII.GetString(response));
            ReportFile.Position = 0;


			return true;
		}

        protected byte[] UploadFiles(string address, IEnumerable<UploadFile> files, NameValueCollection values)
        {
            var request = WebRequest.Create(address);
            request.Method = "POST";
            var boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x", NumberFormatInfo.InvariantInfo);
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            boundary = "--" + boundary;

            using (var requestStream = request.GetRequestStream())
            {
                // Write the values
                foreach (string name in values.Keys)
                {
                    var buffer = Encoding.ASCII.GetBytes(boundary + Environment.NewLine);
                    requestStream.Write(buffer, 0, buffer.Length);
                    buffer = Encoding.ASCII.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\"{1}{1}", name, Environment.NewLine));
                    requestStream.Write(buffer, 0, buffer.Length);
                    buffer = Encoding.UTF8.GetBytes(values[name] + Environment.NewLine);
                    requestStream.Write(buffer, 0, buffer.Length);
                }

                // Write the files
                foreach (var file in files)
                {
                    var buffer = Encoding.ASCII.GetBytes(boundary + Environment.NewLine);
                    requestStream.Write(buffer, 0, buffer.Length);
                    buffer = Encoding.UTF8.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"{2}", file.Name, file.Filename, Environment.NewLine));
                    requestStream.Write(buffer, 0, buffer.Length);
                    buffer = Encoding.ASCII.GetBytes(string.Format("Content-Type: {0}{1}{1}", file.ContentType, Environment.NewLine));
                    requestStream.Write(buffer, 0, buffer.Length);
                    file.Stream.CopyTo(requestStream);
                    buffer = Encoding.ASCII.GetBytes(Environment.NewLine);
                    requestStream.Write(buffer, 0, buffer.Length);
                }

                var boundaryBuffer = Encoding.ASCII.GetBytes(boundary + "--");
                requestStream.Write(boundaryBuffer, 0, boundaryBuffer.Length);
            }

            using (var response = request.GetResponse())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    using (var stream = new MemoryStream())
                    {
                        responseStream.CopyTo(stream);
                        return stream.ToArray();
                    }
                }
            }
        }

        protected class UploadFile
        {
            public UploadFile()
            {
                ContentType = "application/octet-stream";
            }
            public string Name { get; set; }
            public string Filename { get; set; }
            public string ContentType { get; set; }
            public Stream Stream { get; set; }
        }
	}
}
