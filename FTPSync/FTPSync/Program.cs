using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using FluentFTP;
using System.Text.RegularExpressions;

namespace FTPSync
{
    class Program
    {
        private static FTP ftp;
        private static FtpClient ftpCl;
        private static Uri filePath;
        private static TaskTray tt;
        private static string[] fileFilters;

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
            tt = new TaskTray(Properties.Resources.Circle_icons_upload);
            Application.Run(tt);
        }

        private static bool IsMonitored(string fileName)
        {
            if (fileFilters.Count() == 0) return true;
            return fileFilters.Any(x => Regex.IsMatch(x, fileName));
        }

        private static void FileRenamed(object sender, RenamedEventArgs e)
        {
            if (!IsMonitored(e.FullPath)) return;
            ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    string newName = Path.GetFileName(e.Name);
                    ftp.Rename(e.OldName, newName);
                } catch (Exception ex)
                {
                    tt.ShowAlert($"Failed to rename file on server: {ex.Message}", ToolTipIcon.Error, $"Rename File: {e.OldName} - {e.Name}", 2000);
                }
            });
        }

        private static void FileDeleted(object sender, FileSystemEventArgs e)
        {
            if (!IsMonitored(e.FullPath)) return;
            ThreadPool.QueueUserWorkItem((state) =>
            {
                    if (ftpCl.DirectoryExists(e.Name))
                    {
                        try
                        {
                            ftpCl.DeleteDirectory(e.Name);
                        } catch (Exception ex)
                        {
                            tt.ShowAlert($"Failed to delete directory on server: {ex.Message}", ToolTipIcon.Error, $"Delete Directory: {e.FullPath}", 2000);
                        }
                    }
                    else
                    {
                        try
                        {
                            ftp.Delete(e.Name);
                        }
                        catch (Exception ex)
                        {
                            tt.ShowAlert($"Failed to delete file on server: {ex.Message}", ToolTipIcon.Error, $"Delete File: {e.FullPath}", 2000);
                        }
                    }
            });
           
        }

        private static void FileCreated(object sender, FileSystemEventArgs e)
        {
            if (!IsMonitored(e.FullPath)) return;
            ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    ftp.Upload(e.FullPath, e.Name);
                } catch (Exception ex)
                {
                    tt.ShowAlert($"Failed to create file on server: {ex.Message}", ToolTipIcon.Error, $"Create File: {e.FullPath}", 2000);
                }
                
            });
        }

        private static void FileChanged(object sender, FileSystemEventArgs e)
        {
            if (!IsMonitored(e.FullPath)) return;
            ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    ftp.Upload(e.FullPath, e.Name);
                } catch (Exception ex)
                {
                    tt.ShowAlert($"Failed to modify file on server: {ex.Message}", ToolTipIcon.Error, $"Modify File: {e.FullPath}", 2000);
                }
            });
        }
    }
}
