using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using FluentFTP;

namespace FTPSync
{
    class Program
    {
        private static FTP ftp;
        private static FtpClient ftpCl;
        private static Uri filePath;
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
           
            string directory = Properties.Settings.Default.localPath;
            string remotePath = Properties.Settings.Default.remotePath;
            string host = Properties.Settings.Default.host;
            string port = Properties.Settings.Default.port;
            string username = Properties.Settings.Default.username;
            string password = Properties.Settings.Default.password;

            ThreadPool.SetMaxThreads(10, 10);

            ftp = new FTP(host, username, password, port, remotePath);
            ftpCl = new FtpClient(host, int.Parse(port), username, password);
            filePath = new Uri(directory);
            ThreadPool.QueueUserWorkItem((state) => {
                FileSystemWatcher fsw = new FileSystemWatcher(directory);
                fsw.Filter = "";
                fsw.IncludeSubdirectories = true;
                fsw.EnableRaisingEvents = true;
                fsw.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName;
                fsw.Changed += FileChanged;
                fsw.Created += FileCreated;
                fsw.Deleted += FileDeleted;
                fsw.Renamed += FileRenamed;
            });

            Application.Run(new TaskTray(Properties.Resources.Circle_icons_upload));
        }

        private static void FileRenamed(object sender, RenamedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                string newName = Path.GetFileName(e.Name);
                ftp.Rename(e.OldName, newName);
            });
        }

        private static void FileDeleted(object sender, FileSystemEventArgs e)
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    if (ftpCl.DirectoryExists(e.Name))
                    {
                        ftpCl.DeleteDirectory(e.Name);
                    }
                    else
                    {
                        ftp.Delete(e.Name);
                    }
                }
                catch (FtpCommandException ex)
                {

                }
            });
           
        }

        private static void FileCreated(object sender, FileSystemEventArgs e)
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                ftp.Upload(e.FullPath, e.Name);
            });
        }

        private static void FileChanged(object sender, FileSystemEventArgs e)
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                ftp.Upload(e.FullPath, e.Name);
            });
        }
    }
}
