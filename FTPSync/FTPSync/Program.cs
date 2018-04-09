using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace FTPSync
{
    class Program
    {
        private static FTP ftp;
        private static Uri filePath;
        static void Main(string[] args)
        {
            string directory = Properties.Settings.Default.localPath;
            string remotePath = Properties.Settings.Default.remotePath;
            string host = Properties.Settings.Default.host;
            string port = Properties.Settings.Default.port;
            string username = Properties.Settings.Default.username;
            string password = Properties.Settings.Default.password;
            ftp = new FTP(host, username, password, port, remotePath);
            filePath = new Uri(directory);
            Thread taskThd = new Thread(() => {
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

            taskThd.Start();
            while (true) ;
        }

        private static void FileRenamed(object sender, RenamedEventArgs e)
        {
            string newName = Path.GetFileName(e.Name);
            ftp.Rename(e.OldName, newName);
        }

        private static void FileDeleted(object sender, FileSystemEventArgs e)
        {
            ftp.Delete(e.Name);
        }

        private static void FileCreated(object sender, FileSystemEventArgs e)
        {
            ftp.Upload(e.FullPath, e.Name);
        }

        private static void FileChanged(object sender, FileSystemEventArgs e)
        {
            ftp.Upload(e.FullPath, e.Name);
        }
    }
}
