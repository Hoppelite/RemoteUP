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
        private static string[] fileIncludes;
        private static string[] fileExcludes;

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
            fileIncludes = new string[Properties.Settings.Default.includes.Count];
            fileExcludes = new string[Properties.Settings.Default.excludes.Count];
            Properties.Settings.Default.includes.CopyTo(fileIncludes, 0);
            Properties.Settings.Default.excludes.CopyTo(fileExcludes, 0);

            if(fileIncludes.Any(x => !IsValidRegex(x)) || fileExcludes.Any(x => !IsValidRegex(x)))
            {
                tt.ShowAlert($"Invalid Regular Expression", ToolTipIcon.Error, $"Regex Check Failed", 2000);
            }

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

        private static bool IsValidRegex(string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return false;

            try
            {
                Regex.Match("", pattern);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private static bool CheckIncludes(string fileName)
        {
            if (fileIncludes.Count() == 0) return true;
            try
            {
                return fileIncludes.Any(x => Regex.IsMatch(fileName, x));
            } catch (Exception ex)
            {
                tt.ShowAlert($"Failed Checking Includes: {ex.Message}", ToolTipIcon.Error, $"Includes Regex Failed", 2000);
                Application.Exit();
                return false;
            }
        }

        private static bool CheckExcludes(string fileName)
        {
            if (fileExcludes.Count() == 0) return true;
            try
            {
                return fileExcludes.All(x => !Regex.IsMatch(fileName, x));
            }
            catch (Exception ex)
            {
                tt.ShowAlert($"Failed Checking Exludes: {ex.Message}", ToolTipIcon.Error, $"Exclude Regex Failed", 2000);
                Application.Exit();
                return false;
            }
        }

        private static bool IsMonitored(string fileName)
        {
            return CheckExcludes(fileName) && CheckIncludes(fileName);
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
