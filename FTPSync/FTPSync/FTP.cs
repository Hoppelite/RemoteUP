using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FTPSync
{
    /// <summary>
    /// Controls basic FTP commands
    /// </summary>
    public class FTP
    {
        private string host;
        private string username;
        private string password;
        private string port;
        private string remotePath;

        /// <summary>
        /// Create FTP object with default parameters
        /// </summary>
        /// <param name="host"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="port"></param>
        /// <param name="remotePath"></param>
        public FTP(string host = "localhost", string username = "anonymous", string password = "anonymous", string port = "21", string remotePath = "/")
        {
            this.host = host;
            this.username = username;
            this.password = password;
            this.port = port;
            this.remotePath = remotePath;
        }

        /// <summary>
        /// Make a request given a method and a file path
        /// </summary>
        /// <param name="file">The relative file path</param>
        /// <param name="method">The ftp method i.e. what to do - <see cref="WebRequestMethods.Ftp"/></param>
        /// <param name="requestCallback"></param>
        /// <param name="responseCallback"></param>
        /// <returns></returns>
        private bool MakeFTPRequest(string file, string method, Action<FtpWebRequest> requestCallback = null, Action<FtpWebResponse> responseCallback  = null)
        {
            Uri uri = new Uri($"ftp://{host}:{port}/{file}");
            // Get the object used to communicate with the server.
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = method;
            request.Credentials = new NetworkCredential(username, password);

            requestCallback?.Invoke(request);

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            responseCallback?.Invoke(response);

            response.Close();
            return true;
        }

        /// <summary>
        /// Delete a directory from the server
        /// </summary>
        /// <param name="file">The relative file path</param>
        public void DeleteDirectory(string file)
        {
            try
            {
                MakeFTPRequest(file, WebRequestMethods.Ftp.RemoveDirectory);
            }
            catch
            {

            }
        }

        /// <summary>
        /// Delete a file from the server
        /// </summary>
        /// <param name="file">The relative file path</param>
        public void Delete(string file)
        {
            try
            {
                MakeFTPRequest(file, WebRequestMethods.Ftp.DeleteFile);
            }
            catch
            {
                try
                {
                    MakeFTPRequest(file, WebRequestMethods.Ftp.RemoveDirectory);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        /// <summary>
        /// renames a file on the server
        /// </summary>
        /// <param name="file">Relative file path</param>
        /// <param name="newName">New name to rename to</param>
        public void Rename(string file, string newName)
        {
            try
            {
                MakeFTPRequest(file, WebRequestMethods.Ftp.Rename, req => req.RenameTo = newName);
            } catch (Exception ex)
            {
                throw ex;
            }
            
        }

        /// <summary>
        /// Upload a file to the server
        /// </summary>
        /// <param name="fileLocal">Full local path</param>
        /// <param name="fileServer">Relative server path</param>
        public void Upload(string fileLocal, string fileServer)
        {
            Thread.Sleep(200);
            try
            {
                if (Directory.Exists(fileLocal))
                {
                    MakeFTPRequest(fileServer, WebRequestMethods.Ftp.MakeDirectory);
                } else
                {
                    MakeFTPRequest(fileServer, WebRequestMethods.Ftp.UploadFile, req =>
                    {
                        using (FileStream fs = File.OpenRead(fileLocal))
                        {
                            byte[] buffer = new byte[fs.Length];
                            fs.Read(buffer, 0, buffer.Length);
                            fs.Close();
                            Stream requestStream = req.GetRequestStream();
                            requestStream.Write(buffer, 0, buffer.Length);
                            requestStream.Flush();
                            requestStream.Close();
                        }
                    });
                }
                
            } catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
